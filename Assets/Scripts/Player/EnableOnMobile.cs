using UnityEngine;

public class EnableOnMobile : MonoBehaviour
{
    [Tooltip("The GameObject to enable if running on a mobile platform.")]
    public GameObject targetObject;

    void Start()
    {
        if (targetObject == null) return;

        // Check if running on mobile
        if (Application.isMobilePlatform && Application.platform!=RuntimePlatform.WebGLPlayer)
        {
            targetObject.SetActive(true);
        }
        else
        {
            targetObject.SetActive(false);
        }
    }
}
