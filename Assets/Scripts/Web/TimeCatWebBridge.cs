using UnityEngine;
using System.Runtime.InteropServices;
[RequireComponent(typeof(SceneLoader))]
public sealed class TimeCatWebBridge : MonoBehaviour {
    [System.Serializable] class TouchState {public float x,y;}
    public static bool TouchMode {get;private set;}
    public static Vector2 Movement {get;private set;}
    public static int TimeSequence {get;private set;}
    public static int AdvanceSequence {get;private set;}
    public static int Token(KeyCode key){return key==KeyCode.F || key==KeyCode.T?TimeSequence:(key==KeyCode.Space || key==KeyCode.Return || key==KeyCode.E)?AdvanceSequence:0;}
    public static bool KeyDown(KeyCode key,ref int seen){int token=Token(key);bool pressed=token!=seen;seen=token;return pressed;}
    static TimeCatWebBridge instance;
    static bool JumpQueued,AttackQueued,InteractQueued;
    public static bool ConsumeJump(){bool value=JumpQueued;JumpQueued=false;return value;}
    public static bool ConsumeAttack(){bool value=AttackQueued;AttackQueued=false;return value;}
    public static bool ConsumeInteract(){bool value=InteractQueued;InteractQueued=false;return value;}
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void TimeCat_Pause(int paused);
    [DllImport("__Internal")] static extern int TimeCat_IsTouch();
    [DllImport("__Internal")] static extern void TimeCat_Scene(string name);
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot(){
        if(instance)return;instance=new GameObject("TimeCatWebBridge").AddComponent<TimeCatWebBridge>();DontDestroyOnLoad(instance.gameObject);
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput=false;
        TouchMode=TimeCat_IsTouch()!=0;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded+=(scene,mode)=>{AudioListener.pause=false;TimeCat_Pause(0);TimeCat_Scene(scene.name);};
#else
        TouchMode=Application.isMobilePlatform;
#endif
    }
    public void SetTouchMode(string value){TouchMode=value=="1";if(!TouchMode)ClearInput();}
    public void SetTouchState(string json){var state=JsonUtility.FromJson<TouchState>(json);Movement=Vector2.ClampMagnitude(new Vector2(state.x,state.y),1);}
    public void Action(string action){
        switch(action){case "jump":JumpQueued=true;AdvanceSequence++;break;case "attack":AttackQueued=true;break;case "interact":InteractQueued=true;AdvanceSequence++;break;case "time":var boss=FindObjectOfType<SpiderBossController>();if(boss && boss.timelineTraversal && boss.timelineTraversal.enabled){if(!boss.IsCutsceneActive && !boss.IsReentering)boss.timelineTraversal.TryTraverse();}else TimeSequence++;break;case "continue":if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Intro")UnityEngine.SceneManagement.SceneManager.LoadScene("Lab_1");else AdvanceSequence++;break;}
    }
    public void Inspect(string ignored){
        Debug.Log("WEB QA scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" audioPaused="+AudioListener.pause+" audioVolume="+AudioListener.volume+" timeScale="+Time.timeScale+" touch="+TouchMode+" movement="+Movement);
        foreach(var camera in FindObjectsOfType<Camera>())Debug.Log("WEB QA camera="+camera.name+" enabled="+camera.enabled+" tag="+camera.tag+" position="+camera.transform.position+" size="+camera.orthographicSize);
        foreach(var particles in FindObjectsOfType<ParticleSystem>()){var renderer=particles.GetComponent<ParticleSystemRenderer>();Debug.Log("WEB QA particles="+particles.name+" count="+particles.particleCount+" playing="+particles.isPlaying+" visible="+(renderer && renderer.isVisible)+" shader="+(renderer && renderer.sharedMaterial?renderer.sharedMaterial.shader.name:"missing"));}
        foreach(var music in FindObjectsOfType<MusicFaderController>()){var source=music.GetComponent<AudioSource>();if(source)Debug.Log("WEB QA music="+music.name+" playing="+source.isPlaying+" volume="+source.volume+" spatial="+source.spatialBlend+" clip="+(source.clip?source.clip.name:"none"));}
        foreach(var cat in FindObjectsOfType<CatPlayerController>(true)){var sprite=cat.GetComponent<SpriteRenderer>();var body=cat.GetComponent<Rigidbody2D>();Debug.Log("WEB QA player="+cat.name+" position="+cat.transform.position+" active="+cat.gameObject.activeInHierarchy+" enabled="+cat.enabled+" frozen="+cat.IsFrozen+" sprite="+(sprite?sprite.enabled.ToString():"missing")+" velocity="+(body?body.velocity.ToString():"missing")+" viewport="+(Camera.main?Camera.main.WorldToViewportPoint(cat.transform.position).ToString():"no camera"));}
    }
    public void ClearInput(){Movement=Vector2.zero;JumpQueued=AttackQueued=InteractQueued=false;}
    public void SetMuted(string value){AudioListener.volume=value=="1"?0:1;}
    public static void NotifyPause(bool paused){
#if UNITY_WEBGL && !UNITY_EDITOR
        AudioListener.pause=paused;TimeCat_Pause(paused?1:0);
#endif
    }
    public void SetPaused(string value){ClearInput();bool paused=value=="1";var manager=FindObjectOfType<PauseManager>();AudioListener.pause=paused;if(manager){if(paused)manager.PauseGame();else manager.ResumeGame();}else {Time.timeScale=paused?0:1;NotifyPause(paused);}}
    void OnApplicationFocus(bool focus){if(!focus)ClearInput();}
}
