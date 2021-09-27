using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowing : MonoBehaviour
{
    public GameObject targetObject;
    public GameObject cameraObject;
    public bool activeSmoothing = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(targetObject != null && cameraObject != null)
        {
            var cameraPos = cameraObject.transform.position;
            var mePos = targetObject.transform.position;

            var newTargetPos = new Vector3(mePos.x, cameraPos.y, mePos.z);
            if (Vector3.Distance(cameraPos, newTargetPos) < 0.02f || !activeSmoothing)
            {
                cameraObject.transform.position = newTargetPos;
                activeSmoothing = false;
            }
            else
            {
                cameraObject.transform.position = cameraPos * 0.85f + newTargetPos * 0.15f;
            }
        }
    }
}
