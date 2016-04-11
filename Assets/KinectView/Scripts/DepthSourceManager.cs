using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Runtime.InteropServices;
using System;

public class DepthSourceManager : MonoBehaviour
{   
    private KinectSensor _Sensor;
    private DepthFrameReader _Reader;
    private ushort[] _Data;

    private DepthSpacePoint[] m_pDepthCoordinates;
    private CoordinateMapper m_pCoordinateMapper;


    public int DepthWidth { get; private set; }
    public int DepthHeight { get; private set; }

    public ushort[] GetData()
    {
        return _Data;
    }

    public ushort GetDepthDataAt(int x, int y,int ColorWidth)
    {
        DepthSpacePoint point = m_pDepthCoordinates[y * ColorWidth + x];
        return _Data[(int)(point.Y * DepthWidth + point.X)];
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();
        
        if (_Sensor != null) 
        {
            _Reader = _Sensor.DepthFrameSource.OpenReader();
            _Data = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels];

            m_pCoordinateMapper = _Sensor.CoordinateMapper;
            m_pDepthCoordinates = new DepthSpacePoint[1920 * 1080];

            DepthWidth = _Reader.DepthFrameSource.FrameDescription.Width;
            DepthHeight = _Reader.DepthFrameSource.FrameDescription.Height;
        }
    }

    void ProcessFrame()
    {
        /*
        var pDepthData = GCHandle.Alloc(_Data, GCHandleType.Pinned);
        var pDepthCoordinatesData = GCHandle.Alloc(m_pDepthCoordinates, GCHandleType.Pinned);

        m_pCoordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
            pDepthData.AddrOfPinnedObject(),
            (uint)_Data.Length * sizeof(ushort),
            pDepthCoordinatesData.AddrOfPinnedObject(),
            (uint)m_pDepthCoordinates.Length);

        pDepthCoordinatesData.Free();
        pDepthData.Free();
        */

        //m_pCoordinateMapper.MapColorFrameToDepthSpace(_Data, m_pDepthCoordinates);
    }

    void Update () 
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                frame.CopyFrameDataToArray(_Data);

                ProcessFrame();

                frame.Dispose();
                frame = null;
            }
        }
    }
    
    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
}
