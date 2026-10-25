using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ForceAspectRatio : MonoBehaviour
{
    public float targetWidth = 39f;
    public float targetHeight = 20f;

    void Start()
    {
        EnforceAspect();
    }

    
    void EnforceAspect()
    {
        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = GetComponent<Camera>();

        if (scaleHeight < 1.0f)
        {
            // Letterbox (black bars top and bottom)
            Rect rect = new Rect(0, (1.0f - scaleHeight) / 2.0f, 1.0f, scaleHeight);
            cam.rect = rect;
        }
        else
        {
            // Pillarbox (black bars left and right)
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = new Rect((1.0f - scaleWidth) / 2.0f, 0, scaleWidth, 1.0f);
            cam.rect = rect;
        }
    }
    void Update()
{
    EnforceAspect(); // Caution: only do this during development if needed
}

    // Camera clearing only covers its viewport. Repaint the bars so old overlay pixels cannot persist.
    void OnGUI()
    {
        var cam = GetComponent<Camera>();
        if (!cam.enabled || cam.targetTexture != null || Event.current.type != EventType.Repaint) return;
        Rect pixels = cam.pixelRect;
        float top = Screen.height - pixels.yMax;
        Color previous = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = 10000;
        GUI.color = Color.black;
        if (top > 0) GUI.DrawTexture(new Rect(0, 0, Screen.width, top), Texture2D.whiteTexture);
        if (pixels.y > 0) GUI.DrawTexture(new Rect(0, Screen.height - pixels.y, Screen.width, pixels.y), Texture2D.whiteTexture);
        if (pixels.x > 0) GUI.DrawTexture(new Rect(0, top, pixels.x, pixels.height), Texture2D.whiteTexture);
        if (pixels.xMax < Screen.width) GUI.DrawTexture(new Rect(pixels.xMax, top, Screen.width - pixels.xMax, pixels.height), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.depth = previousDepth;
    }

}
