using UnityEngine;

public class QuitHandler : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TimeCat_Exit();
#endif
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop playing in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        TimeCat_Exit();
#else
        // Quit the built application
        Application.Quit();
#endif
    }
}
