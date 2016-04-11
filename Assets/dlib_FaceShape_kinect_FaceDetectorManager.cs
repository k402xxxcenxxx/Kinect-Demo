using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Windows.Kinect;
using Microsoft.Kinect.Face;

public class dlib_FaceShape_kinect_FaceDetectorManager : MonoBehaviour
{
    [DllImport("DlibFaceDetector")]
    static extern bool face_shape_detect(Byte[] src, int width, int height, out int pointCount, ref IntPtr point_ptr);

    [DllImport("DlibFaceDetector")]
    static extern bool face_shape_detect_with_face_detect2(Byte[] src, int width, int height, out int pointCount, ref IntPtr point_ptr);

    [DllImport("opencv")]
    static extern void graph_cut(Byte[] src, int width, int height, Byte[] mask_ByteArray);

    [DllImport("DlibFaceDetector")]
    static extern void initShapePredictor();

    [DllImport("DlibFaceDetector")]
    static extern void initFaceDetector();

    public enum GrabCutClasses
    {
        GC_BGD = 0,  //!< an obvious background pixels
        GC_FGD = 1,  //!< an obvious foreground (object) pixel
        GC_PR_BGD = 2,  //!< a possible background pixel
        GC_PR_FGD = 3   //!< a possible foreground pixel
    }

    public bool useDlibFaceDetector = false;

    private ColorFrameReader _Reader;

    //public GameObject MultiSourceManager;
    //private MultiSourceManager _MultiSourceManager;



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
    private Texture2D _SlicedTexture;
    private Texture2D _DepthTexture;
    private byte[] _Data;

