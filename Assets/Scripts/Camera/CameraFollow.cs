using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Camera Bounds")]
    public Vector3[] boundsPoints;

    [Header("Smooth Settings")]
    public float smoothTime = 0.2f;

    public Camera cam;

    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if(player && cam)FollowPlayer();
    }

    private void FollowPlayer()
    {
        if (boundsPoints == null || boundsPoints.Length < 3)
        {
            Debug.LogError("boundsPoints array must have at least 3 points.");
            return;
        }

        Vector3 desiredPosition = player.position + offset;

        // Adjust for camera size
        float cameraHeight = 2f * cam.orthographicSize;
        float cameraWidth = cameraHeight * cam.aspect;

        // Clamp the camera center to remain within the polygon
        Vector3 constrainedPosition = ClampToPolygon(desiredPosition, cameraWidth, cameraHeight);

        // Smooth damp instead of Lerp
        transform.position = Vector3.SmoothDamp(transform.position, constrainedPosition, ref velocity, smoothTime);
    }

    private Vector3 ClampToPolygon(Vector3 position, float camWidth, float camHeight)
    {
        Vector2 camCenter2D = new Vector2(position.x, position.y);
        Vector2[] polyPoints = new Vector2[boundsPoints.Length];
        for (int i = 0; i < boundsPoints.Length; i++)
            polyPoints[i] = new Vector2(boundsPoints[i].x, boundsPoints[i].y);

        if (IsPointInPolygon(camCenter2D, polyPoints))
            return position;

        // If outside, clamp to closest edge
        Vector2 closest = camCenter2D;
        float closestDist = float.MaxValue;

        for (int i = 0; i < polyPoints.Length; i++)
        {
            Vector2 a = polyPoints[i];
            Vector2 b = polyPoints[(i + 1) % polyPoints.Length];

            Vector2 clamped = ClosestPointOnLine(a, b, camCenter2D);
            float dist = (clamped - camCenter2D).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = clamped;
            }
        }

        return new Vector3(closest.x, closest.y, position.z);
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int crossings = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];

            if ((a.y > point.y) != (b.y > point.y))
            {
                float t = (point.y - a.y) / (b.y - a.y);
                if (point.x < a.x + t * (b.x - a.x))
                    crossings++;
            }
        }
        return crossings % 2 == 1;
    }

    private Vector2 ClosestPointOnLine(Vector2 a, Vector2 b, Vector2 point)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    private void OnDrawGizmosSelected()
    {
        if (boundsPoints == null || boundsPoints.Length < 3)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < boundsPoints.Length; i++)
        {
            Vector3 current = boundsPoints[i];
            Vector3 next = boundsPoints[(i + 1) % boundsPoints.Length];
            Gizmos.DrawLine(current, next);
        }
    }
}
