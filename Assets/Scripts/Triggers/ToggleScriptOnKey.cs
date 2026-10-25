using UnityEngine;

public class ToggleScriptOnKey : MonoBehaviour
{
    int webInputToken;
    void OnEnable(){webInputToken=TimeCatWebBridge.Token(toggleKey);}

    public GameObject targetObject;          // The object that has the script
    public string scriptNameToToggle;        // The name of the script (e.g., "MyScript")
    public KeyCode toggleKey = KeyCode.T;    // The key used to toggle
    public Transform presentPlayer;
    public Transform pastPlayer;
    bool CanTraverseTime()
    {
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
    void Update()
    {
        if ((Input.GetKeyDown(toggleKey) || TimeCatWebBridge.KeyDown(toggleKey,ref webInputToken)))
        {
            if (CanTraverseTime() && targetObject != null && !string.IsNullOrEmpty(scriptNameToToggle))
            {
                Component script = targetObject.GetComponent(scriptNameToToggle);
                if (script != null)
                {
                    MonoBehaviour mono = script as MonoBehaviour;
                    if (mono != null)
                    {
                        mono.enabled = !mono.enabled;
                        Debug.Log($"{scriptNameToToggle} on {targetObject.name} is now {(mono.enabled ? "enabled" : "disabled")}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Script '{scriptNameToToggle}' not found on {targetObject.name}.");
                }
            }
        }
    }
}