    DepthSpacePoint[] DepthSpacePoints;
    private ushort[] _DepthData;
    private int depthWidth;
    private int depthHeight;
    // Use this for initialization
    void Start()
    {
        initFaceDetector();
        initShapePredictor();
        /*
        if (MultiSourceManager != null)
        {
            _MultiSourceManager = MultiSourceManager.GetComponent<MultiSourceManager>();
            depthWidth = _MultiSourceManager.DepthWidth;
            depthHeight = _MultiSourceManager.DepthHeight;
        }
        */

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
        _DepthTexture = new Texture2D(displayWidth, displayHeight, TextureFormat.RGBA32, false);
        _Data = new byte[frameDescription.BytesPerPixel * frameDescription.LengthInPixels];
        DepthSpacePoints = new DepthSpacePoint[displayWidth * displayHeight];

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
    void Update()
    {
        

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

    public Texture2D GetColorTexture()
    {
        return _Texture;
    }

    public Texture2D GetSlicedColorTexture()
    {
        if (_SlicedTexture != null)
            return _SlicedTexture;
        else
            return null;
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
                    
                    frame.Dispose();
                    frame = null;
                }

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
                            this.DrawFaceFrameResults(i, this.faceFrameResults[i], _Data, displayWidth, displayHeight);
                           
                            DrawFaceShape(faceFrameResults[i],_Data, displayWidth, displayHeight);
                            
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
                _Texture.LoadRawTextureData(_Data);
                
                _Texture.Apply();
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

    private void DrawFaceShape(FaceFrameResult faceResult, byte[] drawingTexture, int width, int height)
    {

        var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
        Rect faceBox;

        if (!useDlibFaceDetector)
        {
            faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
        }
        else
        {
            faceBox = new Rect(faceBoxSource.Left - (faceBoxSource.Right - faceBoxSource.Left) * 0.5f, faceBoxSource.Top - (faceBoxSource.Bottom - faceBoxSource.Top) * 0.5f, (faceBoxSource.Right - faceBoxSource.Left) * 2, (faceBoxSource.Bottom - faceBoxSource.Top) * 2);

        }

        byte[] testByteArray;
        float offsetX = faceBox.xMin;
        float offsetY = faceBox.yMin;


        IntPtr facePoints_ptr = IntPtr.Zero;
        int FacePointCount = 0;
        int[] FacePoints;
        bool IsCatched = false;

        testByteArray = new byte[(int)faceBox.width * (int)faceBox.height * 4];
        testByteArray = getSlicedByteArray(drawingTexture, faceBox, width, height);

        if (!useDlibFaceDetector)
        {
            IsCatched = face_shape_detect(testByteArray, (int)faceBox.width, (int)faceBox.height, out FacePointCount, ref facePoints_ptr);
        }
        else
        {
            IsCatched = face_shape_detect_with_face_detect2(testByteArray, (int)faceBox.width, (int)faceBox.height, out FacePointCount, ref facePoints_ptr);
        }

        //face_shape_detect(testByteArray, (int)faceBox.width, (int)faceBox.height, out FacePointCount, ref facePoints_ptr);
        //face_shape_detect(drawingTexture,width, height, out FacePointCount, ref facePoints_ptr);

        if (!IsCatched)
        {
            return;
        }

        FacePoints = new int[(int)FacePointCount * 2];
        Marshal.Copy(facePoints_ptr, FacePoints, 0, (int)FacePointCount * 2);

        for (int i = 1; i <= 16; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.red);

        for (int i = 28; i <= 30; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.green);

        for (int i = 18; i <= 21; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.black);
        for (int i = 23; i <= 26; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.black);
        for (int i = 31; i <= 35; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.blue);
        drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[30 * 2], offsetY + FacePoints[30 * 2 + 1]), new Vector2(offsetX + FacePoints[35 * 2], offsetY + FacePoints[35 * 2 + 1]), UnityEngine.Color.blue);

        for (int i = 37; i <= 41; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.green);
        drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[36 * 2], offsetY + FacePoints[36 * 2 + 1]), new Vector2(offsetX + FacePoints[41 * 2], offsetY + FacePoints[41 * 2 + 1]), UnityEngine.Color.green);

        for (int i = 43; i <= 47; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.green);
        drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[42 * 2], offsetY + FacePoints[42 * 2 + 1]), new Vector2(offsetX + FacePoints[47 * 2], offsetY + FacePoints[47 * 2 + 1]), UnityEngine.Color.green);

        for (int i = 49; i <= 59; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.green);
        drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[48 * 2], offsetY + FacePoints[48 * 2 + 1]), new Vector2(offsetX + FacePoints[59 * 2], offsetY + FacePoints[59 * 2 + 1]), UnityEngine.Color.green);

        for (int i = 61; i <= 67; ++i)
            drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[i * 2], offsetY + FacePoints[i * 2 + 1]), new Vector2(offsetX + FacePoints[(i - 1) * 2], offsetY + FacePoints[(i - 1) * 2 + 1]), UnityEngine.Color.green);
        drawingTexture.toDraw(width, height, new Vector2(offsetX + FacePoints[60 * 2], offsetY + FacePoints[60 * 2 + 1]), new Vector2(offsetX + FacePoints[67 * 2], offsetY + FacePoints[67 * 2 + 1]), UnityEngine.Color.green);

        testByteArray = getSlicedByteArray(drawingTexture, faceBox, width, height);

        if (!useDlibFaceDetector)
        {
            renderMask(testByteArray, (int)faceBox.width, (int)faceBox.height, FacePoints, (int)offsetX, (int)offsetY);
            //_SlicedTexture = new Texture2D((int)faceBox.width, (int)faceBox.height, TextureFormat.RGBA32, false);
            //_SlicedTexture.LoadRawTextureData(testByteArray);
            //_SlicedTexture.Apply();
        }

        _SlicedTexture = new Texture2D((int)faceBox.width, (int)faceBox.height, TextureFormat.RGBA32, false);
        _SlicedTexture.LoadRawTextureData(testByteArray);
        _SlicedTexture.Apply();
    }

    //CheckInside

    bool InsidePolygon(Vector2[] polygon, int n, Vector2 p)
    {
        int i;
        double angle = 0;
        Vector2 p1, p2;

        for (i = 0; i < n; i++)
        {
            p1 = polygon[i] - p;
            p2 = polygon[(i + 1) % n] - p;
            angle += Vector2.Angle(p1, p2);// Angle2D(p1.x, p1.y, p2.x, p2.y);
        }

        if (Math.Abs(angle) < 359)
            return false;
        else
            return true;
    }
    

    byte[] createMask(byte[] src,int slicedWidth,int slicedHeight, int[] points,int offsetX,int offsetY) {
        //回傳結果
        byte[] result = new byte[slicedWidth * slicedHeight];
        //把color上的每一個點對應到深度資訊的點

        /*
        ushort[] depths = new ushort[51];

        //五官
        ColorSpacePoint[] Features = new ColorSpacePoint[51];

        for (int i = 17; i <= 67; ++i)
        {
            Features[i - 17] = new ColorSpacePoint();
            Features[i - 17].X = points[2 * i];
            Features[i - 17].Y = points[2 * i + 1];
        }
        //取得五官深度
        for (int i = 0; i < Features.Length; i++) {

            ushort depth = _MultiSourceManager.GetDepthDataAt(offsetX + (int)Features[i].X, offsetY + (int)Features[i].Y);
            depths[i] = depth;
            print("depth of Features[" + i + "] = " + depth);
        }
        */
        /*
        ushort[] depths = new ushort[6];

        //只做鼻子
        ColorSpacePoint[] Features = new ColorSpacePoint[6];

        for (int i = 30; i <= 35; ++i)
        {
            Features[i - 30] = new ColorSpacePoint();
            Features[i - 30].X = points[2 * i];
            Features[i - 30].Y = points[2 * i + 1];
        }
        //取得五官深度
        for (int i = 0; i < Features.Length; i++)
        {

            ushort depth = _DepthSourceManager.GetDepthDataAt(offsetX + (int)Features[i].X, offsetY + (int)Features[i].Y,displayWidth);
            depths[i] = depth;
            print("depth of Features[" + i + "] = " + depth);
        }


        
        ushort DepthMax = ushort.MinValue;
        ushort DepthMin = ushort.MaxValue;

        for (int i = 0; i < Features.Length; i++)
        {
            
            if (depths[i] > DepthMax) {
                DepthMax = depths[i];
            }else if (depths[i] < DepthMin)
            {
                DepthMin = depths[i];
            }
            
        }

        print("DepthMax : " + DepthMax);
       
        //sliced
        for (int i = 0; i < slicedHeight; i++)
        {
            for (int j = 0; j < slicedWidth; j++)
            {
                ushort depth = _DepthSourceManager.GetDepthDataAt(offsetX + i, offsetY + j, displayWidth);

                if (depth <= DepthMax && depth >= DepthMin)
                {
                    result[i * slicedWidth + j] = 0;
                }
                else {
                    result[i * slicedWidth + j] = 2;
                }
            }
        }
        */
        /*
        //臉部輪廓
        Vector2[] FaceFeatures = new Vector2[17];

        for (int i = 0; i <= 16; ++i)
        {
            FaceFeatures[i] = new Vector2(points[2 * i],points[2 * i + 1]);
        }

        for (int i = 0; i < slicedHeight; i++)
        {
            for (int j = 0; j < slicedWidth; j++)
            {
                if(InsidePolygon(FaceFeatures,17,new Vector2(j,i))) { 
                    result[i * slicedWidth + j] = (byte)GrabCutClasses.GC_FGD;
                }
                else
                {
                    result[i * slicedWidth + j] = (byte)GrabCutClasses.GC_PR_BGD;
                }
            }
        }
        */
        return result;
    }

    void renderMask(byte[] texture,int slicedWidth,int slicedHeight, int[] points,int offsetX,int offsetY) {
        byte[] mask = createMask(texture, slicedWidth, slicedHeight, points, offsetX, offsetY);

        

        for (int i = 0; i < slicedHeight; i++)
        {
            for (int j = 0; j < slicedWidth; j++)
            {
                if (mask[i * slicedWidth + j] == (byte)GrabCutClasses.GC_FGD)
                {
                    texture[i * slicedWidth * 4 + j * 4] = 0;
                    texture[i * slicedWidth * 4 + j * 4 + 1] = 0;
                    texture[i * slicedWidth * 4 + j * 4 + 2] = 255;
                    texture[i * slicedWidth * 4 + j * 4 + 3] = 255;
                }
                else if (mask[i * slicedWidth + j] == (byte)GrabCutClasses.GC_PR_FGD) {
                    texture[i * slicedWidth * 4 + j * 4] = 0;
                    texture[i * slicedWidth * 4 + j * 4 + 1] = 255;
                    texture[i * slicedWidth * 4 + j * 4 + 2] = 0;
                    texture[i * slicedWidth * 4 + j * 4 + 3] = 255;
                }
            }
        }

        //graph_cut(texture, slicedWidth, slicedHeight, mask);
    }

    byte[] getSlicedByteArray(byte[] drawingTexture, Rect box, int width, int height) {

        int sWidth = (int)box.width;
        int sHeight = (int)box.height;

        int leftPoint = (int)box.xMin;
        int rightPoint = (int)box.xMax;
        int topPoint = (int)box.yMin;
        int bottomPoint = (int)box.yMax;

        byte[] resultByteArray = new byte[sWidth * sHeight * 4];
        

        for (int i = topPoint, ii = 0; i < bottomPoint && ii < sHeight; i++,ii++) {
            for (int j = leftPoint, jj = 0; j < rightPoint && jj < sWidth; j++,jj++)
            {
                resultByteArray[ii * sWidth * 4 + jj * 4] = drawingTexture[i * width * 4 + j * 4];
                resultByteArray[ii * sWidth * 4 + jj * 4 + 1] = drawingTexture[i * width * 4 + j * 4 + 1];
                resultByteArray[ii * sWidth * 4 + jj * 4 + 2] = drawingTexture[i * width * 4 + j * 4 + 2];
                resultByteArray[ii * sWidth * 4 + jj * 4 + 3] = drawingTexture[i * width * 4 + j * 4 + 3];
            }
        }
        
        return resultByteArray;
    }

    void DrawRectangle(byte[] textureBytes, int width, int height, Rect box)
    {
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
        pitchD = (float)(Mathf.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Mathf.PI * 180.0);
        yawD = (float)(Mathf.Asin(2 * ((w * y) - (x * z))) / Mathf.PI * 180.0);
        rollD = (float)(Mathf.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Mathf.PI * 180.0);

        // clamp the values to a multiple of the specified increment to control the refresh rate
        float increment = FaceRotationIncrementInDegrees;
        pitch = (int)(Mathf.Floor((pitchD + ((increment / 2.0f) * (pitchD > 0 ? 1.0f : -1.0f))) / increment) * increment);
        yaw = (int)(Mathf.Floor((yawD + ((increment / 2.0f) * (yawD > 0 ? 1.0f : -1.0f))) / increment) * increment);
        roll = (int)(Mathf.Floor((rollD + ((increment / 2.0f) * (rollD > 0 ? 1.0f : -1.0f))) / increment) * increment);
    }
}
