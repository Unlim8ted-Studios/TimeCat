using UnityEngine;

public class SettingsLoader : MonoBehaviour
{
    public KeyEventTrigger changeKeyEventTrigger;
    void Start()
    {
        // Load volume
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        foreach (AudioSource source in FindObjectsOfType<AudioSource>())
        {
            source.volume = volume;
        }

        // Load display mode
        int displayModeIndex = PlayerPrefs.GetInt("DisplayMode", 0);
        FullScreenMode mode = displayModeIndex switch
        {
            0 => FullScreenMode.FullScreenWindow,
            1 => FullScreenMode.Windowed,
            _ => FullScreenMode.Windowed,
        };

        if(Application.platform!=RuntimePlatform.WebGLPlayer)Screen.fullScreenMode=mode;

        // Load TimeTravelCameraSwitcher switch key
        string timeTravelKeyStr = PlayerPrefs.GetString("TimeTravelKey", "F");
        if (System.Enum.TryParse(timeTravelKeyStr, out KeyCode timeTravelKey))
        {
            foreach (TimeTravelCameraSwitcher switcher in FindObjectsOfType<TimeTravelCameraSwitcher>())
            {
                switcher.switchKey = timeTravelKey;
            }
            if(changeKeyEventTrigger)changeKeyEventTrigger.triggerKey = timeTravelKey;
        }

        // Load keybinds into all CatPlayerControllers
        foreach (CatPlayerController controller in FindObjectsOfType<CatPlayerController>())
        {
            controller.upKey = GetKey("UpKey", KeyCode.W);
            controller.downKey = GetKey("DownKey", KeyCode.S);
            controller.leftKey = GetKey("LeftKey", KeyCode.A);
            controller.rightKey = GetKey("RightKey", KeyCode.D);
            controller.JumpKey = GetKey("JumpKey", KeyCode.Space);
            controller.timeTravelKey = GetKey("TimeTravelKey", KeyCode.F);
            controller.AttackKey = GetKey("AttackKey", KeyCode.Mouse0);
            controller.SprintKey = GetKey("SprintKey", KeyCode.LeftShift);
            controller.InteractKey = GetKey("InteractKey", KeyCode.E);
        }
    }

    private KeyCode GetKey(string keyName, KeyCode defaultKey)
    {
        string saved = PlayerPrefs.GetString(keyName, defaultKey.ToString());
        if (System.Enum.TryParse(saved, out KeyCode key))
            return key;
        return defaultKey;
    }
}
