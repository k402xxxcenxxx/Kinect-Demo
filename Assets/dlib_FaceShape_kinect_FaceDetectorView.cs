using UnityEngine;
using System.Collections;

public class dlib_FaceShape_kinect_FaceDetectorView : MonoBehaviour {

    public GameObject dlib_FaceShape_kinect_FaceDetectorManager;
    private dlib_FaceShape_kinect_FaceDetectorManager _dlib_FaceShape_kinect_FaceDetectorManager;

    public bool isSliced = false;
    public bool isDepth = false;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (dlib_FaceShape_kinect_FaceDetectorManager == null)
        {
            return;
        }

        _dlib_FaceShape_kinect_FaceDetectorManager = dlib_FaceShape_kinect_FaceDetectorManager.GetComponent<dlib_FaceShape_kinect_FaceDetectorManager>();
        if (_dlib_FaceShape_kinect_FaceDetectorManager == null)
        {
            return;
        }
        if(!isSliced && !isDepth)
            gameObject.GetComponent<Renderer>().material.mainTexture = _dlib_FaceShape_kinect_FaceDetectorManager.GetColorTexture();
        else if(isSliced && !isDepth)
            gameObject.GetComponent<Renderer>().material.mainTexture = _dlib_FaceShape_kinect_FaceDetectorManager.GetSlicedColorTexture();
    }
}
