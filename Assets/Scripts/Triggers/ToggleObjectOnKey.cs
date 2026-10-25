using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ToggleObjectOnKey : MonoBehaviour
{
    int webInputToken;
    void OnEnable(){webInputToken=TimeCatWebBridge.Token(toggleKey);}

    [Header("Toggle Settings")]
    [Tooltip("Objects to toggle when traversal is allowed.")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Tooltip("Key used to attempt traversal.")]
    public KeyCode toggleKey = KeyCode.T;

    [Header("Player References")]
    public Transform presentPlayer;
    public Transform pastPlayer;

    [Header("Events")]
    [Tooltip("Invoked when traversal succeeds.")]
    public UnityEvent onTraverseSuccess;

    [Tooltip("Invoked when traversal fails (blocked by NonTraversable).")]
    public UnityEvent onTraverseBlocked;

    // -----------------------------------------------------------------------

    private void Update()
    {
        if ((Input.GetKeyDown(toggleKey) || TimeCatWebBridge.KeyDown(toggleKey,ref webInputToken))) TryTraverse();
    }

    public bool TryTraverse()
    {
        if (!CanTraverseTime()) { onTraverseBlocked?.Invoke(); return false; }
        if (targetObjects == null || targetObjects.Count == 0) return false;
        ToggleTargets();
        onTraverseSuccess?.Invoke();
        return true;
    }

    // -----------------------------------------------------------------------

    private bool CanTraverseTime()
    {
        if (presentPlayer == null || pastPlayer == null)
        {
            Debug.LogWarning("[ToggleObjectOnKey] Missing player references.");
            return false;
        }

        Collider2D[] presentHits = Physics2D.OverlapCircleAll(presentPlayer.position, 0.1f);
        Collider2D[] pastHits = Physics2D.OverlapCircleAll(pastPlayer.position, 0.1f);

        foreach (var hit in presentHits)
        {
            if (hit.CompareTag("NonTraversable"))
                return false;
        }

        foreach (var hit in pastHits)
        {
            if (hit.CompareTag("NonTraversable"))
                return false;
        }

        return true;
    }

    private void ToggleTargets()
    {
        if (targetObjects == null || targetObjects.Count == 0)
        {
            Debug.LogWarning("[ToggleObjectOnKey] No target objects assigned.");
            return;
        }

        foreach (var obj in targetObjects)
        {
            if (obj == null) continue;

            // Avoid disabling the object running this script
            if (obj != gameObject)
                obj.SetActive(!obj.activeSelf);
        }
    }


    // Optional visual debug for traversal check range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (presentPlayer != null)
            Gizmos.DrawWireSphere(presentPlayer.position, 0.1f);
        if (pastPlayer != null)
            Gizmos.DrawWireSphere(pastPlayer.position, 0.1f);
    }
}
