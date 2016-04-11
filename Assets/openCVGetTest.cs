using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Windows.Kinect;

public class openCVGetTest : MonoBehaviour {
    [DllImport("DlibFaceDetector")]
    static extern bool face_detect(Byte[] src, int width, int height, Byte[] output);

    [DllImport("DlibFaceDetector")]
    static extern bool face_detect2(Byte[] src, int width, int height, Byte[] output);

    [DllImport("DlibFaceDetector")]
    static extern bool face_detect3(Byte[] src, int width, int height);

    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    public Texture2D testImage;
    public bool testImageMode;
    public int FaceDetectorNum = 1;
    public int testPerFrame = 60;
    int times;

    private KinectSensor _Sensor;
    private ColorFrameReader _Reader;
    private Texture2D _Texture;
    private Texture2D _SlicedTexture;
    private byte[] _Data;

    private byte[] _BufferData;
    private byte[] _TestdstByteData;
    bool testOnce = false;

    public Texture2D GetColorTexture()
    {
        return _Texture;
    }

    public Texture2D GetslicedTexture() {
        return _SlicedTexture;
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.ColorFrameSource.OpenReader();

            var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;

            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _SlicedTexture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

            
            
            _BufferData = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
            _TestdstByteData = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
            

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }

            //測單一圖片
            if (testImageMode) { 
                _Texture = new Texture2D(testImage.width, testImage.height, TextureFormat.RGBA32, false);
                _BufferData = new byte[testImage.GetRawTextureData().Length];
                _TestdstByteData = new byte[testImage.GetRawTextureData().Length];
                _BufferData = testImage.GetRawTextureData();
            
                print("testImage.GetRawTextureData().Length : " + testImage.GetRawTextureData().Length);
                print("testImage.width :" + testImage.width + " , testImage.height : " + testImage.height);

                switch (FaceDetectorNum) {
                    case 1:
                        if (face_detect(_BufferData, testImage.width, testImage.height, _TestdstByteData))
                        {
                            print("face_detect1 result : true");
                        }
                        else
                        {
                            print("face_detect1 result : false");
                        }
                        _Texture.LoadRawTextureData(_TestdstByteData);
                        break;
                    case 2:
                        if (face_detect2(_BufferData, testImage.width, testImage.height, _TestdstByteData))
                        {
                            print("face_detect2 result : true");
                        }
                        else
                        {
                            print("face_detect2 result : false");
                        }
                        _Texture.LoadRawTextureData(_TestdstByteData);
                        break;
                    case 3:
                        if (face_detect3(_BufferData, testImage.width, testImage.height))
                        {
                            print("face_detect3 result : true");
                        }
                        else
                        {
                            print("face_detect3 result : false");
                        }
                        _Texture.LoadRawTextureData(_BufferData);
                        break;
                    default:
                        if (face_detect2(_BufferData, testImage.width, testImage.height, _TestdstByteData))
                        {
                            print("face_detect2 result : true");
                        }
                        else
                        {
                            print("face_detect2 result : false");
                        }
                        _Texture.LoadRawTextureData(_TestdstByteData);
                        break;
                }

               
                _Texture.Apply();
                
                
            }

        }
    }

    void Update()
    {
        if (times < testPerFrame)
        {
            times++;
            return;
        }
        else {
            times = 0;
        }
        
        if (_Reader != null)
        {
            
            var frame = _Reader.AcquireLatestFrame();

            if (frame != null)
            {
                if (!testImageMode) { 
                    frame.CopyConvertedFrameDataToArray(_BufferData, ColorImageFormat.Rgba);
                    
                    switch (FaceDetectorNum)
                    {
                        case 1:
                            if (face_detect(_BufferData, ColorWidth,ColorHeight, _TestdstByteData))
                            {
                                print("face_detect1 result : true");
                            }
                            else
                            {
                                print("face_detect1 result : false");
                            }

                            _Texture.LoadRawTextureData(_TestdstByteData);
                            break;
                        case 2:
                            if (face_detect2(_BufferData, ColorWidth, ColorHeight, _TestdstByteData))
                            {
                                print("face_detect2 result : true");
                            }
                            else
                            {
                                print("face_detect2 result : false");
                            }

                            _Texture.LoadRawTextureData(_TestdstByteData);
                            break;
                        case 3:
                            if (face_detect3(_BufferData, ColorWidth, ColorHeight))
                            {
                                print("face_detect3 result : true");
                            }
                            else
                            {
                                print("face_detect3 result : false");
                            }

                            _Texture.LoadRawTextureData(_BufferData);
                            break;
                        default:
                            if (face_detect2(_BufferData, ColorWidth, ColorHeight, _TestdstByteData))
                            {
                                print("face_detect2 result : true");
                            }
                            else
                            {
                                print("face_detect2 result : false");
                            }
                            _Texture.LoadRawTextureData(_TestdstByteData);
                            break;
                    }
                    //_Texture.LoadRawTextureData(_TestdstByteData);

                    /*
                    if (face_detect2(_BufferData, ColorWidth, ColorHeight, _TestdstByteData))
                    {
                        _Texture.LoadRawTextureData(_TestdstByteData);
                    }
                    else {
                        frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                        _Texture.LoadRawTextureData(_Data);
                    }
                    */

                    //print("ColorWidth : "+ ColorWidth);
                    //print("ColorHeight : " + ColorHeight);
                    //print("_BufferData.Length : " + _BufferData.Length);

                    //_Texture.LoadRawTextureData(_Data);
                    //_Texture.LoadRawTextureData(_BufferData);


                    //_Texture.SetPixel((int)resultData[0], (int)resultData[1],Color.red);
                    //_Texture.SetPixel((int)(resultData[0] + resultData[2]), (int)(resultData[1] + resultData[3]), Color.red);


                    _Texture.Apply();
                }

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
