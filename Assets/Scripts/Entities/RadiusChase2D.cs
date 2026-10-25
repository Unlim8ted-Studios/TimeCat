using UnityEngine;

public class RadiusChase2D : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform originPoint;

    [Header("Chase Settings")]
    public float chaseRadius = 5f;
    public float moveSpeed = 3f;

    private Vector3 currentTarget;

    void Update()
    {
        float distanceToTarget = Vector2.Distance(originPoint.position, target.position);

        if (distanceToTarget <= chaseRadius)
        {
            // Chase the target
            currentTarget = target.position;
        }
        else
        {
            // Return to origin
            currentTarget = originPoint.position;
        }

        // Move toward currentTarget
        Vector2 direction = (currentTarget - transform.position).normalized;
        transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        if (originPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(originPoint.position, chaseRadius);
        }
    }
}
