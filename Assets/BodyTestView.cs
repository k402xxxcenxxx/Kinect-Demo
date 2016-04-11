using UnityEngine;
using System.Collections;

public class BodyTestView : MonoBehaviour {

    public GameObject bodyTest;
    private bodyTest _bodyTest;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (bodyTest == null)
        {
            return;
        }

        _bodyTest = bodyTest.GetComponent<bodyTest>();
        if (_bodyTest == null)
        {
            return;
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = _bodyTest.GetTestTexture();
    }
}
