using UnityEngine;
using UnityEngine.Events;

public class AnimationEventWatcher : MonoBehaviour
{
    public Animator animator;
    public string animationStateName = "UseButton";
    public int animatorLayer = 0;
    public UnityEvent onAnimationComplete;

    private bool wasPlaying = false;

    void Update()
    {
        if (animator == null || string.IsNullOrEmpty(animationStateName))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);

        if (stateInfo.IsName(animationStateName))
        {
            // Animation is currently playing
            wasPlaying = true;
        }
        else
        {
            // Animation is NOT playing anymore, but it was last frame
            if (wasPlaying)
            {
                wasPlaying = false;
                onAnimationComplete?.Invoke();
            }
        }
    }
}
