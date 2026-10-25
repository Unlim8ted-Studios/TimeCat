using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns encounter music independently of the boss and the loaded scene.
public sealed class BossMusicPlayback : MonoBehaviour {
    static BossMusicPlayback instance;
    AudioSource outro;
    bool protectedOutro;
    double outroEndsAt;
    public static bool OutroPlaying => instance && instance.protectedOutro;
    static BossMusicPlayback Get() {
        if(instance)return instance;
        var go=new GameObject("Boss music continuity");instance=go.AddComponent<BossMusicPlayback>();
        DontDestroyOnLoad(go);return instance;
    }
    public static void FadeOnEntry(float seconds=1.2f) { var owner=Get();owner.StartCoroutine(owner.FadeExisting(seconds)); }
    IEnumerator FadeExisting(float seconds) {
        var sources=new List<AudioSource>();
        foreach(var music in FindObjectsOfType<MusicController>()) { music.PrepareBossFade();var source=music.GetComponent<AudioSource>();if(source && !sources.Contains(source))sources.Add(source); }
        foreach(var music in FindObjectsOfType<MusicFaderController>()) { if(music.initialClip && music.initialClip.name=="LabBossIntro")continue;music.PrepareBossFade();var source=music.GetComponent<AudioSource>();if(source && !sources.Contains(source))sources.Add(source); }
        var volumes=new List<float>();var clips=new List<AudioClip>();
        foreach(var source in sources){volumes.Add(source.volume);clips.Add(source.clip);}
        for(float elapsed=0;elapsed<seconds;elapsed+=Time.unscaledDeltaTime) {
            if(protectedOutro)yield break;
            for(int i=0;i<sources.Count;i++)if(sources[i] && sources[i].clip==clips[i])sources[i].volume=volumes[i]*(1-Mathf.Clamp01(elapsed/Mathf.Max(.01f,seconds)));
            yield return null;
        }
        for(int i=0;i<sources.Count;i++)if(sources[i] && sources[i].clip==clips[i]) {sources[i].Stop();sources[i].volume=volumes[i];}
    }
    public static void PlayOutro(AudioClip clip) {
        if(!clip)return;
        var owner=Get();if(owner.protectedOutro && owner.outro && owner.outro.clip==clip)return;
        owner.StopAllCoroutines();
        foreach(var music in FindObjectsOfType<MusicController>())music.StopMusic();
        foreach(var music in FindObjectsOfType<MusicFaderController>())music.StopForBossOutro();
        if(!owner.outro)owner.outro=owner.gameObject.AddComponent<AudioSource>();
        owner.outro.playOnAwake=false;owner.outro.spatialBlend=0;owner.outro.loop=false;
        owner.outro.volume=1;owner.outro.pitch=1;owner.outro.ignoreListenerPause=true;owner.outro.clip=clip;
        owner.protectedOutro=true;owner.outroEndsAt=double.PositiveInfinity;owner.StartCoroutine(owner.LoadAndPlayOutro(clip));
    }
    IEnumerator LoadAndPlayOutro(AudioClip clip) {
        if(clip.loadState!=AudioDataLoadState.Loaded)clip.LoadAudioData();
        float timeout=Time.realtimeSinceStartup+15;
        while(clip && clip.loadState!=AudioDataLoadState.Loaded && clip.loadState!=AudioDataLoadState.Failed && Time.realtimeSinceStartup<timeout)yield return null;
        if(!clip || clip.loadState!=AudioDataLoadState.Loaded){protectedOutro=false;yield break;}
        outro.Play();outroEndsAt=AudioSettings.dspTime+clip.length;
    }
    void Update(){if(protectedOutro && outro && AudioSettings.dspTime>=outroEndsAt && !outro.isPlaying)protectedOutro=false;}
    void OnDestroy(){if(instance==this)instance=null;}
}
