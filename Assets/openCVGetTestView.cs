using UnityEngine;
using System.Collections;

public class openCVGetTestView : MonoBehaviour {

    public GameObject openCVGetTestManager;
    private openCVGetTest _openCVGetTestManager;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (openCVGetTestManager == null)
        {
            return;
        }

        _openCVGetTestManager = openCVGetTestManager.GetComponent<openCVGetTest>();
        if (_openCVGetTestManager == null)
        {
            return;
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = _openCVGetTestManager.GetColorTexture();
        //gameObject.GetComponent<Renderer>().material.mainTexture = _openCVGetTestManager.GetslicedTexture();
    }
}
