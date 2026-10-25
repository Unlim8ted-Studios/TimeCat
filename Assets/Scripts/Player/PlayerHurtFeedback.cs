using UnityEngine;

// Damage feedback is driven by accepted health changes, not collision callbacks.
// It never takes ownership of music, movement, time scale, or camera transforms.
[DefaultExecutionOrder(1000)]
public sealed class PlayerHurtFeedback : MonoBehaviour {
    public float health=100, maximumHealth=100;
    public float lowHealthThreshold=.3f;
    SpriteRenderer[] sprites;
    Color[] baseColors;
    AudioSource voice;
    AudioClip impactSound,acidSound,heartbeat;
    CatPlayerController cat;
    SpiderBossController boss;
    Texture2D vignette;
    float hitUntil,hitStrength,nextHurtSound,nextHeartbeat,lowWeight;
    bool acidHit,colorsApplied;
    public static float LowHealthWeight(float health,float maximum,float threshold=.3f) {
        if(health<=0 || maximum<=0)return 0;
        return Mathf.Clamp01((threshold-health/maximum)/Mathf.Max(.01f,threshold));
    }
    public static void Notify(CatPlayerController cat,float remaining,float maximum,float damage,bool acid=false) {
        if(!cat || !Application.isPlaying || damage<=0)return;
        var fx=cat.GetComponent<PlayerHurtFeedback>();if(!fx)fx=cat.gameObject.AddComponent<PlayerHurtFeedback>();
        fx.health=remaining;fx.maximumHealth=maximum;fx.acidHit=acid;
        fx.hitUntil=Time.unscaledTime+(acid?.22f:.38f);fx.hitStrength=Mathf.Clamp(damage/30,.2f,1);
        if(Time.unscaledTime>=fx.nextHurtSound && fx.voice) {
            fx.voice.pitch=Random.Range(.95f,1.04f);
            fx.voice.PlayOneShot(acid?fx.acidSound:fx.impactSound,acid?.45f:.8f);
            fx.nextHurtSound=Time.unscaledTime+(acid?.6f:.16f);
        }
    }
    public static void ResetFor(CatPlayerController cat) {
        if(!cat)return;var fx=cat.GetComponent<PlayerHurtFeedback>();if(!fx)return;
        fx.health=fx.maximumHealth;fx.hitUntil=0;fx.lowWeight=0;fx.nextHeartbeat=Time.unscaledTime+1;
        if(fx.voice)fx.voice.Stop();fx.RestoreColors();
    }
    void Awake() {
        cat=GetComponent<CatPlayerController>();boss=FindObjectOfType<SpiderBossController>();
        sprites=GetComponentsInChildren<SpriteRenderer>(true);baseColors=new Color[sprites.Length];
        for(int i=0;i<sprites.Length;i++)baseColors[i]=sprites[i].color;
        var audioObject=new GameObject("Player hurt sounds");audioObject.transform.SetParent(transform,false);
        voice=audioObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;voice.loop=false;voice.volume=PlayerPrefs.GetFloat("MasterVolume",1)*.65f;
        impactSound=Resources.Load<AudioClip>("PlayerFeedback/HurtImpact");acidSound=Resources.Load<AudioClip>("PlayerFeedback/AcidHurt");heartbeat=Resources.Load<AudioClip>("PlayerFeedback/Heartbeat");
        vignette=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Soft player damage vignette",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
            float radius=new Vector2((x/127f-.5f)*2,(y/127f-.5f)*2).magnitude;
            float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1.35f,radius));
            vignette.SetPixel(x,y,new Color(.58f,.025f,.015f,edge));
        }
        vignette.Apply(false,true);
    }
    bool Suppressed => !cat || cat.IsFrozen || health<=0 || Time.timeScale<=0 ||
        (boss && (boss.IsCutsceneActive || boss.IsReentering || boss.CurrentPhase>=SpiderBossController.Phase.Escaping));
    void RestoreColors() {
        if(!colorsApplied)return;
        for(int i=0;i<sprites.Length;i++)if(sprites[i])sprites[i].color=baseColors[i];
        colorsApplied=false;
    }
    void Update() {
        RestoreColors();
        if(voice)voice.volume=PlayerPrefs.GetFloat("MasterVolume",1)*.65f;
        float target=Suppressed?0:LowHealthWeight(health,maximumHealth,lowHealthThreshold);
        lowWeight=Mathf.MoveTowards(lowWeight,target,Time.unscaledDeltaTime*2);
        if(!Suppressed && target>0 && Time.unscaledTime>=nextHeartbeat && voice) {
            voice.pitch=1;voice.PlayOneShot(heartbeat,Mathf.Lerp(.10f,.28f,target));
            nextHeartbeat=Time.unscaledTime+Mathf.Lerp(1.45f,.95f,target);
        }
    }
    void LateUpdate() {
        if(Suppressed || Time.unscaledTime>=hitUntil)return;
        float flash=Mathf.Clamp01((hitUntil-Time.unscaledTime)/.18f)*.7f;
        for(int i=0;i<sprites.Length;i++)if(sprites[i]) {
            baseColors[i]=sprites[i].color;
            Color tint=Color.Lerp(baseColors[i],acidHit?new Color(.75f,1,.38f):new Color(1,.63f,.48f),flash);
            tint.a=baseColors[i].a;sprites[i].color=tint;
        }
        colorsApplied=true;
    }
    void OnGUI() {
        if(Suppressed || !vignette)return;
        float hit=Mathf.Clamp01((hitUntil-Time.unscaledTime)/.38f)*hitStrength;
        float pulse=.82f+.18f*Mathf.Sin(Time.unscaledTime*5.6f);
        float opacity=Mathf.Clamp(lowWeight*.42f*pulse+hit*.30f,0,.6f);
        if(opacity<=0)return;
        Camera camera=Camera.main;
        if(!camera || !camera.enabled || camera.targetTexture) {
            camera=null;
            foreach(var candidate in Camera.allCameras)
                if(candidate.enabled && !candidate.targetTexture && candidate.cameraType==CameraType.Game && (!camera || candidate.depth>camera.depth))camera=candidate;
        }
        if(!camera)return;
        Rect rect=camera.pixelRect;rect.y=Screen.height-rect.yMax;
        var old=GUI.color;int depth=GUI.depth;GUI.depth=-500;GUI.color=new Color(1,1,1,opacity);
        GUI.DrawTexture(rect,vignette,ScaleMode.StretchToFill,true);GUI.color=old;GUI.depth=depth;
    }
    void OnDisable(){RestoreColors();lowWeight=0;if(voice)voice.Stop();}
    void OnDestroy(){if(vignette)Destroy(vignette);}
}
