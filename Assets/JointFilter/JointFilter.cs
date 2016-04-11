
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

// Holt Double Exponential Smoothing filter
public class FilterDoubleExponentialData
{
    public Vector3 m_vRawPosition;
    public Quaternion m_vRawOrientation;
    public Vector3 m_vFilteredPosition;
    public Quaternion m_vFilteredOrientation;
    public Vector3 m_vTrend;
    public Quaternion m_vTrendOrientation;
    public int m_dwFrameCount;

    public FilterDoubleExponentialData() {
        m_vRawPosition = Vector3.zero;
        m_vRawOrientation = Quaternion.identity;
        m_vFilteredPosition = Vector3.zero;
        m_vFilteredOrientation = Quaternion.identity;
        m_vTrend = Vector3.zero;
        m_vTrendOrientation = Quaternion.identity;
        m_dwFrameCount = 0;
    }
};

public class JointFilter : MonoBehaviour {

    int JointType_Count = 25;
    int BODY_COUNT = 5;

    public struct TRANSFORM_SMOOTH_PARAMETERS
    {
        public float fSmoothing;             // [0..1], lower values closer to raw data
        public float fCorrection;            // [0..1], lower values slower to correct towards the raw data
        public float fPrediction;            // [0..n], the number of frames to predict into the future
        public float fJitterRadius;          // The radius in meters for jitter reduction
        public float fMaxDeviationRadius;    // The maximum radius in meters that filtered positions are allowed to deviate from raw data
    };

    public JointFilter() { Init(); }

