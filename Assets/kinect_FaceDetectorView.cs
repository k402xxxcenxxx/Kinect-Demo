using UnityEngine;
using System.Collections;

public class kinect_FaceDetectorView : MonoBehaviour {

    public GameObject kinect_FaceDetectorManager;
    private kinect_FaceDetectorManager _kinect_FaceDetectorManager;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (kinect_FaceDetectorManager == null)
        {
            return;
        }

        _kinect_FaceDetectorManager = kinect_FaceDetectorManager.GetComponent<kinect_FaceDetectorManager>();
        if (_kinect_FaceDetectorManager == null)
        {
            return;
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = _kinect_FaceDetectorManager.GetColorTexture();
    }
}
