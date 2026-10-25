using UnityEngine;

/// <summary>
/// Simple 2D Parallax script.
/// Attach to background/foreground objects.
/// </summary>

public class Parallax2D : MonoBehaviour
{
    public enum LayerType { Background, Midground, Foreground }

    [Header("Parallax Settings")]
    public LayerType layerType = LayerType.Background;

    [Tooltip("Reference to the camera (defaults to main camera).")]
    public Transform cameraTransform;
    public Vector3 offset;

    [Tooltip("How much the layer should move relative to the camera.")]
    [Range(-2f, 2f)] public float parallaxStrength = 0.5f;

    [Tooltip("If true, parallax will not be applied on the Y axis.")]
    public bool ignoreY = false;

    private Vector3 startPos;
    private Vector3 camStartPos;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        startPos = transform.position;
        camStartPos = cameraTransform.position;

        // Default strengths based on type if user hasn’t set
        if (Mathf.Approximately(parallaxStrength, 0f))
        {
            switch (layerType)
            {
                case LayerType.Background: parallaxStrength = 0.3f; break;
                case LayerType.Midground:  parallaxStrength = 0.6f; break;
                case LayerType.Foreground: parallaxStrength = 1.2f; break;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 camDelta = cameraTransform.position - camStartPos;

        if (ignoreY)
            camDelta.y = 0f;

        // Move this layer relative to camera movement
        transform.position = startPos + camDelta * parallaxStrength + offset;
    }
}