    public void Init(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
    {
        m_pFilteredJoints = new Dictionary<Windows.Kinect.JointType, Kinect.Joint>(JointType_Count);
        m_pFilteredOrientations = new Dictionary<Windows.Kinect.JointType, Windows.Kinect.JointOrientation>(JointType_Count);
        m_pHistory = new FilterDoubleExponentialData[BODY_COUNT, JointType_Count];
        for(int i = 0;i < BODY_COUNT; i++)
        {
            for (int j = 0; j < JointType_Count; j++)
            {
                m_pHistory[i, j] = new FilterDoubleExponentialData();
            }
        }
        
        Reset(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);
    }

    public void Reset(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
    {
        m_fMaxDeviationRadius = fMaxDeviationRadius; // Size of the max prediction radius Can snap back to noisy data when too high
        m_fSmoothing = fSmoothing;                   // How much smothing will occur.  Will lag when too high
        m_fCorrection = fCorrection;                 // How much to correct back from prediction.  Can make things springy
        m_fPrediction = fPrediction;                 // Amount of prediction into the future to use. Can over shoot when too high
        m_fJitterRadius = fJitterRadius;             // Size of the radius where jitter is removed. Can do too much smoothing when too high

        m_pFilteredJoints = new Dictionary<Windows.Kinect.JointType, Windows.Kinect.Joint>();
        m_pFilteredOrientations = new Dictionary<Windows.Kinect.JointType, Windows.Kinect.JointOrientation>();
        m_pHistory = new FilterDoubleExponentialData[BODY_COUNT, JointType_Count];
        for (int i = 0; i < BODY_COUNT; i++)
        {
            for (int j = 0; j < JointType_Count; j++)
            {
                m_pHistory[i, j] = new FilterDoubleExponentialData();
            }
        }
        /*
        memset(m_pFilteredJoints, 0, sizeof(Joint) * JointType_Count);
        memset(m_pFilteredOrientations, 0, sizeof(JointOrientation) * JointType_Count);
        memset(m_pHistory[0], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        memset(m_pHistory[1], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        memset(m_pHistory[2], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        memset(m_pHistory[3], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        memset(m_pHistory[4], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        memset(m_pHistory[5], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        */
    }

    public void JointUpdate(Dictionary<Kinect.JointType, Kinect.Joint> joints, Dictionary<Kinect.JointType, Kinect.JointOrientation> orients, int body_id = 0)
    {
        // Check for divide by zero. Use an epsilon of a 10th of a millimeter
        if(m_fJitterRadius < Mathf.Epsilon)
            m_fJitterRadius = Mathf.Epsilon;

        TRANSFORM_SMOOTH_PARAMETERS SmoothingParams;

        for (int i = 0; i < JointType_Count; i++)
        {
            SmoothingParams.fSmoothing = m_fSmoothing;
            SmoothingParams.fCorrection = m_fCorrection;
            SmoothingParams.fPrediction = m_fPrediction;
            SmoothingParams.fJitterRadius = m_fJitterRadius;
            SmoothingParams.fMaxDeviationRadius = m_fMaxDeviationRadius;

            // If inferred, we smooth a bit more by using a bigger jitter radius
            Kinect.Joint joint = joints[(Kinect.JointType)i];
            if (joint.TrackingState == Kinect.TrackingState.Inferred || i == (int)Kinect.JointType.FootLeft || i == (int)Kinect.JointType.FootRight)
            {
                SmoothingParams.fJitterRadius *= 2.0f;
                SmoothingParams.fMaxDeviationRadius *= 2.0f;
            }

            JointUpdate(joints, orients, i, joint.JointType, joint.TrackingState, SmoothingParams, body_id);

        }

    }
    public Dictionary<Kinect.JointType, Kinect.Joint> GetFilteredJoints() { return m_pFilteredJoints; }
    public Dictionary<Kinect.JointType, Kinect.JointOrientation> GetFilteredOrientations() { return m_pFilteredOrientations; }

    Dictionary<Kinect.JointType, Kinect.Joint> m_pFilteredJoints;
    Dictionary<Kinect.JointType, Kinect.JointOrientation> m_pFilteredOrientations;
    FilterDoubleExponentialData[,] m_pHistory;
       
    float m_fSmoothing;
    float m_fCorrection;
    float m_fPrediction;
    float m_fJitterRadius;
    float m_fMaxDeviationRadius;

    public void JointUpdate(Dictionary<Kinect.JointType, Kinect.Joint> joints, Dictionary<Kinect.JointType, Kinect.JointOrientation> orients, int JointID, Kinect.JointType type, Kinect.TrackingState state, TRANSFORM_SMOOTH_PARAMETERS smoothingParams, int body_id = 0)
    {
        //printf("body: %d\n", body_id);
        Vector3 vPrevRawPosition;
        Vector3 vPrevFilteredPosition;
        Vector3 vPrevTrend;
        Vector3 vRawPosition;
        Vector3 vFilteredPosition;
        Vector3 vPredictedPosition;
        Vector3 vDiff;
        Vector3 vTrend;
        float vLength;
        float fDiff;
        bool bJointIsValid;

        Quaternion filteredOrientation;
        Quaternion trend;
        Quaternion rawOrientation = new Quaternion(orients[(Kinect.JointType)JointID].Orientation.X, orients[(Kinect.JointType)JointID].Orientation.Y, orients[(Kinect.JointType)JointID].Orientation.Z, orients[(Kinect.JointType)JointID].Orientation.W);

        Quaternion prevFilteredOrientation = m_pHistory[body_id,JointID].m_vFilteredOrientation;
        Quaternion prevTrend = m_pHistory[body_id,JointID].m_vTrendOrientation;

        Kinect.Joint joint = joints[(Kinect.JointType)JointID];
        Kinect.JointOrientation orient = orients[(Kinect.JointType)JointID];

        vRawPosition = new Vector4(joint.Position.X, joint.Position.Y, joint.Position.Z, 0.0f);
        vPrevFilteredPosition = m_pHistory[body_id,JointID].m_vFilteredPosition;
        vPrevTrend = m_pHistory[body_id,JointID].m_vTrend;
        vPrevRawPosition = m_pHistory[body_id,JointID].m_vRawPosition;
        bJointIsValid = JointPositionIsValid(vRawPosition);

        // If joint is invalid, reset the filter
        if (!bJointIsValid)
        {
            rawOrientation = m_pHistory[body_id,JointID].m_vFilteredOrientation;
            m_pHistory[body_id,JointID].m_dwFrameCount = 0;
        }

        // Initial start values
        if (m_pHistory[body_id,JointID].m_dwFrameCount == 0)
        {
            vFilteredPosition = vRawPosition;
            vTrend = Vector3.zero;
            m_pHistory[body_id,JointID].m_dwFrameCount++;

            filteredOrientation = rawOrientation;
            trend = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (m_pHistory[body_id,JointID].m_dwFrameCount == 1)
        {
            vFilteredPosition = (vRawPosition + vPrevRawPosition)* 0.5f;
            vDiff = vFilteredPosition - vPrevFilteredPosition;
            vTrend = (vDiff* smoothingParams.fCorrection) + vPrevTrend* (1.0f - smoothingParams.fCorrection);
            m_pHistory[body_id,JointID].m_dwFrameCount++;

            Quaternion prevRawOrientation = m_pHistory[body_id,JointID].m_vRawOrientation;
            filteredOrientation = EnhansedQuaternionSlerp(prevRawOrientation, rawOrientation, 0.5f);

            Quaternion diffStarted = RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
            trend = EnhansedQuaternionSlerp(prevTrend, diffStarted, smoothingParams.fCorrection);
        }
        else
        {
            // First apply jitter filter
            vDiff = vRawPosition - vPrevFilteredPosition;
            vLength = Mathf.Sqrt(vDiff.sqrMagnitude);
            fDiff = Mathf.Abs(vLength);

            if (fDiff <= smoothingParams.fJitterRadius)
            {
                vFilteredPosition = vRawPosition *( fDiff / smoothingParams.fJitterRadius) + vPrevFilteredPosition * (1.0f - fDiff / smoothingParams.fJitterRadius);
            }
            else
            {
                vFilteredPosition = vRawPosition;
            }

            Quaternion diffJitter = RotationBetweenQuaternions(rawOrientation, prevFilteredOrientation);
            float diffValJitter = Mathf.Abs(QuaternionAngle(diffJitter));
            if (diffValJitter <= smoothingParams.fJitterRadius)
            {
                filteredOrientation = EnhansedQuaternionSlerp(prevFilteredOrientation, rawOrientation, diffValJitter / smoothingParams.fJitterRadius);
            }
            else
            {
                filteredOrientation = rawOrientation;
            }

            // Now the double exponential smoothing filter
            vFilteredPosition = vFilteredPosition * ( 1.0f - smoothingParams.fSmoothing) + (vPrevFilteredPosition + vPrevTrend) * smoothingParams.fSmoothing;


            vDiff = vFilteredPosition - vPrevFilteredPosition;
            vTrend = ((vDiff * smoothingParams.fCorrection ) + (vPrevTrend * ( 1.0f - smoothingParams.fCorrection)));

            // Now the double exponential smoothing filter
            filteredOrientation = EnhansedQuaternionSlerp(filteredOrientation,(prevFilteredOrientation * prevTrend), smoothingParams.fSmoothing);

            diffJitter = RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
            trend = EnhansedQuaternionSlerp(prevTrend, diffJitter, smoothingParams.fCorrection);
        }

        // Predict into the future to reduce latency
        vPredictedPosition = (vFilteredPosition+ (vTrend* smoothingParams.fPrediction));
        Quaternion predictedOrientation = (filteredOrientation * (EnhansedQuaternionSlerp(new Quaternion(0.0f, 0.0f, 0.0f, 1.0f), trend, smoothingParams.fPrediction)));

        // Check that we are not too far away from raw data
        vDiff = (vRawPosition - vPrevFilteredPosition);
        vLength = Mathf.Sqrt(vDiff.sqrMagnitude);
        fDiff = Mathf.Abs(vLength);

        Quaternion diff = RotationBetweenQuaternions(predictedOrientation, filteredOrientation);
        float diffVal = Mathf.Abs(QuaternionAngle(diff));

        if (fDiff > smoothingParams.fMaxDeviationRadius)
        {
            vPredictedPosition = ((vPredictedPosition *( smoothingParams.fMaxDeviationRadius / fDiff)) + (vRawPosition * ( 1.0f - smoothingParams.fMaxDeviationRadius / fDiff)));
        }
        if (diffVal > smoothingParams.fMaxDeviationRadius)
        {
            predictedOrientation = EnhansedQuaternionSlerp(filteredOrientation, predictedOrientation, smoothingParams.fMaxDeviationRadius / diffVal);
        }
        predictedOrientation = QuaternionNormalise(predictedOrientation);
        filteredOrientation = QuaternionNormalise(filteredOrientation);
        trend = QuaternionNormalise(trend);

        // Save the data from this frame
        m_pHistory[body_id,JointID].m_vRawPosition = vRawPosition;
        m_pHistory[body_id,JointID].m_vFilteredPosition = vFilteredPosition;
        m_pHistory[body_id,JointID].m_vTrend = vTrend;

        if (!(rawOrientation == null) && !(filteredOrientation == null) && !(trend == null))
        {
            m_pHistory[body_id,JointID].m_vRawOrientation = rawOrientation;
            m_pHistory[body_id,JointID].m_vFilteredOrientation = filteredOrientation;
            m_pHistory[body_id,JointID].m_vTrendOrientation = trend;
        }

        // Output the data
        //vPredictedPosition = new Vector4(vPredictedPosition , 1.0f);
        Kinect.Joint j = new Windows.Kinect.Joint();
        Kinect.CameraSpacePoint CameraSpacePoint = new Windows.Kinect.CameraSpacePoint();

        CameraSpacePoint.X = vPredictedPosition.x;
        CameraSpacePoint.Y = vPredictedPosition.y;
        CameraSpacePoint.Z = vPredictedPosition.z;

        j.Position = CameraSpacePoint;
        j.JointType = type;
        j.TrackingState = state;
        m_pFilteredJoints[(Kinect.JointType)JointID] = j;


        // HipCenter has no parent and is the root of our skeleton - leave the HipCenter absolute set as it is
        if (type != Kinect.JointType.SpineBase)
        {
            Kinect.JointOrientation jo = new Windows.Kinect.JointOrientation();

            Kinect.Vector4 v4 = new Kinect.Vector4();
            v4.X = predictedOrientation.x;
            v4.Y = predictedOrientation.y;
            v4.Z = predictedOrientation.z;
            v4.W = predictedOrientation.w;

            jo.Orientation = v4;

            jo.JointType = type;
            m_pFilteredOrientations[(Kinect.JointType)JointID] = jo;
        }
        else
        {
            m_pFilteredOrientations[(Kinect.JointType)JointID] = orients[(Kinect.JointType)JointID];
        }
        //m_pFilteredJoints[JointID] 
    }
    Quaternion RotationBetweenQuaternions(Quaternion Q1, Quaternion Q2)
    {
        Quaternion modifiedQ2 = EnsureQuaternionNeighborhood(Q1, Q2);
        return Quaternion.Inverse(Q1) * modifiedQ2;
    }
    Quaternion EnsureQuaternionNeighborhood(Quaternion Q1, Quaternion Q2) {
        if (QuaternionDot(Q1, Q2) < 0)
        {
            return new Quaternion(-(Q2.x), -(Q2.y), -(Q2.z), -(Q2.w));
        }
        return Q2;
    }
    Quaternion EnhansedQuaternionSlerp(Quaternion Q1, Quaternion Q2, float amount) {
        Quaternion modifiedQ2 = EnsureQuaternionNeighborhood(Q1, Q2);
        return Quaternion.Slerp(Q1, modifiedQ2, amount);
    }
    float QuaternionDot(Quaternion Q1, Quaternion Q2) {
        return (Q1.w) * (Q2.w) + (Q1.x) * (Q2.x) + (Q1.y) * (Q2.y) + (Q1.z) * (Q2.z);
    }
    float QuaternionAngle(Quaternion rotation) {
        rotation = QuaternionNormalise(rotation);
        float angle = 2.0f * Mathf.Acos((rotation.w));
        return angle;
    }
    Quaternion QuaternionNormalise(Quaternion Q)
    {
        float n = Mathf.Sqrt(Q.x * Q.x + Q.y * Q.y + Q.z * Q.z + Q.w * Q.w);
        Q.x /= n;
        Q.y /= n;
        Q.z /= n;
        Q.w /= n;

        return Q;
    }
    //-------------------------------------------------------------------------------------
    // Name: Lerp()
    // Desc: Linear interpolation between two floats
    //-------------------------------------------------------------------------------------

    float Lerp(float f1, float f2, float fBlend)
    {
        return f1 + (f2 - f1) * fBlend;
    }

//--------------------------------------------------------------------------------------
// if joint is 0 it is not valid.
//--------------------------------------------------------------------------------------

    bool JointPositionIsValid(Vector3 vJointPosition)
    {
        return (vJointPosition.x != 0.0f ||
                vJointPosition.x != 0.0f ||
                vJointPosition.x != 0.0f);
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
    
}
