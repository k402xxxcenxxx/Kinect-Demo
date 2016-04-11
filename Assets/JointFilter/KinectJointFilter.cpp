//--------------------------------------------------------------------------------------
// KinectJointFilter.cpp
//
// This file contains Holt Double Exponential Smoothing filter for filtering Joints
//
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

#include "KinectJointFilter.h"

using namespace Sample;
using namespace DirectX;

//-------------------------------------------------------------------------------------
// Name: Lerp()
// Desc: Linear interpolation between two floats
//-------------------------------------------------------------------------------------
inline FLOAT Lerp( FLOAT f1, FLOAT f2, FLOAT fBlend )
{
    return f1 + ( f2 - f1 ) * fBlend;
}
 
//--------------------------------------------------------------------------------------
// if joint is 0 it is not valid.
//--------------------------------------------------------------------------------------
inline BOOL JointPositionIsValid( XMVECTOR vJointPosition )
{
    return ( XMVectorGetX( vJointPosition ) != 0.0f ||
                XMVectorGetY( vJointPosition ) != 0.0f ||
                XMVectorGetZ( vJointPosition ) != 0.0f );
}

//--------------------------------------------------------------------------------------
// Implementation of a Holt Double Exponential Smoothing filter. The double exponential
// smooths the curve and predicts.  There is also noise jitter removal. And maximum
// prediction bounds.  The paramaters are commented in the init function.
//--------------------------------------------------------------------------------------
//void FilterDoubleExponential::Update(IBody* const pBody)
//{
//    assert( pBody );
//
//    // Check for divide by zero. Use an epsilon of a 10th of a millimeter
//    m_fJitterRadius = XMMax( 0.0001f, m_fJitterRadius );
//
//    TRANSFORM_SMOOTH_PARAMETERS SmoothingParams;
//    
//	
//    Joint joints[JointType_Count];
//	UINT jointCapacity = _countof(joints);
//
//    pBody->GetJoints(jointCapacity, joints);
//    for (INT i = 0; i < JointType_Count; i++)
//    {
//        SmoothingParams.fSmoothing      = m_fSmoothing;
//        SmoothingParams.fCorrection     = m_fCorrection;
//        SmoothingParams.fPrediction     = m_fPrediction;
//        SmoothingParams.fJitterRadius   = m_fJitterRadius;
//        SmoothingParams.fMaxDeviationRadius = m_fMaxDeviationRadius;
//
//        // If inferred, we smooth a bit more by using a bigger jitter radius
//        Joint joint = joints[i];
//        if ( joint.TrackingState == TrackingState::TrackingState_Inferred )
//        {
//            SmoothingParams.fJitterRadius       *= 2.0f;
//            SmoothingParams.fMaxDeviationRadius *= 2.0f;
//        }
//
//		//Update(joints, i, joint.JointType,joint.TrackingState, SmoothingParams);
//    }
//}

void FilterDoubleExponential::Update(Joint joints[], JointOrientation orients[], int body_id)
{
    // Check for divide by zero. Use an epsilon of a 10th of a millimeter
    m_fJitterRadius = XMMax(0.0001f, m_fJitterRadius);

    TRANSFORM_SMOOTH_PARAMETERS SmoothingParams;
    for (INT i = 0; i < JointType_Count; i++)
    {
        SmoothingParams.fSmoothing = m_fSmoothing;
        SmoothingParams.fCorrection = m_fCorrection;
        SmoothingParams.fPrediction = m_fPrediction;
        SmoothingParams.fJitterRadius = m_fJitterRadius;
        SmoothingParams.fMaxDeviationRadius = m_fMaxDeviationRadius;

        // If inferred, we smooth a bit more by using a bigger jitter radius
        Joint joint = joints[i];
		if (joint.TrackingState == TrackingState::TrackingState_Inferred || i == JointType_FootLeft || i == JointType_FootRight)
        {
            SmoothingParams.fJitterRadius *= 2.0f;
            SmoothingParams.fMaxDeviationRadius *= 2.0f;
        }

		Update(joints, orients, i, joint.JointType, joint.TrackingState, SmoothingParams, body_id);
		
    }

}

