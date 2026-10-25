using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCamera2D : MonoBehaviour
{
    [Header("Settings")]
    public int pixelsPerUnit = 16;          // Match your sprite PPU
    public int referenceHeight = 180;       // Game resolution height in pixels (e.g., 180 for 320x180)

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyPixelPerfectSettings();
    }

    void ApplyPixelPerfectSettings()
    {
        // Calculate orthographic size to fit the reference height
        cam.orthographic = true;
        cam.orthographicSize = referenceHeight / (pixelsPerUnit * 2f);
    }

    void LateUpdate()
    {
        // Snap camera position to pixel grid
        float unitsPerPixel = 1f / pixelsPerUnit;
        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Round(pos.x / unitsPerPixel) * unitsPerPixel;
        pos.y = Mathf.Round(pos.y / unitsPerPixel) * unitsPerPixel;
        cam.transform.position = pos;
    }
}
