using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip initialClip;
    public AudioClip loopedClip;
    public bool playOnStart = true;
    public bool loopSecondClip = true;
    public bool DoNotDestroyOnLoad = true;

    [Header("Position → Music Mapping")]
    public Transform target;              // The object whose position drives the music time
    public Transform startPoint;          // Start of the range
    public Transform endPoint;            // End of the range
    public enum Axis { X, Y, Z }
    public Axis measureAxis = Axis.X;     // Axis to measure along
    public bool useScrubControl = false;   // Toggle scrubbing mode

    [Tooltip("Start time in seconds of the clip mapping range")]
    public float clipStartTime = 0f;
    [Tooltip("End time in seconds of the clip mapping range")]
    public float clipEndTime = 5f;

    private AudioSource _audioSource;
    private AudioClip _nextClip;
    private bool _loopNextClip;
    private bool pendingAfterOutro;
    private bool finishCurrentPlay;

    private void OnEnable(){
        SceneManager.sceneLoaded+=OnMusicSceneLoaded;
        OnMusicSceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }
    private void OnDisable(){SceneManager.sceneLoaded-=OnMusicSceneLoaded;}
    private void OnMusicSceneLoaded(Scene scene,LoadSceneMode mode){
        finishCurrentPlay=scene.name=="Lab_5";
        if(!finishCurrentPlay)return;
        if(!_audioSource)_audioSource=GetComponent<AudioSource>();
        // Disabling looping preserves the current sample position and lets it end naturally.
        if(_audioSource)_audioSource.loop=false;
        _nextClip=null;_loopNextClip=false;useScrubControl=false;
    }

    private void Awake()
    {
        if (DoNotDestroyOnLoad){
            transform.SetParent(null,true);DontDestroyOnLoad(transform.gameObject);
        }
        _audioSource = GetComponent<AudioSource>();
        if(_audioSource && BossMusicPlayback.OutroPlaying)_audioSource.Stop();
    }

    private void Start()
    {
        if (playOnStart && initialClip != null)
        {
            PlayMusic(initialClip);
            if (loopedClip != null)
            {
                QueueNextMusic(loopedClip, loopSecondClip);
            }
        }
    }

    private void Update()
    {
        if(!_audioSource)return;
        if(BossMusicPlayback.OutroPlaying) { if(_audioSource.isPlaying)_audioSource.Pause();return; }
        if(pendingAfterOutro) { pendingAfterOutro=false;_audioSource.Play(); }
        if(useScrubControl && (!initialClip || !target || !startPoint || !endPoint))useScrubControl=false;
        if (useScrubControl && initialClip != null && target != null && _audioSource.clip == initialClip)
        {
            if(initialClip.loadState!=AudioDataLoadState.Loaded){initialClip.LoadAudioData();return;}
            float start = GetAxisValue(startPoint.position);
            float end = GetAxisValue(endPoint.position);
            float pos = Mathf.InverseLerp(start, end, GetAxisValue(target.position));

            // Map 0..1 to custom clip range
            float mappedTime = Mathf.Lerp(clipStartTime, Mathf.Min(clipEndTime, initialClip.length), pos);

            if (!_audioSource.isPlaying)
                _audioSource.Play();

            _audioSource.time = mappedTime;

            // Stop scrubbing once we pass the end
            if (pos >= 1f && !_audioSource.loop)
            {
                useScrubControl = false;

                // Continue from clipEndTime onward naturally
                _audioSource.time = Mathf.Min(clipEndTime, initialClip.length - 0.01f);
            }
        }
        else
        {
            // Handle queued clips
            if (!_audioSource.isPlaying && _nextClip != null)
            {
                _audioSource.clip = _nextClip;
                _audioSource.loop = _loopNextClip;
                _audioSource.Play();
                _nextClip = null;
            }
        }
    }

    private float GetAxisValue(Vector3 v)
    {
        switch (measureAxis)
        {
            case Axis.Y: return v.y;
            case Axis.Z: return v.z;
            default: return v.x;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if(!clip || finishCurrentPlay)return;
        if(!_audioSource)_audioSource=GetComponent<AudioSource>();
        if(!_audioSource){_audioSource=gameObject.AddComponent<AudioSource>();_audioSource.playOnAwake=false;}
        _audioSource.loop = false;
        _audioSource.clip = clip;
        if(BossMusicPlayback.OutroPlaying) {_audioSource.Stop();pendingAfterOutro=true;}else _audioSource.Play();
        _nextClip = null;
    }

    public void StartMusic()
    {
        PlayMusic(initialClip);
        if (loopedClip != null)
        {
            QueueNextMusic(loopedClip, loopSecondClip);
        }
    }

    public void QueueNextMusic(AudioClip nextClip, bool loop = false)
    {
        if(finishCurrentPlay)return;
        _nextClip = nextClip;
        _loopNextClip = loop;
    }

    public void PrepareBossFade() { playOnStart=false;useScrubControl=false;_nextClip=null; }

    public void StopMusic()
    {
        if(!_audioSource)_audioSource=GetComponent<AudioSource>();
        if(_audioSource)_audioSource.Stop();
        pendingAfterOutro=false;useScrubControl=false;_nextClip = null;
    }
}
