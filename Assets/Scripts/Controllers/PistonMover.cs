using UnityEngine;

public class PistonMover : MonoBehaviour
{
    public Vector3 targetOffset = new Vector3(0, 2f, 0); // How far the piston moves when activated
    public float moveSpeed = 2f; // Speed of movement

    private Vector3 originPosition;
    private Vector3 targetPosition;

    public bool isActivated = false;

    void Start()
    {
        originPosition = transform.position;
        targetPosition = originPosition + targetOffset;
    }

    void Update()
    {
        Vector3 destination = isActivated ? targetPosition : originPosition;
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    // Call this to activate the piston
    public void EnablePiston()
    {
        isActivated = true;
    }

    // Call this to deactivate the piston
    public void DisablePiston()
    {
        isActivated = false;
    }

    // Draw gizmo showing the target position in the Scene view
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // Preview the gizmo based on the current transform position in edit mode
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + targetOffset, 0.2f);
        }
        else
        {
            // At runtime, use the cached originPosition
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(originPosition + targetOffset, 0.2f);
        }
    }
}
