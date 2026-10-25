using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class SettingsController : MonoBehaviour
{
    [Header("Volume")]
    public Slider volumeSlider;
    
    [Header("Display Mode")]
    public TMP_Dropdown displayModeDropdown;

    [Header("Keybind UI")]
    public GameObject keybindRowPrefab;      // Prefab with: label, key label, edit button
    public Transform keybindRowParent;       // Parent container for keybind rows
    public GameObject promptPanel;           // Panel that shows "Press a key for..."
    public TMP_Text promptText;

    private string listeningForKey = null;
    private Action<string, KeyCode> onKeySelected;

    private Dictionary<string, string> keybindLabels = new Dictionary<string, string>
    {
        { "UpKey", "MOVE UP" },
        { "DownKey", "MOVE DOWN" },
        { "LeftKey", "MOVE LEFT" },
        { "RightKey", "MOVE RIGHT" },
        { "JumpKey", "JUMP" },
        { "TimeTravelKey", "TIME TRAVEL" },
        { "AttackKey", "ATTACK" },
        { "SprintKey", "SPRINT" },
        { "InteractKey", "INTERACT" }
    };

    public UnityEvent onFullscreen;
    public UnityEvent onWindowed;

    private Dictionary<string, TMP_Text> keyLabelLookup = new Dictionary<string, TMP_Text>();
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        // Cache all AudioSource original volumes once
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            if (!originalVolumes.ContainsKey(source))
                originalVolumes[source] = source.volume;
        }

        // Volume
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        volumeSlider.value = volume;
        ApplyVolume(volume);
        volumeSlider.onValueChanged.AddListener((v) =>
        {
            ApplyVolume(v);
            PlayerPrefs.SetFloat("MasterVolume", v);
        });

        // Display Mode
        int displayModeIndex = PlayerPrefs.GetInt("DisplayMode", 0);
        displayModeDropdown.value = displayModeIndex;
        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        OnDisplayModeChanged(displayModeIndex);

        float ySpacing = -50f;
        int index = 0;

        foreach (var kvp in keybindLabels)
        {
            string keyName = kvp.Key;
            string label = kvp.Value;

            GameObject row = Instantiate(keybindRowPrefab, keybindRowParent);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(keybindRowParent, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, index * ySpacing);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            Button editButton = row.GetComponentInChildren<Button>();

            TMP_Text labelText = texts[0];
            TMP_Text keyText = texts[1];

            labelText.text = label;
            string savedKey = PlayerPrefs.GetString(keyName, GetDefaultKey(keyName).ToString());
            keyText.text = savedKey;
            keyLabelLookup[keyName] = keyText;

            editButton.onClick.AddListener(() => StartRebind(keyName, label));

            index++;
        }

        promptPanel.SetActive(false);
    }

    void Update()
    {
        if (listeningForKey != null && Input.anyKeyDown)
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    FinishRebind(key);
                    break;
                }
            }
        }
    }

    // --- Rebinding Logic ---

    void StartRebind(string keyName, string label)
    {
        listeningForKey = keyName;
        promptText.text = $"Press a key for {label}";
        promptPanel.SetActive(true);
    }

    void FinishRebind(KeyCode newKey)
    {
        string keyName = listeningForKey;
        PlayerPrefs.SetString(keyName, newKey.ToString());

        if (keyLabelLookup.ContainsKey(keyName))
            keyLabelLookup[keyName].text = newKey.ToString();

        ApplyKeyToComponents(keyName, newKey);

        listeningForKey = null;
        promptPanel.SetActive(false);
    }

    void ApplyKeyToComponents(string keyName, KeyCode newKey)
    {
        foreach (var player in FindObjectsOfType<CatPlayerController>())
        {
            switch (keyName)
            {
                case "UpKey": player.upKey = newKey; break;
                case "DownKey": player.downKey = newKey; break;
                case "LeftKey": player.leftKey = newKey; break;
                case "RightKey": player.rightKey = newKey; break;
                case "JumpKey": player.JumpKey = newKey; break;
                case "TimeTravelKey": player.timeTravelKey = newKey; break;
                case "AttackKey": player.AttackKey = newKey; break;
                case "SprintKey": player.SprintKey = newKey; break;
                case "InteractKey": player.InteractKey = newKey; break;
            }
        }

        if (keyName == "TimeTravelKey")
        {
            foreach (var cam in FindObjectsOfType<TimeTravelCameraSwitcher>())
                cam.switchKey = newKey;
        }
    }

    // --- Volume + Display ---

    void ApplyVolume(float v)
    {
        // v is treated as a percentage (0–1)
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            if (!originalVolumes.ContainsKey(source))
                originalVolumes[source] = source.volume;

            source.volume = originalVolumes[source] * v;
        }
    }

    void OnDisplayModeChanged(int index)
    {
        if (index == 0){
            onFullscreen?.Invoke();
        } else {
            onWindowed?.Invoke();
        }
        FullScreenMode mode = index switch
        {
            0 => FullScreenMode.FullScreenWindow,
            1 => FullScreenMode.Windowed,
        };

        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("DisplayMode", index);
    }

    KeyCode GetDefaultKey(string keyName)
    {
        return keyName switch
        {
            "UpKey" => KeyCode.W,
            "DownKey" => KeyCode.S,
            "LeftKey" => KeyCode.A,
            "RightKey" => KeyCode.D,
            "JumpKey" => KeyCode.Space,
            "TimeTravelKey" => KeyCode.F,
            "AttackKey" => KeyCode.Mouse0,
            "SprintKey" => KeyCode.LeftShift,
            "InteractKey" => KeyCode.E,
            _ => KeyCode.None
        };
    }
}
