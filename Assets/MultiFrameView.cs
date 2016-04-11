using UnityEngine;
using System.Collections;

public class MultiFrameView : MonoBehaviour {

    GameObject MultiSourceManager;
    MultiSourceManager _MultiSourceManager;

    public MultiSourceViewType Type;

    public enum MultiSourceViewType
    {
        COLOR,
        DEPTH
    }
        
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    // Update is called once per frame
    void Update () {
        if (MultiSourceManager == null)
        {
            return;
        }

        _MultiSourceManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiSourceManager == null)
        {
            return;
        }

        switch (Type) {
            case MultiSourceViewType.COLOR:
                
                    gameObject.GetComponent<Renderer>().material.mainTexture = _MultiSourceManager.GetColorTexture();

                break;
        }
	}
}
