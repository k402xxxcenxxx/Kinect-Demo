using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;

public class bodyTest : MonoBehaviour {
    /*
    GameObject myAvatar;
    GameObject myCamera;
    //左肩右肩
    public GameObject ShoulderLeftObj;
    public GameObject ShoulderRightObj;

    //左肘右肘
    public GameObject ElbowLeftObj;
    public GameObject ElbowRightObj;

    //左腕右腕
    public GameObject WristLeftObj;
    public GameObject WristRightObj;

    public GameObject Base;

    float time = 0;
    float leftShouldertoElbowTotal = 0;
    float leftShouldertoElbowCount = 0;

    float leftElbowtoWristTotal = 0;
    float leftElbowtoWristCount = 0;

    float rightShouldertoElbowTotal = 0;
    float rightShouldertoElbowCount = 0;

    float rightElbowtoWristTotal = 0;
    float rightElbowtoWristCount = 0;

    // Use this for initialization
    void Start () {
        //左肩右肩
        ShoulderLeftObj = GameObject.Find(Kinect.JointType.ShoulderLeft.ToString());
        ShoulderRightObj = GameObject.Find(Kinect.JointType.ShoulderRight.ToString());

        //左肘右肘
        ElbowLeftObj = GameObject.Find(Kinect.JointType.ElbowLeft.ToString());
        ElbowRightObj = GameObject.Find(Kinect.JointType.ElbowRight.ToString());

        //左腕右腕
        WristLeftObj = GameObject.Find(Kinect.JointType.WristLeft.ToString());
        WristRightObj = GameObject.Find(Kinect.JointType.WristRight.ToString());

        myAvatar = GameObject.Find("unitychan");
        myCamera = GameObject.Find("Main Camera");

        Base = GameObject.Find(Kinect.JointType.SpineBase.ToString());
    }

    // Update is called once per frame
    void Update () {
        time += 1;
        if (ShoulderLeftObj == null) {
            ShoulderLeftObj = GameObject.Find(Kinect.JointType.ShoulderLeft.ToString());
            return;
        }

        if (ShoulderRightObj == null)
        {
            ShoulderRightObj = GameObject.Find(Kinect.JointType.ShoulderRight.ToString());
            return;
        }

        if (ElbowLeftObj == null)
        {
            ElbowLeftObj = GameObject.Find(Kinect.JointType.ElbowLeft.ToString());
            return;
        }

        if (ElbowRightObj == null)
        {
            ElbowRightObj = GameObject.Find(Kinect.JointType.ElbowRight.ToString());
            return;
        }

        if (WristLeftObj == null)
        {
            WristLeftObj = GameObject.Find(Kinect.JointType.WristLeft.ToString());
            return;
        }

        if (WristRightObj == null)
        {
            WristRightObj = GameObject.Find(Kinect.JointType.WristRight.ToString());
            return;
        }

        if (leftElbowtoWristCount < 60)
        {

            Vector3 leftShouldertoElbowV = (ShoulderLeftObj.transform.position - ElbowLeftObj.transform.position);
            Vector3 leftElbowtoWristV = (ElbowLeftObj.transform.position - WristLeftObj.transform.position);

            Vector3 rightShouldertoElbowV = (ShoulderRightObj.transform.position - ElbowRightObj.transform.position);
            Vector3 rightElbowtoWristV = (ElbowRightObj.transform.position - WristRightObj.transform.position);

            //左肩到左肘左右平伸
            if (Mathf.Abs((leftShouldertoElbowV).y) < 0.5 && Mathf.Abs((leftShouldertoElbowV).z) < 0.5)
            {
                //左肘到左腕上下平舉
                if (Mathf.Abs((leftElbowtoWristV).x) < 0.5 && Mathf.Abs((leftElbowtoWristV).z) < 0.5)
                {

                    //右肩到右肘左右平伸
                    if (Mathf.Abs((rightShouldertoElbowV).y) < 0.5 && Mathf.Abs((rightShouldertoElbowV).z) < 0.5)
                    {
                        //右肘到右腕上下平舉
                        if (Mathf.Abs((rightElbowtoWristV).x) < 0.5 && Mathf.Abs((rightElbowtoWristV).z) < 0.5)
                        {

                            myCamera.GetComponent<Camera>().backgroundColor = Color.green;

                            //左肩到左肘長度
                            float leftShouldertoElbowlength = Mathf.Abs(leftShouldertoElbowV.x);
                            float leftElbowtoWristlength = Mathf.Abs(leftElbowtoWristV.y);
                            leftShouldertoElbowTotal += leftShouldertoElbowlength;
                            leftShouldertoElbowCount++;

                            //左肩到左肘長度
                            float rightShouldertoElbowlength = Mathf.Abs(rightShouldertoElbowV.x);
                            float rightElbowtoWristlength = Mathf.Abs(rightElbowtoWristV.y);
                            rightShouldertoElbowTotal += rightShouldertoElbowlength;
                            rightShouldertoElbowCount++;

                            leftElbowtoWristTotal += leftElbowtoWristlength;
                            leftElbowtoWristCount++;

                            rightElbowtoWristTotal += rightElbowtoWristlength;
                            rightElbowtoWristCount++;

                            if (leftElbowtoWristCount % 60 == 0)
                            {
                                Debug.Log("TIMES = [" + leftElbowtoWristCount + "]============================================================");
                                Debug.Log("average length leftShouldertoElbow = " + leftShouldertoElbowTotal / leftShouldertoElbowCount);
                                Debug.Log("average length leftElbowtoWrist = " + leftElbowtoWristTotal / leftElbowtoWristCount);
                                Debug.Log("average length rightShouldertoElbow = " + rightShouldertoElbowTotal / rightShouldertoElbowCount);
                                Debug.Log("average length rightElbowtoWrist = " + rightElbowtoWristTotal / rightElbowtoWristCount);
                                Debug.Log("==================================================================================================");
                            }
                        }
                    }
                }
            }
            else
            {
                myCamera.GetComponent<Camera>().backgroundColor = Color.blue;
            }
        }
        else {
            myCamera.GetComponent<Camera>().backgroundColor = Color.yellow;
            myAvatar.transform.parent = this.transform;
            myAvatar.GetComponent<Transform>().localPosition = Base.GetComponent<Transform>().position;
        }
            
    }
    */

