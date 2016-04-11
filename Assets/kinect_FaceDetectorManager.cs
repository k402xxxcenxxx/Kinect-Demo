using UnityEngine;
using System.Collections;
using Windows.Kinect;
using Microsoft.Kinect.Face;

public class kinect_FaceDetectorManager : MonoBehaviour {
    private ColorFrameReader _Reader;

    /// <summary>
    /// Face rotation display angle increment in degrees
    /// </summary>
    private const float FaceRotationIncrementInDegrees = 5.0f;

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor kinectSensor = null;

    /// <summary>
    /// Coordinate mapper to map one type of point to another
    /// </summary>
    private CoordinateMapper coordinateMapper = null;

    /// <summary>
    /// Reader for body frames
    /// </summary>
    private BodyFrameReader bodyFrameReader = null;

    /// <summary>
    /// Array to store bodies
    /// </summary>
    private Body[] bodies = null;

    /// <summary>
    /// Number of bodies tracked
    /// </summary>
    private int bodyCount;

    /// <summary>
    /// Face frame sources
    /// </summary>
    private FaceFrameSource[] faceFrameSources = null;

    /// <summary>
    /// Face frame readers
    /// </summary>
    private FaceFrameReader[] faceFrameReaders = null;

    /// <summary>
    /// Storage for face frame results
    /// </summary>
    private FaceFrameResult[] faceFrameResults = null;

    /// <summary>
    /// Width of display (color space)
    /// </summary>
    private int displayWidth;

    /// <summary>
    /// Height of display (color space)
    /// </summary>
    private int displayHeight;

    /// <summary>
    /// Display rectangle
    /// </summary>
    private Rect displayRect;

    private Texture2D _Texture;
    private byte[] _Data;
    private TextureDraw lineDrawer;

    // Use this for initialization
    void Start () {
        // one sensor is currently supported
        kinectSensor = KinectSensor.GetDefault();

        // get the coordinate mapper
        coordinateMapper = kinectSensor.CoordinateMapper;

        // get the color frame details
        var frameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

        // set the display specifics
        displayWidth = frameDescription.Width;
        displayHeight = frameDescription.Height;
        displayRect = new Rect(0.0f, 0.0f, displayWidth, displayHeight);

        _Reader = kinectSensor.ColorFrameSource.OpenReader();

        _Texture = new Texture2D(frameDescription.Width, frameDescription.Height, TextureFormat.RGBA32, false);
        _Data = new byte[frameDescription.BytesPerPixel * frameDescription.LengthInPixels];

        // open the reader for the body frames
        bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

        // wire handler for body frame arrival
        bodyFrameReader.FrameArrived += Reader_BodyFrameArrived;

        // set the maximum number of bodies that would be tracked by Kinect
        bodyCount = kinectSensor.BodyFrameSource.BodyCount;

        // allocate storage to store body objects
        bodies = new Body[bodyCount];

        // specify the required face frame results
        FaceFrameFeatures faceFrameFeatures =
            FaceFrameFeatures.BoundingBoxInColorSpace
            | FaceFrameFeatures.PointsInColorSpace
            | FaceFrameFeatures.RotationOrientation
            | FaceFrameFeatures.FaceEngagement
            | FaceFrameFeatures.Glasses
            | FaceFrameFeatures.Happy
            | FaceFrameFeatures.LeftEyeClosed
            | FaceFrameFeatures.RightEyeClosed
            | FaceFrameFeatures.LookingAway
            | FaceFrameFeatures.MouthMoved
            | FaceFrameFeatures.MouthOpen;

        // create a face frame source + reader to track each face in the FOV
        this.faceFrameSources = new FaceFrameSource[bodyCount];
        this.faceFrameReaders = new FaceFrameReader[bodyCount];
        for (int i = 0; i < this.bodyCount; i++)
        {
            // create the face frame source with the required face frame features and an initial tracking Id of 0
            this.faceFrameSources[i] = Microsoft.Kinect.Face.FaceFrameSource.Create(this.kinectSensor, 0, faceFrameFeatures);

            // open the corresponding reader
            this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
        }

        // allocate storage to store face frame results for each face in the FOV
        this.faceFrameResults = new FaceFrameResult[this.bodyCount];

        // set IsAvailableChanged event notifier
        this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

        // open the sensor
        this.kinectSensor.Open();

        for (int i = 0; i < this.bodyCount; i++)
        {
            if (this.faceFrameReaders[i] != null)
            {
                // wire handler for face frame arrival
                this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
            }
        }

        if (this.bodyFrameReader != null)
        {
            // wire handler for body frame arrival
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
        }
    }

