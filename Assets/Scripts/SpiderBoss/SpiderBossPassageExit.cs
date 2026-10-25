using UnityEngine;
using UnityEngine.SceneManagement;

public class SpiderBossPassageExit : MonoBehaviour {
    public string sceneName="Lab_7";
    public SpiderBossController owner;
    public GateController door;
    bool loading;
    void OnTriggerEnter2D(Collider2D other) { TryLeave(other); }
    void OnTriggerStay2D(Collider2D other) { TryLeave(other); }
    void TryLeave(Collider2D other) {
        if(loading || (door && !door.IsOpen) || !owner || owner.CurrentPhase!=SpiderBossController.Phase.Complete || !other.GetComponentInParent<CatPlayerController>()) return;
        if(!Application.CanStreamedLevelBeLoaded(sceneName)) { Debug.LogWarning("Boss passage scene is not in Build Settings: "+sceneName); return; }
        loading=true;var transition=new GameObject("Boss exit scene fade").AddComponent<SceneLoader>();transition.LoadSceneWithFade(sceneName);
    }
}
