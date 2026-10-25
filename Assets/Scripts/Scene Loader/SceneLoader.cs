using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneWithFade(string destination) {
        if(!transitioning && Application.CanStreamedLevelBeLoaded(destination))StartCoroutine(FinishLab(destination));
    }
    static bool transitioning;
    float fade;
    IEnumerator FinishLab(string destination) {
        transitioning=true;
        foreach(var collider in GetComponents<Collider2D>())collider.enabled=false;
        transform.SetParent(null);DontDestroyOnLoad(gameObject);
        foreach(var cat in FindObjectsOfType<CatPlayerController>())cat.Freeze();
        for(float t=0;t<.85f;t+=Time.unscaledDeltaTime){fade=Mathf.SmoothStep(0,1,t/.85f);yield return null;}
        fade=1;
        var loading=SceneManager.LoadSceneAsync(destination);
        while(!loading.isDone)yield return null;
        yield return null;
        for(float t=0;t<.85f;t+=Time.unscaledDeltaTime){fade=1-Mathf.SmoothStep(0,1,t/.85f);yield return null;}
        fade=0;transitioning=false;Destroy(gameObject);
    }
    void OnGUI() {
        if(fade<=0)return;
        GUI.depth=-10000;var old=GUI.color;GUI.color=new Color(0,0,0,fade);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
    }
    // Call this function and pass the name of the scene you want to load
    public void LoadSceneByName(string sceneName)
    {
        if(SceneManager.GetActiveScene().name=="Lab_7" && sceneName=="Title") {
            if(!transitioning)StartCoroutine(FinishLab(sceneName));
        } else SceneManager.LoadScene(sceneName);
    }

    // Optional: Load scene by index (from Build Settings)
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // Optional: Reload the current scene
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
