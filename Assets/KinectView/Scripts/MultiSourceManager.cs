using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Runtime.InteropServices;
using System;

public class MultiSourceManager : MonoBehaviour {
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    public int DepthWidth { get; private set; }
    public int DepthHeight { get; private set; }

    private KinectSensor _Sensor;
    private MultiSourceFrameReader _Reader;
    private Texture2D _ColorTexture;
    private ushort[] _DepthData;
    private byte[] _ColorData;
    private Body[] _BodyData = null;

    private DepthSpacePoint[] m_pDepthCoordinates;
    private CoordinateMapper m_pCoordinateMapper;

    public Texture2D GetColorTexture()
    {
        return _ColorTexture;
    }
    
    public ushort[] GetDepthData()
    {
        return _DepthData;
    }

    public ushort GetDepthDataAt(int x,int y)
    {
        DepthSpacePoint point = m_pDepthCoordinates[y * ColorWidth + x];
        return _DepthData[(int)(point.Y * DepthWidth + point.X)];
    }

    public Body[] GetBodyData()
    {
        return _BodyData;
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();
        
        if (_Sensor != null)
        {
            m_pCoordinateMapper = _Sensor.CoordinateMapper;

            _Reader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Body);
            
            var colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = colorFrameDesc.Width;
            ColorHeight = colorFrameDesc.Height;

            m_pDepthCoordinates = new DepthSpacePoint[ColorWidth * ColorHeight];


            _ColorTexture = new Texture2D(colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
            _ColorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
            
            var depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            _DepthData = new ushort[depthFrameDesc.LengthInPixels];
            DepthWidth = depthFrameDesc.Width;
            DepthHeight = depthFrameDesc.Height;
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }
    
    void Update () 
    {
        if (_Reader != null) 
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                var colorFrame = frame.ColorFrameReference.AcquireFrame();
                if (colorFrame != null)
                {
                    var depthFrame = frame.DepthFrameReference.AcquireFrame();
                    if (depthFrame != null)
                    {
                        var bodyFrame = frame.BodyFrameReference.AcquireFrame();
                        if (bodyFrame != null)
                        {
                            if (_BodyData == null)
                            {
                                _BodyData = new Body[_Sensor.BodyFrameSource.BodyCount];
                            }

                            colorFrame.CopyConvertedFrameDataToArray(_ColorData, ColorImageFormat.Rgba);
                            _ColorTexture.LoadRawTextureData(_ColorData);
                            _ColorTexture.Apply();

                            depthFrame.CopyFrameDataToArray(_DepthData);
                            ProcessFrame();

                            bodyFrame.GetAndRefreshBodyData(_BodyData);

                            bodyFrame.Dispose();
                            frame = null;
                        }
                        
                        
                        depthFrame.Dispose();
                        depthFrame = null;
                    }
                
                    colorFrame.Dispose();
                    colorFrame = null;
                }
                
                frame = null;
            }
        }
    }

    void ProcessFrame()
    {
        var pDepthData = GCHandle.Alloc(_DepthData, GCHandleType.Pinned);
        var pDepthCoordinatesData = GCHandle.Alloc(m_pDepthCoordinates, GCHandleType.Pinned);

        m_pCoordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
            pDepthData.AddrOfPinnedObject(),
            (uint)_DepthData.Length * sizeof(ushort),
            pDepthCoordinatesData.AddrOfPinnedObject(),
            (uint)m_pDepthCoordinates.Length);

        pDepthCoordinatesData.Free();
        pDepthData.Free();
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
