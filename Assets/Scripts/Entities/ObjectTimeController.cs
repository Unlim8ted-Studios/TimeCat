using UnityEngine;

public class ObjectTimeController : MonoBehaviour
{
    public Transform pastobject; // The object to follow
    public LayerMask groundLayer; // Layers considered as ground
    public float maxFallDistance = 100f; // How far to search for ground

    public enum FollowMode { GroundedXFollow, ExactFollow }
    public FollowMode currentMode = FollowMode.GroundedXFollow;

    private TimeTravelCameraSwitcher TimeController;

    private Vector3 startingPosition;

    private bool isInPresent;

    void Start()
    {
        startingPosition = transform.position;

        TimeController = GameObject.Find("TimeManager").GetComponent<TimeTravelCameraSwitcher>();

        isInPresent = TimeController.getIsInPresent();
    }

    void Update()
    {

        isInPresent = TimeController.getIsInPresent();

        

        if (pastobject == null) return;

        // Only follow logic when in the past (i.e., isPresent is false)
        if (!isInPresent)
        {
            switch (currentMode)
            {
                case FollowMode.GroundedXFollow:
                    FollowTransformWithGroundConstraint();
                    break;

                case FollowMode.ExactFollow:
                    FollowExactTransform();
                    break;
            }
        }
    }

    public void SetGroundedXFollow()
    {
        currentMode = FollowMode.GroundedXFollow;
    }

    public void SetExactFollow()
    {
        currentMode = FollowMode.ExactFollow;
    }

    void FollowTransformWithGroundConstraint()
    {
        Vector3 position = transform.position;
        position.x = pastobject.position.x;

        Vector3 rayOrigin = new Vector3(position.x, pastobject.position.y + 1f, position.z);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, maxFallDistance, groundLayer);

        if (hit.collider != null)
        {
            position.y = hit.point.y;
        }

        transform.position = position;
    }

    void FollowExactTransform()
    {
        transform.position = startingPosition;
    }
}
