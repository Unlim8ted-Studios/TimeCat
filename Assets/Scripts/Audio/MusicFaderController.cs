using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicFaderController : MonoBehaviour
{
    [Header("Target Music Controller")]
    [Tooltip("Name of the GameObject containing the original music AudioSource. Optional.")]
    public string targetControllerName = "music manager";

    [Tooltip("Fade time in seconds.")]
    public float fadeDuration = 2f;

    public float Volume = 1f;

    [Header("New Music Clips")]
    public AudioClip initialClip;
    public AudioClip loopedClip;
    public bool loopSecondClip = true;

    private AudioSource targetSource;
    private AudioSource newSource;

    private Coroutine crossfadeRoutine;
    private float targetOriginalVolume = 1f;

    private void Awake()
    {
        newSource = GetComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.spatialBlend=0;newSource.dopplerLevel=0;
        newSource.volume = 0f;
    }

    private void Start()
    {
        FindTargetSource();
    }

    private void FindTargetSource()
    {
        if (targetSource != null)
            return;

        if (string.IsNullOrEmpty(targetControllerName))
            return;

        GameObject obj = GameObject.Find(targetControllerName);

        if (obj == null)
            return;

        AudioSource source = obj.GetComponent<AudioSource>();

        if (source != null && source != newSource)
        {
            targetSource = source;
            targetOriginalVolume = targetSource.volume;
        }
    }

    /// <summary>
    /// Fades into the new music, with or without an original music controller.
    /// </summary>
    public void CrossfadeToNewMusic()
    {
        if(newSource && newSource.isPlaying && (newSource.clip==initialClip || newSource.clip==loopedClip))return;
        if (initialClip == null && loopedClip == null)
        {
            Debug.LogWarning("No music clips assigned.");
            return;
        }

        if (targetSource == null)
            FindTargetSource();

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        crossfadeRoutine = StartCoroutine(CrossfadeRoutine());
    }

    private IEnumerator CrossfadeRoutine()
    {
        while(BossMusicPlayback.OutroPlaying)yield return null;
        float startVolume = targetSource != null
            ? targetSource.volume
            : 0f;

        float time = 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        // Start the new music, even without an original source.
        newSource.Stop();
        newSource.volume = 0f;

        if (initialClip != null)
        {
            newSource.clip = initialClip;
            newSource.loop = false;
        }
        else
        {
            newSource.clip = loopedClip;
            newSource.loop = loopSecondClip;
        }

        yield return LoadClip(newSource.clip);
        if(!newSource.clip || newSource.clip.loadState!=AudioDataLoadState.Loaded){crossfadeRoutine=null;yield break;}
        newSource.Play();

        // Fade out the original (if present) and fade in the new.
        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);

            if (targetSource != null)
            {
                targetSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    t
                );
            }

            newSource.volume = Mathf.Lerp(
                0f,
                Mathf.Clamp01(Volume),
                t
            );

            yield return null;
        }

        if (targetSource != null)
            targetSource.volume = 0f;

        newSource.volume = Mathf.Clamp01(Volume);

        // Switch to the second clip after the initial clip finishes.
        if (initialClip != null && loopedClip != null)
        {
            yield return new WaitUntil(() => !AudioListener.pause && Time.timeScale>0 && !newSource.isPlaying);
            yield return LoadClip(loopedClip);
            if(loopedClip.loadState!=AudioDataLoadState.Loaded){crossfadeRoutine=null;yield break;}

            newSource.clip = loopedClip;
            newSource.loop = loopSecondClip;
            newSource.volume = Mathf.Clamp01(Volume);
            newSource.Play();
        }

        crossfadeRoutine = null;
    }

    /// <summary>
    /// Fades back to original music if available.
    /// Otherwise, fades the new music out to silence.
    /// </summary>
    public void FadeBackToOriginalMusic()
    {
        if (targetSource == null)
            FindTargetSource();

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        crossfadeRoutine = StartCoroutine(FadeBackRoutine());
    }

    private IEnumerator FadeBackRoutine()
    {
        while(BossMusicPlayback.OutroPlaying)yield return null;
        float startNewVol = newSource.volume;

        float startOriginalVol = targetSource != null
            ? targetSource.volume
            : 0f;

        float time = 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);

            // Always fade out the new music.
            newSource.volume = Mathf.Lerp(
                startNewVol,
                0f,
                t
            );

            // Only fade in original music if it exists.
            if (targetSource != null)
            {
                targetSource.volume = Mathf.Lerp(
                    startOriginalVol,
                    targetOriginalVolume,
                    t
                );
            }

            yield return null;
        }

        newSource.Stop();
        newSource.volume = 0f;

        if (targetSource != null)
            targetSource.volume = targetOriginalVolume;

        crossfadeRoutine = null;
    }
    IEnumerator LoadClip(AudioClip clip){
        if(!clip)yield break;if(clip.loadState!=AudioDataLoadState.Loaded)clip.LoadAudioData();
        float timeout=Time.realtimeSinceStartup+20;
        while(clip.loadState==AudioDataLoadState.Loading && Time.realtimeSinceStartup<timeout)yield return null;
        if(clip.loadState!=AudioDataLoadState.Loaded)Debug.LogWarning("Music clip could not load: "+clip.name);
    }
    public void PrepareBossFade() { StopAllCoroutines();crossfadeRoutine=null; }
    public void StopForBossOutro() { PrepareBossFade();if(newSource)newSource.Stop(); }
} 