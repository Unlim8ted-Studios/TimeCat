using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

public class ColliderEventTriggerDelayed : MonoBehaviour
{
    [Header("Tag Filter")]
    [FormerlySerializedAs("tagused")] // exact old name
    public string tagUsed;

    [System.Serializable]
    public class TimedUnityEvent
    {
        [FormerlySerializedAs("onPressed")] // maps UnityEvent to old onPressed or onLeave
        public UnityEvent unityEvent;

        [Header("Delay Options")]
        public bool waitBeforeInvoke = false;
        public float waitTime = 0f;
        [Header("Animation Wait (optional)")]
        public Animator animatorToWatch;
        public string animationNameToWaitFor;
    }

    [Header("On Pressed Actions")]
    [FormerlySerializedAs("onPressed")] // exact old name
    public List<TimedUnityEvent> onPressedEvents = new List<TimedUnityEvent>();

    [Header("On Leave Actions")]
    [FormerlySerializedAs("onLeave")] // exact old name
    public List<TimedUnityEvent> onLeaveEvents = new List<TimedUnityEvent>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActiveAndEnabled && other.CompareTag(tagUsed))
            StartCoroutine(InvokeEventList(onPressedEvents));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isActiveAndEnabled && other.CompareTag(tagUsed))
            StartCoroutine(InvokeEventList(onLeaveEvents));
    }

    private IEnumerator InvokeEventList(List<TimedUnityEvent> events)
    {
        foreach (var e in events)
        {
            if (e.waitBeforeInvoke && e.waitTime > 0)
                yield return new WaitForSeconds(e.waitTime);

            if (e.animatorToWatch && !string.IsNullOrEmpty(e.animationNameToWaitFor))
            {
                Animator anim = e.animatorToWatch;
                while (anim.GetCurrentAnimatorStateInfo(0).IsName(e.animationNameToWaitFor) &&
                       anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }
            }

            e.unityEvent.Invoke();
        }
    }
}
