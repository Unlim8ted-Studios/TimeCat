using UnityEngine;

public class RatAnimationController : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator component not found on: " + gameObject.name);
        }
    }

    // Call this to set the talking state
    public void SetTalking(bool isTalking)
    {
        animator.SetBool("isTalking", isTalking);
    }
    public void OpenGate()
    {
        animator.SetTrigger("OpenGate");
    }

    // Call this to trigger the wake-up animation
    public void TriggerWakeUp()
    {
        animator.SetTrigger("WakeUp");
    }

    // Add more control methods here as needed
}
