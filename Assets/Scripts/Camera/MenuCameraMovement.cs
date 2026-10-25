using UnityEngine;

public class MenuCameraMovement : MonoBehaviour
{
    [Header("Wander Settings")]
    public Vector3 movementRange = new Vector3(2f, 1f, 0f); // Max offset in each direction
    public float moveSpeed = 0.5f;                          // How quickly it moves
    public float waitTime = 2f;                             // Time to wait before picking new position

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        // Smoothly move toward target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        // If close to target, start countdown to pick a new one
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                PickNewTarget();
                waitTimer = 0f;
            }
        }
    }

    void PickNewTarget()
    {
        float offsetX = Random.Range(-movementRange.x, movementRange.x);
        float offsetY = Random.Range(-movementRange.y, movementRange.y);
        float offsetZ = Random.Range(-movementRange.z, movementRange.z);

        targetPosition = startPosition + new Vector3(offsetX, offsetY, offsetZ);
    }
}