void FilterDoubleExponential::Update(Joint joints[],JointOrientation orients[], UINT JointID, JointType type, TrackingState state, TRANSFORM_SMOOTH_PARAMETERS smoothingParams, int body_id)
{
	//printf("body: %d\n", body_id);
    XMVECTOR vPrevRawPosition;
    XMVECTOR vPrevFilteredPosition;
    XMVECTOR vPrevTrend;
    XMVECTOR vRawPosition;
    XMVECTOR vFilteredPosition;
    XMVECTOR vPredictedPosition;
    XMVECTOR vDiff;
    XMVECTOR vTrend;
    XMVECTOR vLength;
    FLOAT fDiff;
    BOOL bJointIsValid;

	XMVECTOR filteredOrientation;
	XMVECTOR trend;
	XMVECTOR rawOrientation = XMVectorSet(orients[JointID].Orientation.x, orients[JointID].Orientation.y, orients[JointID].Orientation.z, orients[JointID].Orientation.w);

	XMVECTOR prevFilteredOrientation =m_pHistory[body_id][JointID].m_vFilteredOrientation;
	XMVECTOR prevTrend =m_pHistory[body_id][JointID].m_vTrendOrientation;

    const Joint joint = joints[JointID];
	const JointOrientation orient = orients[JointID];

    vRawPosition = XMVectorSet(joint.Position.X, joint.Position.Y, joint.Position.Z, 0.0f);
    vPrevFilteredPosition =m_pHistory[body_id][JointID].m_vFilteredPosition;
    vPrevTrend =m_pHistory[body_id][JointID].m_vTrend;
    vPrevRawPosition =m_pHistory[body_id][JointID].m_vRawPosition;
    bJointIsValid = JointPositionIsValid(vRawPosition);

    // If joint is invalid, reset the filter
    if (!bJointIsValid)
    {
		rawOrientation =m_pHistory[body_id][JointID].m_vFilteredOrientation;
       m_pHistory[body_id][JointID].m_dwFrameCount = 0;
    }

    // Initial start values
    if (m_pHistory[body_id][JointID].m_dwFrameCount == 0)
    {
        vFilteredPosition = vRawPosition;
        vTrend = XMVectorZero();
       m_pHistory[body_id][JointID].m_dwFrameCount++;

		filteredOrientation = rawOrientation;
		trend = XMVectorSet(0.0f, 0.0f, 0.0f, 1.0f);
    }
    else if (m_pHistory[body_id][JointID].m_dwFrameCount == 1)
    {
        vFilteredPosition = XMVectorScale(XMVectorAdd(vRawPosition, vPrevRawPosition), 0.5f);
        vDiff = XMVectorSubtract(vFilteredPosition, vPrevFilteredPosition);
        vTrend = XMVectorAdd(XMVectorScale(vDiff, smoothingParams.fCorrection), XMVectorScale(vPrevTrend, 1.0f - smoothingParams.fCorrection));
       m_pHistory[body_id][JointID].m_dwFrameCount++;

		XMVECTOR prevRawOrientation =m_pHistory[body_id][JointID].m_vRawOrientation;
		filteredOrientation = EnhansedQuaternionSlerp(prevRawOrientation, rawOrientation, 0.5f);

		XMVECTOR diffStarted = RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
		trend = EnhansedQuaternionSlerp(prevTrend, diffStarted, smoothingParams.fCorrection);
    }
    else
    {
        // First apply jitter filter
        vDiff = XMVectorSubtract(vRawPosition, vPrevFilteredPosition);
        vLength = XMVector3Length(vDiff);
        fDiff = fabs(XMVectorGetX(vLength));

        if (fDiff <= smoothingParams.fJitterRadius)
        {
            vFilteredPosition = XMVectorAdd(XMVectorScale(vRawPosition, fDiff / smoothingParams.fJitterRadius),
                XMVectorScale(vPrevFilteredPosition, 1.0f - fDiff / smoothingParams.fJitterRadius));
        }
        else
        {
            vFilteredPosition = vRawPosition;
        }

		XMVECTOR diffJitter = RotationBetweenQuaternions(rawOrientation, prevFilteredOrientation);
		float diffValJitter = std::fabs(QuaternionAngle(diffJitter));
		if (diffValJitter <= smoothingParams.fJitterRadius)
		{
			filteredOrientation = EnhansedQuaternionSlerp(prevFilteredOrientation, rawOrientation, diffValJitter / smoothingParams.fJitterRadius);
		}
		else
		{
			filteredOrientation = rawOrientation;
		}

        // Now the double exponential smoothing filter
        vFilteredPosition = XMVectorAdd(XMVectorScale(vFilteredPosition, 1.0f - smoothingParams.fSmoothing),
            XMVectorScale(XMVectorAdd(vPrevFilteredPosition, vPrevTrend), smoothingParams.fSmoothing));


        vDiff = XMVectorSubtract(vFilteredPosition, vPrevFilteredPosition);
        vTrend = XMVectorAdd(XMVectorScale(vDiff, smoothingParams.fCorrection), XMVectorScale(vPrevTrend, 1.0f - smoothingParams.fCorrection));

		// Now the double exponential smoothing filter
		filteredOrientation = EnhansedQuaternionSlerp(filteredOrientation, XMQuaternionMultiply(prevFilteredOrientation, prevTrend), smoothingParams.fSmoothing);

		diffJitter = RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
		trend = EnhansedQuaternionSlerp(prevTrend, diffJitter, smoothingParams.fCorrection);
    }

    // Predict into the future to reduce latency
    vPredictedPosition = XMVectorAdd(vFilteredPosition, XMVectorScale(vTrend, smoothingParams.fPrediction));
	XMVECTOR predictedOrientation = XMQuaternionMultiply(filteredOrientation, EnhansedQuaternionSlerp(XMVectorSet(0.0f, 0.0f, 0.0f, 1.0f), trend, smoothingParams.fPrediction));

    // Check that we are not too far away from raw data
    vDiff = XMVectorSubtract(vPredictedPosition, vRawPosition);
    vLength = XMVector3Length(vDiff);
    fDiff = fabs(XMVectorGetX(vLength));

	XMVECTOR diff = RotationBetweenQuaternions(predictedOrientation, filteredOrientation);
	float diffVal = std::fabs(QuaternionAngle(diff));

    if (fDiff > smoothingParams.fMaxDeviationRadius)
    {
        vPredictedPosition = XMVectorAdd(XMVectorScale(vPredictedPosition, smoothingParams.fMaxDeviationRadius / fDiff),
            XMVectorScale(vRawPosition, 1.0f - smoothingParams.fMaxDeviationRadius / fDiff));
    }
	if (diffVal > smoothingParams.fMaxDeviationRadius)
	{
		predictedOrientation = EnhansedQuaternionSlerp(filteredOrientation, predictedOrientation, smoothingParams.fMaxDeviationRadius / diffVal);
	}
	predictedOrientation = XMQuaternionNormalize(predictedOrientation);
	filteredOrientation = XMQuaternionNormalize(filteredOrientation);
	trend = XMQuaternionNormalize(trend);

    // Save the data from this frame
   m_pHistory[body_id][JointID].m_vRawPosition = vRawPosition;
   m_pHistory[body_id][JointID].m_vFilteredPosition = vFilteredPosition;
   m_pHistory[body_id][JointID].m_vTrend = vTrend;
	
	if (!XMQuaternionIsNaN(rawOrientation) && !XMQuaternionIsNaN(filteredOrientation) && !XMQuaternionIsNaN(trend))
	{
		m_pHistory[body_id][JointID].m_vRawOrientation = rawOrientation;
		m_pHistory[body_id][JointID].m_vFilteredOrientation = filteredOrientation;
		m_pHistory[body_id][JointID].m_vTrendOrientation = trend;
	}

    // Output the data
	vPredictedPosition = XMVectorSetW(vPredictedPosition, 1.0f);
	Joint j;
	j.Position.X = XMVectorGetX(vPredictedPosition);
	j.Position.Y = XMVectorGetY(vPredictedPosition);
	j.Position.Z = XMVectorGetZ(vPredictedPosition);
	j.JointType = type;
	j.TrackingState = state;
    m_pFilteredJoints[JointID] = j;


	// HipCenter has no parent and is the root of our skeleton - leave the HipCenter absolute set as it is
	if (type != JointType_SpineBase)
	{
		JointOrientation jo;
		jo.Orientation.x = XMVectorGetX(predictedOrientation);
		jo.Orientation.y = XMVectorGetY(predictedOrientation);
		jo.Orientation.z = XMVectorGetZ(predictedOrientation);
		jo.Orientation.w = XMVectorGetW(predictedOrientation);
		jo.JointType = type;
		m_pFilteredOrientations[JointID] = jo;
	}
	else
	{
		m_pFilteredOrientations[JointID] = orients[JointID];
	}
	//m_pFilteredJoints[JointID] 
}

