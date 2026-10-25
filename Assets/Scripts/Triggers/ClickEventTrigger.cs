using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ClickEventTrigger : MonoBehaviour
{
    public UnityEvent onClick;

    private void OnMouseDown()
    {
        if (onClick != null)
        {
            onClick.Invoke();
        }
    }
}
