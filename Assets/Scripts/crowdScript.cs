using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class crowdScript : MonoBehaviour
{
    int m = 100, n = 200;
    MaterialPropertyBlock mPB;
    SpriteRenderer sr;
    Transform[] transChildren;
    bool[] boolChildren;
    int[] frameChildren;
    int frame = 0, childCount;
    float height = 0.3f;
    // Start is called before the first frame update
    void Start()
    {
        childCount = transform.childCount;
        transChildren = new Transform[childCount];
        boolChildren = new bool[childCount];
        frameChildren = new int[childCount];
        mPB = new MaterialPropertyBlock();
        float t,r,g,b; int i = 0;
        foreach(Transform child in transform){
            transChildren[i] = child;
            boolChildren[i] = false;
            frameChildren[i] = Random.Range(0, 50);
            t = Random.Range(m,n); r = Random.Range(m,n); g = Random.Range(m,n); b = Random.Range(m,n);
            sr = child.gameObject.GetComponent<SpriteRenderer>();
            sr.GetPropertyBlock(mPB);
            mPB.SetColor("_Color", new Color(r/255f,g/255f,b/255f));
            sr.SetPropertyBlock(mPB);
            i+=1;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        for(int i = 0; i < childCount; i++){
            if(frameChildren[i] == frame){
                frameChildren[i] = Random.Range(0,50);
                boolChildren[i] = !boolChildren[i];

                transChildren[i].position+=new Vector3(0,(boolChildren[i]?-1:1)*height,0);
            }
        }
        
        frame+=1;
        frame%=50;
    }
}