    public GameObject BodySourceManager;
    public GameObject ColorSourceManager;

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;
    private ColorSourceManager _ColorManager;

    private Dictionary<JointType, JointType> _BoneMap = new Dictionary<JointType, JointType>()
    {
        { JointType.FootLeft, JointType.AnkleLeft },
        { JointType.AnkleLeft, JointType.KneeLeft },
        { JointType.KneeLeft, JointType.HipLeft },
        { JointType.HipLeft, JointType.SpineBase },

        { JointType.FootRight, JointType.AnkleRight },
        { JointType.AnkleRight, JointType.KneeRight },
        { JointType.KneeRight, JointType.HipRight },
        { JointType.HipRight, JointType.SpineBase },

        { JointType.HandTipLeft, JointType.HandLeft },
        { JointType.ThumbLeft, JointType.HandLeft },
        { JointType.HandLeft, JointType.WristLeft },
        { JointType.WristLeft, JointType.ElbowLeft },
        { JointType.ElbowLeft, JointType.ShoulderLeft },
        { JointType.ShoulderLeft, JointType.SpineShoulder },

        { JointType.HandTipRight, JointType.HandRight },
        { JointType.ThumbRight, JointType.HandRight },
        { JointType.HandRight, JointType.WristRight },
        { JointType.WristRight, JointType.ElbowRight },
        { JointType.ElbowRight, JointType.ShoulderRight },
        { JointType.ShoulderRight, JointType.SpineShoulder },

        { JointType.SpineBase, JointType.SpineMid },
        { JointType.SpineMid, JointType.SpineShoulder },
        { JointType.SpineShoulder, JointType.Neck },
        { JointType.Neck, JointType.Head },
    };
    
    Texture2D _Texture;
    private byte[] _Data;

    int ColorWidth;
    int ColorHeight;
    private ColorFrameReader _Reader;
    CoordinateMapper mapper;
    private KinectSensor kinectSensor = null;

    Vector2[] JointData;

