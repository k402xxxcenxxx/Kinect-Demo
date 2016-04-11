using UnityEngine;
using System.Collections;

public class kinect_bodyIndexView : MonoBehaviour {

    public GameObject kinect_bodyIndexManager;
    private kinect_bodyIndexManager _kinect_bodyIndexManager;
    
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (kinect_bodyIndexManager == null)
        {
            return;
        }

        _kinect_bodyIndexManager = kinect_bodyIndexManager.GetComponent<kinect_bodyIndexManager>();
        if (_kinect_bodyIndexManager == null)
        {
            return;
        }
        gameObject.GetComponent<Renderer>().material.mainTexture = _kinect_bodyIndexManager.GetColorTexture();
    }
}
