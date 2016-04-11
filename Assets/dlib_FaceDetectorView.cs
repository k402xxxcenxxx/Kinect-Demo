using UnityEngine;
using System.Collections;

public class dlib_FaceDetectorView : MonoBehaviour {
    public GameObject dlib_FaceDetectorManager;
    private dlib_FaceDetectorManager _dlib_FaceDetectorManager;
    public bool isSliced = false;
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (dlib_FaceDetectorManager == null)
        {
            return;
        }

        _dlib_FaceDetectorManager = dlib_FaceDetectorManager.GetComponent<dlib_FaceDetectorManager>();
        if (_dlib_FaceDetectorManager == null)
        {
            return;
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = _dlib_FaceDetectorManager.GetColorTexture();
    }
}
