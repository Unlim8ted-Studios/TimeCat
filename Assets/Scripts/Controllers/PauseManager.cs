using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu; // Assign your pause UI object
    public List<Animator> uiAnimators; // Assign all pause menu-related animators here

    private bool isPaused = false;

    public void unpause(){
        ResumeGame();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if(pauseMenu)pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        SetAnimatorUpdateMode(AnimatorUpdateMode.UnscaledTime);
        isPaused = true;
        TimeCatWebBridge.NotifyPause(true);
    }

    public void ResumeGame()
    {
        if(pauseMenu)pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        SetAnimatorUpdateMode(AnimatorUpdateMode.Normal);
        isPaused = false;
        TimeCatWebBridge.NotifyPause(false);
    }

    private void SetAnimatorUpdateMode(AnimatorUpdateMode mode)
    {
        if(uiAnimators==null)return;
        foreach (Animator anim in uiAnimators)
        {
            if (anim != null)
                anim.updateMode = mode;
        }
    }
}