XMVECTOR FilterDoubleExponential::RotationBetweenQuaternions(XMVECTOR Q1, XMVECTOR Q2)
{
	XMVECTOR modifiedQ2 = EnsureQuaternionNeighborhood(Q1, Q2);
	return XMQuaternionMultiply(XMQuaternionInverse(Q1), modifiedQ2);
}
XMVECTOR FilterDoubleExponential::EnsureQuaternionNeighborhood(XMVECTOR Q1, XMVECTOR Q2)
{
	if (QuaternionDot(Q1, Q2) < 0)
	{
		return XMVectorSet(-XMVectorGetX(Q2), -XMVectorGetY(Q2), -XMVectorGetZ(Q2), -XMVectorGetW(Q2));
	}
	return Q2;
}
float FilterDoubleExponential::QuaternionAngle(XMVECTOR rotation)
{
	rotation = XMQuaternionNormalize(rotation);
	float angle = 2.0f * (float)std::acos(XMVectorGetW(rotation));
	return angle;
}
XMVECTOR FilterDoubleExponential::EnhansedQuaternionSlerp(XMVECTOR Q1, XMVECTOR Q2, float amount)
{
	XMVECTOR modifiedQ2 = EnsureQuaternionNeighborhood(Q1, Q2);
	return XMQuaternionSlerp(Q1, modifiedQ2, amount);
}
float FilterDoubleExponential::QuaternionDot(XMVECTOR Q1, XMVECTOR Q2)
{
	return XMVectorGetW(Q1)*XMVectorGetW(Q2) + XMVectorGetX(Q1)*XMVectorGetX(Q2) + XMVectorGetY(Q1)*XMVectorGetY(Q2) + XMVectorGetZ(Q1)*XMVectorGetZ(Q2);
}