    void Start() {
        kinectSensor = KinectSensor.GetDefault();
        JointData = new Vector2[25];
        if (kinectSensor != null)
        {
            mapper = kinectSensor.CoordinateMapper;
            _Reader = kinectSensor.ColorFrameSource.OpenReader();

            var frameDesc = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;

            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
        }

        if (!kinectSensor.IsOpen) {
            kinectSensor.Open();
        }

    }

    void Update() {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();

            if (frame != null)
            {
                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);

                frame.Dispose();
                frame = null;
            }
        }

        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                trackedIds.Add(body.TrackingId);
            }
        }

        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                renderJointOnColorFrame(body);
            }
        }
        
        
    }

    void renderJointOnColorFrame(Body body) {

        print("======================================================================================");
        for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
        {
            Windows.Kinect.Joint sourceJoint = body.Joints[jt];
            CameraSpacePoint sourceJoint_CameraPoint = sourceJoint.Position;
            ColorSpacePoint sourceJoint__ColorPoint = mapper.MapCameraPointToColorSpace(sourceJoint_CameraPoint);

            Windows.Kinect.Joint? targetJoint = null;

            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            if (targetJoint.HasValue)
            {
                CameraSpacePoint targetJoint_CameraPoint = targetJoint.Value.Position;
                ColorSpacePoint targetJoint__ColorPoint = mapper.MapCameraPointToColorSpace(targetJoint_CameraPoint);

                int srcPointX = (int)sourceJoint__ColorPoint.X;
                if (srcPointX >= ColorWidth) {
                    srcPointX = ColorWidth - 1;
                } else if (srcPointX <= 0) {
                    srcPointX = 1;
                }

                int tarPointX = (int)targetJoint__ColorPoint.X;
                if (tarPointX >= ColorWidth)
                {
                    tarPointX = ColorWidth - 1;
                }
                else if (tarPointX <= 0)
                {
                    tarPointX = 1;
                }

                int srcPointY = (int)sourceJoint__ColorPoint.Y;
                if (srcPointY >= ColorHeight)
                {
                    srcPointY = ColorHeight - 1;
                }
                else if (srcPointY <= 0) {
                    srcPointY = 0;
                }

                int tarPointY = (int)targetJoint__ColorPoint.Y;
                if (tarPointY >= ColorHeight)
                {
                    tarPointY = ColorHeight - 1;
                }
                else if (tarPointY <= 0)
                {
                    tarPointY = 0;
                }

                _Data.toDraw(ColorWidth, ColorHeight, new Vector2(srcPointX, srcPointY), new Vector2(tarPointX, tarPointY), Color.red);

                JointData[(int)jt] = new Vector2(srcPointX, srcPointY);
            }
        }

        int leftPoint = ColorWidth - 1;
        int rightPoint = 1;
        int topPoint = ColorHeight - 1;
        int bottomPoint = 1;

        for(int i = 0; i < JointData.Length; i++)
        {
            if (JointData[i].x > rightPoint)
            {
                rightPoint = (int)JointData[i].x;
            }

            if (JointData[i].x < leftPoint)
            {
                leftPoint = (int)JointData[i].x;
            }

            if (JointData[i].y > bottomPoint)
            {
                bottomPoint = (int)JointData[i].y;
            }

            if (JointData[i].y < topPoint)
            {
                topPoint = (int)JointData[i].y;
            }
        }

        _Data.toDraw(ColorWidth, ColorHeight, new Vector2(leftPoint, topPoint), new Vector2(rightPoint, topPoint), Color.blue);
        _Data.toDraw(ColorWidth, ColorHeight, new Vector2(leftPoint, bottomPoint), new Vector2(rightPoint, bottomPoint), Color.blue);
        _Data.toDraw(ColorWidth, ColorHeight, new Vector2(leftPoint, topPoint), new Vector2(leftPoint, bottomPoint), Color.blue);
        _Data.toDraw(ColorWidth, ColorHeight, new Vector2(rightPoint, topPoint), new Vector2(rightPoint, bottomPoint), Color.blue);


        _Texture.LoadRawTextureData(_Data);
        _Texture.Apply();

    }

    public Texture2D GetTestTexture() {
        
        return _Texture;
    }
}
