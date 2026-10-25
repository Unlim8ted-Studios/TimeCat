using UnityEngine;
using System.Collections;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;                      // The object to follow
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Keep Z -10 for 2D
    public float smoothSpeed = 0.125f;            // Smoothness of the camera follow
    public bool followX = true;                   // Toggle X-axis following
    public bool followY = true;                   // Toggle Y-axis following
    private Camera cam;

    Coroutine zoomRoutine;
    void Awake(){
        cam = GetComponent<Camera>();
    }

    public void enableYFollow(){
        followY = true;
    }
    public void disableYFollow(){
        followY = false;
    }
    public void enableXFollow(){
        followX = true;
    } 
    public void disableXFollow(){
        followX = false;
    }
    public void setNewTarget(Transform newTarget){
        target = newTarget;
    }
    public void setSmoothSpeed(float newSmoothSpeed){
        smoothSpeed = newSmoothSpeed;
    }
    public void setNewSize(float newCamSize)
    {
        if(!cam)cam=GetComponent<Camera>();
        cam.orthographicSize = Mathf.Max(.1f,newCamSize);
    }

    public void slowlySetSize(float newCamSize){
        if(!cam)cam=GetComponent<Camera>();
        if(zoomRoutine!=null)StopCoroutine(zoomRoutine);
        zoomRoutine=StartCoroutine(ZoomTo(Mathf.Max(.1f,newCamSize)));
    }
    IEnumerator ZoomTo(float newCamSize)
{
    while (Mathf.Abs(cam.orthographicSize - newCamSize) > 0.01f)
    {
        cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, newCamSize, Time.deltaTime * 5f);

        yield return null;
    }

    cam.orthographicSize = newCamSize; // Snap to final value
}

    void LateUpdate()
    {
        if (target == null || (GetComponent<CameraFollow>() && GetComponent<CameraFollow>().enabled) || (GetComponent<Cinemachine.CinemachineBrain>() && GetComponent<Cinemachine.CinemachineBrain>().enabled))
            return;

        Vector3 desiredPosition = transform.position;

        if (followX)
            desiredPosition.x = target.position.x + offset.x;

        if (followY)
            desiredPosition.y = target.position.y + offset.y;

        desiredPosition.z = offset.z; // Always keep the Z offset

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, 1-Mathf.Pow(1-Mathf.Clamp01(smoothSpeed),Time.deltaTime*60));
        transform.position = smoothedPosition;
    }
}
