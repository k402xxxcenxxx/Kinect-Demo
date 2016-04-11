//--------------------------------------------------------------------------------------
// KinectJointFilter.h
//
// This file contains Holt Double Exponential Smoothing filter for filtering Joints
//
//--------------------------------------------------------------------------------------

#pragma once
#include <Kinect.h>
#include <Windows.h>
#include <DirectXMath.h>
#include <queue>

namespace Sample
{
    typedef struct _TRANSFORM_SMOOTH_PARAMETERS
    {
        FLOAT   fSmoothing;             // [0..1], lower values closer to raw data
        FLOAT   fCorrection;            // [0..1], lower values slower to correct towards the raw data
        FLOAT   fPrediction;            // [0..n], the number of frames to predict into the future
        FLOAT   fJitterRadius;          // The radius in meters for jitter reduction
        FLOAT   fMaxDeviationRadius;    // The maximum radius in meters that filtered positions are allowed to deviate from raw data
    } TRANSFORM_SMOOTH_PARAMETERS;

    // Holt Double Exponential Smoothing filter
    class FilterDoubleExponentialData
    {
    public:
        DirectX::XMVECTOR m_vRawPosition;
		DirectX::XMVECTOR m_vRawOrientation;
        DirectX::XMVECTOR m_vFilteredPosition;
		DirectX::XMVECTOR m_vFilteredOrientation;
        DirectX::XMVECTOR m_vTrend;
		DirectX::XMVECTOR m_vTrendOrientation;
        DWORD    m_dwFrameCount;
    };

    class FilterDoubleExponential
    {
    public:
        FilterDoubleExponential() { Init(); }
        ~FilterDoubleExponential() { Shutdown(); }

        VOID Init(FLOAT fSmoothing = 0.25f, FLOAT fCorrection = 0.25f, FLOAT fPrediction = 0.25f, FLOAT fJitterRadius = 0.03f, FLOAT fMaxDeviationRadius = 0.05f)
        {
            Reset(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);
        }

        VOID Shutdown()
        {
        }

        VOID Reset(FLOAT fSmoothing = 0.25f, FLOAT fCorrection = 0.25f, FLOAT fPrediction = 0.25f, FLOAT fJitterRadius = 0.03f, FLOAT fMaxDeviationRadius = 0.05f)
        {
            assert(m_pFilteredJoints);
            assert(m_pHistory);

            m_fMaxDeviationRadius = fMaxDeviationRadius; // Size of the max prediction radius Can snap back to noisy data when too high
            m_fSmoothing = fSmoothing;                   // How much smothing will occur.  Will lag when too high
            m_fCorrection = fCorrection;                 // How much to correct back from prediction.  Can make things springy
            m_fPrediction = fPrediction;                 // Amount of prediction into the future to use. Can over shoot when too high
            m_fJitterRadius = fJitterRadius;             // Size of the radius where jitter is removed. Can do too much smoothing when too high

			memset(m_pFilteredJoints, 0, sizeof(Joint) * JointType_Count);
			memset(m_pFilteredOrientations, 0, sizeof(JointOrientation) * JointType_Count);
            memset(m_pHistory[0], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
			memset(m_pHistory[1], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
			memset(m_pHistory[2], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
			memset(m_pHistory[3], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
			memset(m_pHistory[4], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
			memset(m_pHistory[5], 0, sizeof(FilterDoubleExponentialData) * JointType_Count);
        }

		void Update(Joint joints[], JointOrientation orients[], int body_id=0);
		inline Joint* GetFilteredJoints() { return &m_pFilteredJoints[0]; }
		inline JointOrientation* GetFilteredOrientations() { return &m_pFilteredOrientations[0]; }

    private:
		Joint m_pFilteredJoints[JointType_Count];
		JointOrientation m_pFilteredOrientations[JointType_Count];
		FilterDoubleExponentialData m_pHistory[BODY_COUNT][JointType_Count];
        FLOAT m_fSmoothing;
        FLOAT m_fCorrection;
        FLOAT m_fPrediction;
        FLOAT m_fJitterRadius;
        FLOAT m_fMaxDeviationRadius;

		void Update(Joint joints[], JointOrientation orients[], UINT JointID, JointType type, TrackingState state, TRANSFORM_SMOOTH_PARAMETERS smoothingParams, int body_id=0);
		DirectX::XMVECTOR RotationBetweenQuaternions(DirectX::XMVECTOR Q1, DirectX::XMVECTOR Q2);
		DirectX::XMVECTOR EnsureQuaternionNeighborhood(DirectX::XMVECTOR Q1, DirectX::XMVECTOR Q2);
		DirectX::XMVECTOR EnhansedQuaternionSlerp(DirectX::XMVECTOR Q1, DirectX::XMVECTOR Q2, float amount);
		float QuaternionDot(DirectX::XMVECTOR Q1, DirectX::XMVECTOR Q2);
		float QuaternionAngle(DirectX::XMVECTOR rotation);
    };
}