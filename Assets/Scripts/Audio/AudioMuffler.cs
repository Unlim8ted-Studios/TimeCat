using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioListener))]
public class AudioMuffler : MonoBehaviour
{
    [Header("Muffling Settings")]
    [Tooltip("Cutoff when time is frozen (Hz)")]
    public float muffledCutoff = 800f;
    [Tooltip("Resonance boost near cutoff (0.1 – 5). Lower = flatter, 1 = normal.")]
    public float muffledResonance = 0.5f;
    [Tooltip("Normal cutoff when unpaused (Hz)")]
    public float normalCutoff = 22000f;
    [Tooltip("Resonance when unpaused")]
    public float normalResonance = 1f;
    [Tooltip("High Cutoff for events (Hz)")]
    public float eventCutoff = 3945f;
    [Tooltip("Resonance for event cutoff")]
    public float eventResonance = 0.8f;

    [Tooltip("Keep active across scenes")]
    public bool DoNotDestroyOnLoad = true;

    [Header("Transition Settings")]
    public float transitionSpeed = 2f;

    private AudioLowPassFilter _filter;
    private AudioHighPassFilter _highFilter;
    private Coroutine _transitionRoutine;
    private bool _isEventActive = false;

    void Awake()
    {
        // Scene cameras and child listeners belong to their scene. Persist only
        // an explicitly standalone listener, avoiding duplicate cameras on transitions.
        if (DoNotDestroyOnLoad && !transform.parent && !GetComponent<Camera>())
            DontDestroyOnLoad(gameObject);

        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        _filter = gameObject.AddComponent<AudioLowPassFilter>();
        _highFilter = gameObject.AddComponent<AudioHighPassFilter>();

        _filter.cutoffFrequency = normalCutoff;
        _filter.lowpassResonanceQ = normalResonance;
        _highFilter.cutoffFrequency = 10f;
    }

    void Update()
    {

        if (Time.timeScale == 0f)
        {
            _filter.cutoffFrequency = muffledCutoff;
            _filter.lowpassResonanceQ = muffledResonance;
            _highFilter.cutoffFrequency = 200f;
        }
        else
        {
            _filter.cutoffFrequency = normalCutoff;
            _filter.lowpassResonanceQ = normalResonance;
            _highFilter.cutoffFrequency = 10f;
        }
    }
    public void SetNormal()
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _filter.cutoffFrequency = normalCutoff;
        _filter.lowpassResonanceQ = normalResonance;
        _highFilter.cutoffFrequency = 10f;
    }

    public void SetMuffled()
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _filter.cutoffFrequency = muffledCutoff;
        _filter.lowpassResonanceQ = muffledResonance;
        _highFilter.cutoffFrequency = 200f;
    }

    /// <summary>
    /// Smoothly transitions to event-based cutoff (triggerable via UnityEvent).
    /// </summary>
    public void SetEventCutoff(float duration = 1.5f)
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(EventCutoffRoutine(duration));
    }

    private IEnumerator EventCutoffRoutine(float duration)
    {
        _isEventActive = true;

        // Smoothly fade to event sound
        yield return StartCoroutine(TransitionFilters(eventCutoff, eventResonance, 50f));

        // Hold event effect
        yield return new WaitForSecondsRealtime(duration);

        // Restore correct state instantly
        if (Time.timeScale == 0f)
        {
            _filter.cutoffFrequency = muffledCutoff;
            _filter.lowpassResonanceQ = muffledResonance;
            _highFilter.cutoffFrequency = 200f;
        }
        else
        {
            _filter.cutoffFrequency = normalCutoff;
            _filter.lowpassResonanceQ = normalResonance;
            _highFilter.cutoffFrequency = 10f;
        }

        _isEventActive = false;
    }

    private IEnumerator TransitionFilters(float targetLow, float targetResonance, float targetHigh)
    {
        float startLow = _filter.cutoffFrequency;
        float startRes = _filter.lowpassResonanceQ;
        float startHigh = _highFilter.cutoffFrequency;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * transitionSpeed;
            _filter.cutoffFrequency = Mathf.Lerp(startLow, targetLow, t);
            _filter.lowpassResonanceQ = Mathf.Lerp(startRes, targetResonance, t);
            _highFilter.cutoffFrequency = Mathf.Lerp(startHigh, targetHigh, t);
            yield return null;
        }
    }
}