    // Update is called once per frame
    void Update () {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();

            if (frame != null)
            {

                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                //_Texture.LoadRawTextureData(_Data);
                //_Texture.Apply();

                frame.Dispose();
                frame = null;
            }
        }
    }

    public Texture2D GetColorTexture() {
        return _Texture;
    }

    private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e) {
        if (this.kinectSensor != null)
        {
            // on failure, set the status text
            string text = this.kinectSensor.IsAvailable ? "SensorAvaildable"
                                                        : "SensorNotAvailable";
        }
    }

    /// <summary>
    /// Handles the face frame data arriving from the sensor
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
    {
        using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
        {
            if (faceFrame != null)
            {
                // get the index of the face source from the face source array
                int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                // check if this face frame has valid face frame results
                if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                {
                    // store this face frame result to draw later
                    this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                }
                else
                {
                    // indicates that the latest face frame result from this reader is invalid
                    this.faceFrameResults[index] = null;
                }
            }
        }
    }

    /// <summary>
    /// Returns the index of the face frame source
    /// </summary>
    /// <param name="faceFrameSource">the face frame source</param>
    /// <returns>the index of the face source in the face source array</returns>
    private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
    {
        int index = -1;

        for (int i = 0; i < this.bodyCount; i++)
        {
            if (this.faceFrameSources[i] == faceFrameSource)
            {
                index = i;
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// Validates face bounding box and face points to be within screen space
    /// </summary>
    /// <param name="faceResult">the face frame result containing face box and points</param>
    /// <returns>success or failure</returns>
    private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
    {
        bool isFaceValid = faceResult != null;

        if (isFaceValid)
        {
            var faceBox = faceResult.FaceBoundingBoxInColorSpace;
            if (faceBox != null)
            {
                // check if we have a valid rectangle within the bounds of the screen space
                isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                              (faceBox.Bottom - faceBox.Top) > 0 &&
                              faceBox.Right <= this.displayWidth &&
                              faceBox.Bottom <= this.displayHeight;

                if (isFaceValid)
                {
                    var facePoints = faceResult.FacePointsInColorSpace;
                    if (facePoints != null)
                    {
                        foreach (Microsoft.Kinect.Face.Point pointF in facePoints.Values)
                        {
                            // check if we have a valid face point within the bounds of the screen space
                            bool isFacePointValid = pointF.X > 0.0f &&
                                                    pointF.Y > 0.0f &&
                                                    pointF.X < this.displayWidth &&
                                                    pointF.Y < this.displayHeight;

                            if (!isFacePointValid)
                            {
                                isFaceValid = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return isFaceValid;
    }

    /// <summary>
    /// Handles the body frame data arriving from the sensor
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
    {
        
        using (var bodyFrame = e.FrameReference.AcquireFrame())
        {
            if (bodyFrame != null)
            {
                // update body data
                bodyFrame.GetAndRefreshBodyData(this.bodies);

                bool drawFaceResult = false;
                var frame = _Reader.AcquireLatestFrame();

                if (frame != null)
                {

                    frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                    //_Texture.LoadRawTextureData(_Data);
                    //_Texture.Apply();

                    frame.Dispose();
                    frame = null;
                }

                _Texture.LoadRawTextureData(_Data);
                _Texture.Apply();

                // iterate through each face source
                for (int i = 0; i < this.bodyCount; i++)
                {
                    // check if a valid face is tracked in this face source
                    if (this.faceFrameSources[i].IsTrackingIdValid)
                    {
                        // check if we have valid face frame results
                        if (this.faceFrameResults[i] != null)
                        {

                            
                            // draw face frame results
                            this.DrawFaceFrameResults(i, this.faceFrameResults[i], _Data,displayWidth,displayHeight);
                            /*
                            // draw the face bounding box
                            var faceBoxSource = faceFrameResults[i].FaceBoundingBoxInColorSpace;
                            Rect faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
                            DrawRectangle(_Texture,faceBox);
                            */
                            if (!drawFaceResult)
                            {
                                drawFaceResult = true;
                                
                            }
                        }
                    }
                    else
                    {
                        // check if the corresponding body is tracked 
                        if (this.bodies[i].IsTracked)
                        {
                            // update the face frame source to track this body
                            this.faceFrameSources[i].TrackingId = this.bodies[i].TrackingId;
                        }
                    }
                }

                if (!drawFaceResult)
                {
                    // if no faces were drawn then this indicates one of the following:
                    // a body was not tracked 
                    // a body was tracked but the corresponding face was not tracked
                    // a body and the corresponding face was tracked though the face box or the face points were not valid
                    
                }
                
            }
            
        }
    }

    /// <summary>
    /// Draws face frame results
    /// </summary>
    /// <param name="faceIndex">the index of the face frame corresponding to a specific body in the FOV</param>
    /// <param name="faceResult">container of all face frame results</param>
    /// <param name="drawingContext">drawing context to render to</param>
    private void DrawFaceFrameResults(int faceIndex, FaceFrameResult faceResult, byte[] drawingTexture, int width, int height)
    {
        // draw the face bounding box
        var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
        Rect faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
        DrawRectangle(drawingTexture, width, height, faceBox);

        if (faceResult.FacePointsInColorSpace != null)
        {
            // draw each face point
            foreach (Microsoft.Kinect.Face.Point pointF in faceResult.FacePointsInColorSpace.Values)
            {
                //DrawEllipse(null, drawingPen, new Point(pointF.X, pointF.Y), FacePointRadius, FacePointRadius);
            }
        }

        string faceText = string.Empty;

        // extract each face property information and store it in faceText
        if (faceResult.FaceProperties != null)
        {
            foreach (var item in faceResult.FaceProperties)
            {
                faceText += item.Key.ToString() + " : ";

                // consider a "maybe" as a "no" to restrict 
                // the detection result refresh rate
                if (item.Value == DetectionResult.Maybe)
                {
                    faceText += DetectionResult.No + "\n";
                }
                else
                {
                    faceText += item.Value.ToString() + "\n";
                }
            }
        }

        // extract face rotation in degrees as Euler angles
        if (faceResult.FaceRotationQuaternion != null)
        {
            int pitch, yaw, roll;
            ExtractFaceRotationInDegrees(faceResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
            faceText += "FaceYaw : " + yaw + "\n" +
                        "FacePitch : " + pitch + "\n" +
                        "FacenRoll : " + roll + "\n";
        }

    }

    void DrawRectangle(byte[] textureBytes, int width, int height, Rect box) {
        
        int leftPoint = (int)box.xMin;
        int rightPoint = (int)box.xMax;
        int topPoint = (int)box.yMin;
        int bottomPoint = (int)box.yMax;

        //由左畫到右
        for (int i = leftPoint; i < rightPoint; i++)
        {
            textureBytes[topPoint * width * 4 + i * 4] = 255;
            textureBytes[bottomPoint * width * 4 + i * 4] = 255;
        }

        //由上畫到下
        for (int i = topPoint; i < bottomPoint; i++)
        {
            textureBytes[i * width * 4 + leftPoint * 4] = 255;
            textureBytes[i * width * 4 + rightPoint * 4] = 255;
        }
    }
    void DrawRectangle(Texture2D textureBytes, Rect box)
    {
        int leftPoint = (int)box.xMin;
        int rightPoint = (int)box.xMax;
        int topPoint = (int)box.yMin;
        int bottomPoint = (int)box.yMax;

        lineDrawer.DrawLine(textureBytes, leftPoint, topPoint, rightPoint, topPoint, UnityEngine.Color.red);
        lineDrawer.DrawLine(textureBytes, leftPoint, bottomPoint, rightPoint, bottomPoint, UnityEngine.Color.red);

        lineDrawer.DrawLine(textureBytes, leftPoint, topPoint, leftPoint, bottomPoint, UnityEngine.Color.red);
        lineDrawer.DrawLine(textureBytes, rightPoint, topPoint, rightPoint, bottomPoint, UnityEngine.Color.red);
    }
    /// <summary>
    /// Converts rotation quaternion to Euler angles 
    /// And then maps them to a specified range of values to control the refresh rate
    /// </summary>
    /// <param name="rotQuaternion">face rotation quaternion</param>
    /// <param name="pitch">rotation about the X-axis</param>
    /// <param name="yaw">rotation about the Y-axis</param>
    /// <param name="roll">rotation about the Z-axis</param>
    private static void ExtractFaceRotationInDegrees(Windows.Kinect.Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
    {
        float x = rotQuaternion.X;
        float y = rotQuaternion.Y;
        float z = rotQuaternion.Z;
        float w = rotQuaternion.W;

        // convert face rotation quaternion to Euler angles in degrees
        float yawD, pitchD, rollD;
        pitchD = (float)( Mathf.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Mathf.PI * 180.0);
        yawD = (float)(Mathf.Asin(2 * ((w * y) - (x * z))) / Mathf.PI * 180.0);
        rollD = (float)(Mathf.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Mathf.PI * 180.0);

        // clamp the values to a multiple of the specified increment to control the refresh rate
        float increment = FaceRotationIncrementInDegrees;
        pitch = (int)(Mathf.Floor((pitchD + ((increment / 2.0f) * (pitchD > 0 ? 1.0f : -1.0f))) / increment) * increment);
        yaw = (int)(Mathf.Floor((yawD + ((increment / 2.0f) * (yawD > 0 ? 1.0f : -1.0f))) / increment) * increment);
        roll = (int)(Mathf.Floor((rollD + ((increment / 2.0f) * (rollD > 0 ? 1.0f : -1.0f))) / increment) * increment);
    }
}
