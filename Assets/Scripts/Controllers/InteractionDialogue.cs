using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{

    [TextArea(2, 5)]
    public string text;
    public bool isPlayer = true;
    public UnityEvent onEvent;
    public bool triggerEventBeforeLine = false;
    public bool triggerEventAfterPressingContinue = false;
}

public class InteractionDialogue : MonoBehaviour
{
    int webInputToken;
    void OnEnable(){webInputToken=TimeCatWebBridge.Token(continueKey);}

    [Header("Text Display")]
    public TextMesh textMesh;
    public GameObject textBackground;
    public float typingSpeed = 0.05f;

    [Header("Dialogue Settings")]
    public List<DialogueLine> dialogueLines;
    public bool playerStartsFirst = true;

    [Header("Interaction Settings")]
    public KeyCode continueKey = KeyCode.E;
    public bool allowReturnKey = true;
    public bool requireKeyPressToStart = true;
    public bool playOnTrigger = false;
    public bool destroyAfter = false;

    [Header("Speaker Avatars")]
    public SpriteRenderer avatarDisplay;
    public Sprite playerAvatar;
    public Sprite npcAvatar;

    [Header("Audio")]
    public AudioSource playerAudio;
    public AudioClip playerClip;
    public bool loopPlayerSound = true;

    public AudioSource npcAudio;
    public AudioClip npcClip;
    public bool loopNpcSound = true;

    [Header("Events")]
    public UnityEvent onDialogueStart;
    public UnityEvent onDialogueEnd;

    private int dialogueIndex = 0;
    private bool isTyping = false;
    private bool inDialogue = false;
    private Coroutine typingCoroutine;
    private bool canContinue = false;
    private bool isPlayerTurn;

    [Header("Mobile (optional)")]
    private GameObject continueButton;   // assign a UI button in Inspector if mobile
    private bool useMobile = false;
    private bool mContinuePressed = false;

    private bool Consume(ref bool flag) { if (flag) { flag = false; return true; } return false; }
    private void TryBindMobile()
    {
        // find the existing "interact" button
        GameObject interactObj = GameObject.Find("interact");

        // check it exists AND has the right tag (case-sensitive: "mobile")
        if (interactObj && interactObj.CompareTag("Mobile"))
        {
            Button btn = interactObj.GetComponent<Button>();
            if (btn)
            {
                useMobile = true;
                btn.onClick.AddListener(() => mContinuePressed = true);
            }
        }
    }

    void Start()
    {
        if (playerAudio == null) playerAudio = gameObject.AddComponent<AudioSource>();
        if (npcAudio == null) npcAudio = gameObject.AddComponent<AudioSource>();

        if (playOnTrigger && !requireKeyPressToStart)
            StartDialogue();

        // Bind mobile controls if all required objects exist AND are tagged "mobile"

        TryBindMobile();

    }

    void Update()
    {
        #if UNITY_ANDROID || UNITY_IOS
            if (!useMobile){
                TryBindMobile();
            }
        #endif

        if (!inDialogue || !canContinue) return;

        bool pressed = false;

        if (useMobile)
        {
            pressed = mContinuePressed;
        }

        else
        {
            // Only trigger if not clicking on a UI element
            if (Input.GetKeyDown(continueKey) || TimeCatWebBridge.KeyDown(continueKey,ref webInputToken) || (allowReturnKey && Input.GetKeyDown(KeyCode.Return)))
            {
                ShowNextLine();
            }
        }

        mContinuePressed = false;

        if (pressed) ShowNextLine();

    }

    public void StartDialogue()
    {
        if (inDialogue || dialogueLines.Count == 0) return;

        onDialogueStart?.Invoke();
        inDialogue = true;
        dialogueIndex = 0;
        isPlayerTurn = playerStartsFirst;

        if (textBackground != null)
            textBackground.SetActive(true);

        ShowCurrentLine();
    }

    void ShowNextLine()
    {
        DialogueLine prev_line = dialogueLines[dialogueIndex];
        if (!prev_line.triggerEventBeforeLine && prev_line.triggerEventAfterPressingContinue)
        {
            prev_line.onEvent?.Invoke();
        }


        dialogueIndex++;

        if (dialogueIndex >= dialogueLines.Count)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    void ShowCurrentLine()
    {
        if (dialogueIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines[dialogueIndex];
        isPlayerTurn = line.isPlayer;

        if (line.triggerEventBeforeLine)
            line.onEvent?.Invoke();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.text, line));

        UpdateAvatar();
        PlayVoice();
    }

    IEnumerator TypeText(string line, DialogueLine lineData)
    {
        isTyping = true;
        canContinue = false;
        textMesh.text = "";

        foreach (char letter in line.Replace("\\n", "\n").ToCharArray())
        {
            textMesh.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        canContinue = true;

        playerAudio.Stop();
        npcAudio.Stop();

        if (!lineData.triggerEventBeforeLine && !lineData.triggerEventAfterPressingContinue)
            lineData.onEvent?.Invoke();
    }

    void UpdateAvatar()
    {
        if (avatarDisplay == null) return;
        avatarDisplay.sprite = isPlayerTurn ? playerAvatar : npcAvatar;
    }

    void PlayVoice()
    {
        if (isPlayerTurn && playerClip)
        {
            playerAudio.clip = playerClip;
            playerAudio.loop = loopPlayerSound;
            playerAudio.Play();
        }
        else if (!isPlayerTurn && npcClip)
        {
            npcAudio.clip = npcClip;
            npcAudio.loop = loopNpcSound;
            npcAudio.Play();
        }
    }

    void EndDialogue()
    {
        inDialogue = false;
        textMesh.text = "";

        if (textBackground != null)
            textBackground.SetActive(false);

        onDialogueEnd?.Invoke();

        if (destroyAfter)
            Destroy(gameObject);
    }
    public void SkipDialogue()
{
    if (!inDialogue) return;

    // Stop any ongoing typing
    if (typingCoroutine != null)
    {
        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
    }

    // Fire events for all remaining lines
    for (int i = dialogueIndex; i < dialogueLines.Count; i++)
    {
        DialogueLine line = dialogueLines[i];

        // If it triggers before showing
        if (line.triggerEventBeforeLine)
            line.onEvent?.Invoke();

        // If it triggers after pressing continue (simulate pressing continue)
        if (!line.triggerEventBeforeLine && line.triggerEventAfterPressingContinue)
            line.onEvent?.Invoke();

        // If it only triggers after typing without wait
        if (!line.triggerEventBeforeLine && !line.triggerEventAfterPressingContinue)
            line.onEvent?.Invoke();
    }

    // Stop audio
    playerAudio.Stop();
    npcAudio.Stop();

    // Immediately end dialogue
    EndDialogue();
}


    private void OnTriggerEnter(Collider other)
    {
        if (!playOnTrigger || requireKeyPressToStart) return;
        StartDialogue();
    }
}
