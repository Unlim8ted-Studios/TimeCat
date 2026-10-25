using UnityEngine;
using UnityEngine.Events;

public class ColliderEventTrigger : MonoBehaviour
{

    [Header("tag that works")]
    public string tagused;        // Object to check for active state

    [Header("Primary Events")]
    public UnityEvent onPressed;
    public UnityEvent onLeave;

    void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.gameObject.tag == tagused)
        {
            onPressed.Invoke();
        }
    }

    void OnTriggerExit2D(Collider2D other) 
    {
        if (other.gameObject.tag == tagused)
        {
            onLeave.Invoke();
        }
    }
}//if (other.gameObject.tag == tag) 
