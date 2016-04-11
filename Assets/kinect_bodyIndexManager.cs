using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Windows.Kinect;
using Microsoft.Kinect;

public class kinect_bodyIndexManager : MonoBehaviour {
    /// <summary>
    /// Size of the RGB pixel in the bitmap
    /// </summary>
    private const int BytesPerPixel = 4;

    /// <summary>
    /// Collection of colors to be used to display the BodyIndexFrame data.
    /// </summary>
    private static readonly uint[] BodyColor =
    {
            255,
            255,
            255,
            255,
            255
        };

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    /// <summary>
    /// Reader for body index frames
    /// </summary>
    private BodyIndexFrameReader bodyIndexFrameReader = null;

    /// <summary>
    /// Description of the data contained in the body index frame
    /// </summary>
    private FrameDescription bodyIndexFrameDescription = null;

    /// <summary>
    /// Bitmap to display
    /// </summary>
    private Texture2D _Texture;

    /// <summary>
    /// Intermediate storage for frame data converted to color
    /// </summary>
    private uint[] bodyIndexPixels = null;

    /// <summary>
    /// Current status text to display
    /// </summary>
    private string statusText = null;

    private int frameWidth;
    private int frameHeight;

    byte[] textureByteArray;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    void Start()
    {
        // get the kinectSensor object
        this.kinectSensor = KinectSensor.GetDefault();

        // open the reader for the depth frames
        this.bodyIndexFrameReader = this.kinectSensor.BodyIndexFrameSource.OpenReader();

        // wire handler for frame arrival
        this.bodyIndexFrameReader.FrameArrived += this.Reader_FrameArrived;

        this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;

        frameWidth = bodyIndexFrameDescription.Width;
        frameHeight = bodyIndexFrameDescription.Height;

        _Texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
        textureByteArray = new byte[frameWidth * frameHeight * 4];

        // allocate space to put the pixels being converted
        this.bodyIndexPixels = new uint[this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height];
        
        // set IsAvailableChanged event notifier
        this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

        // open the sensor
        this.kinectSensor.Open();
    }

    private void Reader_FrameArrived(object sender, BodyIndexFrameArrivedEventArgs e) {
        bool bodyIndexFrameProcessed = false;

        using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
        {
            if (bodyIndexFrame != null)
            {
                Debug.Log("get bodyIndexFrame");
                // the fastest way to process the body index data is to directly access 
                // the underlying buffer
                using (Windows.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                {
                    Debug.Log("bodyIndexBuffer = bodyIndexFrame.LockImageBuffer()");
                    this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Length);
                    bodyIndexFrameProcessed = true;
                }
            }
        }

        if (bodyIndexFrameProcessed)
        {
            this.RenderBodyIndexPixels();
        }
    }

    /// <summary>
    /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
    /// create a displayable bitmap.
    /// This function requires the /unsafe compiler option as we make use of direct
    /// access to the native memory pointed to by the bodyIndexFrameData pointer.
    /// </summary>
    /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
    /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
    private void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
    {
        Byte[] frameData = new Byte[bodyIndexFrameDataSize];
        Marshal.Copy( bodyIndexFrameData, frameData, 0, (int)bodyIndexFrameDataSize);

        // convert body index to a visual representation
        for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
        {
            // the BodyColor array has been sized to match
            // BodyFrameSource.BodyCount
            if ((int)frameData[i] < BodyColor.Length)
            {
                // this pixel is part of a player,
                // display the appropriate color
                this.bodyIndexPixels[i] = BodyColor[frameData[i]];
            }
            else
            {
                // this pixel is not part of a player
                // display black
                this.bodyIndexPixels[i] = 0;
            }
        }
        
    }

    /// <summary>
    /// Renders color pixels into the writeableBitmap.
    /// </summary>
    private void RenderBodyIndexPixels()
    {
        
        for (int i = 0; i < frameHeight; i++) {
            for (int j = 0; j < frameWidth; j++) {
                textureByteArray[i * frameWidth * 4 + j * 4] = (byte)bodyIndexPixels[i * frameWidth + j];
                textureByteArray[i * frameWidth * 4 + j * 4 + 1] = (byte)bodyIndexPixels[i * frameWidth + j];
                textureByteArray[i * frameWidth * 4 + j * 4 + 2] = (byte)bodyIndexPixels[i * frameWidth + j];
                textureByteArray[i * frameWidth * 4 + j * 4 + 3] = (byte)bodyIndexPixels[i * frameWidth + j];
            }
        }

        RenderBoundingBox();

    }

    /// <summary>
    /// Renders color pixels into the writeableBitmap.
    /// </summary>
    private void RenderBoundingBox()
    {
        int leftPoint = frameWidth-1;
        int rightPoint = 1;
        int topPoint = frameHeight-1;
        int bottomPoint = 1;

        for (int i = 0; i < frameHeight; i++)
        {
            for (int j = 0; j < frameWidth; j++)
            {
                if (bodyIndexPixels[i * frameWidth + j] == 255) {
                    if (j > rightPoint)
                    {
                        rightPoint = j;
                    }
                    else if (j < leftPoint) {
                        leftPoint = j;
                    }

                    if (i > bottomPoint)
                    {
                        bottomPoint = i;
                    }
                    else if (i < topPoint)
                    {
                        topPoint = i;
                    }
                }
                
            }
        }
        Debug.Log("leftPoint : " + leftPoint + ",rightPoint : " + rightPoint + ", topPoint : " + topPoint + ",bottomPoint : " + bottomPoint);
        textureByteArray.toDraw(frameWidth, frameHeight, new Vector2(leftPoint, topPoint), new Vector2(rightPoint, topPoint),Color.red);
        textureByteArray.toDraw(frameWidth, frameHeight, new Vector2(leftPoint, bottomPoint), new Vector2(rightPoint, bottomPoint), Color.red);
        textureByteArray.toDraw(frameWidth, frameHeight, new Vector2(leftPoint, topPoint), new Vector2(leftPoint, bottomPoint), Color.red);
        textureByteArray.toDraw(frameWidth, frameHeight, new Vector2(rightPoint, topPoint), new Vector2(rightPoint, bottomPoint), Color.red);

        _Texture.LoadRawTextureData(textureByteArray);
        _Texture.Apply();
    }

    public Texture2D GetColorTexture()
    {
        return _Texture;
    }

    private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
    {
        if (this.kinectSensor != null)
        {
            // on failure, set the status text
            string text = this.kinectSensor.IsAvailable ? "SensorAvaildable"
                                                        : "SensorNotAvailable";
        }
    }

    // Update is called once per frame
    void Update () {
	
	}
}
