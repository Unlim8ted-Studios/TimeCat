using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimeTravelCameraSwitcher : MonoBehaviour
{
    int webInputToken;
    void OnEnable(){webInputToken=TimeCatWebBridge.Token(switchKey);}

    [Header("Cameras")]
    public Camera presentCamera;
    public Camera pastCamera;

    [Header("Players")]
    public CatPlayerController presentPlayer;
    public CatPlayerController pastPlayer;

    [Header("Root Objects")]
    public Transform presentRoot;
    public Transform pastRoot;

    [Header("Settings")]
    public KeyCode switchKey = KeyCode.F;
    public bool startInPresent = true;

    [Header("Primary Events")]
    public UnityEvent onKeyPressed;

    [Header("UI Text Meshes")]
    public TextMesh presentMessageText;
    public TextMesh pastMessageText;
    public float messageDuration = 2f;

    private bool isInPresent;
    private float messageTimer;

    private bool useMobile = false;
    private bool mTimePressed = false;

    private bool Consume(ref bool flag) { if (flag) { flag = false; return true; } return false; }


    void Start()
    {
        isInPresent = startInPresent;
        UpdateCameraState();
        presentMessageText.gameObject.SetActive(false);
        pastMessageText.gameObject.SetActive(false);

        // Link players to each other
        presentPlayer.otherPlayer = pastPlayer;
        pastPlayer.otherPlayer = presentPlayer;
        presentPlayer.isPresent = true;
        pastPlayer.isPresent = false;

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
        Debug.Log("Is using mobile:" + useMobile);

    }

    void Update()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (!useMobile)
        {
            Debug.Log("Trying to bind mobile time button");
            GameObject timeObj = GameObject.Find("time");
            if (timeObj && timeObj.CompareTag("Mobile"))
            {
                var btn = timeObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => mTimePressed = true);
                    btn.onClick.AddListener(() => Debug.Log("Mobile time button pressed"));
                    useMobile = true;
                }
            }
        }
        #endif

        bool pressed = false;

        if (useMobile)
        {
            pressed = mTimePressed;
        }
        else
        {
            if ((Input.GetKeyDown(switchKey) || TimeCatWebBridge.KeyDown(switchKey,ref webInputToken)))
                pressed = true;
        }

        if (pressed)
        {
            if (CanTraverseTime())
            {
                onKeyPressed?.Invoke();
                SwitchTimePeriod();
            }
            else
            {
            ShowMessage($"Area obstructed in the {(isInPresent ? "past" : "future")}.");
            }
        }


        SyncPlayerLocalPosition();

        if (presentMessageText.gameObject.activeSelf || pastMessageText.gameObject.activeSelf)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0)
            {
                presentMessageText.gameObject.SetActive(false);
                pastMessageText.gameObject.SetActive(false);
            }
        }
        mTimePressed = false;
    }

    void SwitchTimePeriod()
    {
        CatPlayerController fromPlayer = isInPresent ? presentPlayer : pastPlayer;
        CatPlayerController toPlayer = isInPresent ? pastPlayer : presentPlayer;

        GameObject heldBox = fromPlayer.GetHeldBox();

        if (heldBox != null)
        {
            if (heldBox.CompareTag("TimeBox"))
            {
                string boxName = heldBox.name;
                Vector2 boxPos = heldBox.transform.position;

                toPlayer.resetJumpFlag();
                fromPlayer.resetJumpFlag();

                if (isInPresent) // Present → Past
                {
                    toPlayer.DestroyBoxWithName(boxName);
                    GameObject newPastBox = Instantiate(heldBox, toPlayer.transform.position, Quaternion.identity);
                    newPastBox.name = boxName;
                    toPlayer.ReceiveBoxFromTime(newPastBox);
                    fromPlayer.ForceDropBoxSilently();
                }
                else // Past → Present
                {
                    toPlayer.DestroyBoxWithName(boxName);
                    GameObject newPresentBox = Instantiate(heldBox, toPlayer.transform.position, Quaternion.identity);
                    newPresentBox.name = boxName;
                    toPlayer.ReceiveBoxFromTime(newPresentBox);
                    fromPlayer.ForceDropBoxSilently();
                }
            }
            else
            {
                // Regular box cannot travel through time — just drop it
                fromPlayer.DropBox();
            }
        }


        isInPresent = !isInPresent;
        presentPlayer.isPresent = isInPresent;
        pastPlayer.isPresent = !isInPresent;

        UpdateCameraState();
    }

    void UpdateCameraState()
    {
        // Enable correct camera and player
        presentCamera.enabled = isInPresent;
        pastCamera.enabled = !isInPresent;

        // Toggle audio listeners so only one is active
        AudioListener presentListener = presentCamera.GetComponent<AudioListener>();
        AudioListener pastListener = pastCamera.GetComponent<AudioListener>();
        if (presentListener) presentListener.enabled = isInPresent;
        if (pastListener) pastListener.enabled = !isInPresent;

        presentPlayer.gameObject.SetActive(isInPresent);
        pastPlayer.gameObject.SetActive(!isInPresent);

        // Hide all boxes in the timeline you're not in
        ToggleBoxVisibility("TimeBox", isInPresent ? presentRoot : pastRoot, true);
        ToggleBoxVisibility("TimeBox", isInPresent ? pastRoot : presentRoot, false);
    }

    void ToggleBoxVisibility(string tag, Transform root, bool visible)
    {
        foreach (Transform child in root)
        {
            if (child.CompareTag(tag))
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer) renderer.enabled = visible;

                // Optional: disable physics so they don’t move
                Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
                if (rb) rb.simulated = visible;
            }
        }
    }

    void SyncPlayerLocalPosition()
    {
        if (isInPresent)
            pastPlayer.transform.localPosition = presentPlayer.transform.localPosition;
        else
            presentPlayer.transform.localPosition = pastPlayer.transform.localPosition;
    }

    bool CanTraverseTime()
    {
        Collider2D[] presentHits = Physics2D.OverlapCircleAll(presentPlayer.transform.position, 0.1f);
        Collider2D[] pastHits = Physics2D.OverlapCircleAll(pastPlayer.transform.position, 0.1f);

        foreach (var hit in presentHits)
            if (hit.CompareTag("NonTraversable")) return false;

        foreach (var hit in pastHits)
            if (hit.CompareTag("NonTraversable")) return false;

        return true;
    }

    void ShowMessage(string message)
    {
        if (isInPresent)
        {
            presentMessageText.text = message;
            presentMessageText.gameObject.SetActive(true);
        }
        else
        {
            pastMessageText.text = message;
            pastMessageText.gameObject.SetActive(true);
        }
        messageTimer = messageDuration;
    }

    public bool getIsInPresent(){
        return isInPresent;
    }
}
