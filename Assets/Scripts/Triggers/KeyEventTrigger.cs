using UnityEngine;
using UnityEngine.Events;

public class KeyEventTrigger : MonoBehaviour
{
    int webInputToken;
    void OnEnable(){webInputToken=TimeCatWebBridge.Token(triggerKey);}

    [Header("Key Settings")]
    public KeyCode triggerKey = KeyCode.Space;

    [Header("Conditional Target")]
    public GameObject targetObject;        // Object to check for active state
    public bool invertCondition = false;   // If true, use alternate events when the object IS active

    [Header("Primary Events (when condition is true)")]
    public UnityEvent onKeyPressed;
    public UnityEvent onKeyReleased;

    [Header("Alternate Events (when condition is false)")]
    public UnityEvent onKeyPressed_Alt;
    public UnityEvent onKeyReleased_Alt;

    public bool allowMobileTimeKey = true; // Allow mobile time key to trigger events

    private bool mTimePressed = false;

    private bool useMobile = false;

    private bool Consume(ref bool flag) { if (flag) { flag = false; return true; } return false; }

    private void Start()
    {
        if (allowMobileTimeKey)
        {
            // Auto-find the "time" UI button
            GameObject timeObj = GameObject.Find("time");
            if (timeObj && timeObj.CompareTag("Mobile"))
            {
                var btn = timeObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => mTimePressed = true);
                    useMobile = true;
                }
            }
        }
    }

    void Update()
    {
        bool targetActive = targetObject != null && targetObject.activeInHierarchy;
        bool conditionMet = invertCondition ? !targetActive : targetActive;

        #if UNITY_ANDROID || UNITY_IOS
            if (!useMobile){
                if (allowMobileTimeKey)
                {
                    // Auto-find the "time" UI button
                    GameObject timeObj = GameObject.Find("time");
                    if (timeObj && timeObj.CompareTag("Mobile"))
                    {
                        var btn = timeObj.GetComponent<UnityEngine.UI.Button>();
                        if (btn != null)
                        {
                            btn.onClick.AddListener(() => mTimePressed = true);
                            useMobile = true;
                        }
                    }
                }
            }
        #endif
        bool webPressed=TimeCatWebBridge.KeyDown(triggerKey,ref webInputToken);
        if (Input.GetKeyDown(triggerKey) || mTimePressed || webPressed)
        {
            if (conditionMet)
                onKeyPressed?.Invoke();
            else
                onKeyPressed_Alt?.Invoke();
        }

        if (Input.GetKeyUp(triggerKey) || mTimePressed || webPressed)
        {
            if (conditionMet)
                onKeyReleased?.Invoke();
            else
                onKeyReleased_Alt?.Invoke();
        }
        mTimePressed = false;
    }
}
