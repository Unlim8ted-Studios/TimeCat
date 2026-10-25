using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(AudioSource))]
public class StormController2D : MonoBehaviour
{
    [Header("Lighting Settings")]
    [Tooltip("The 2D light used for flashes (should be a Global or Point Light2D).")]
    public Light2D lightningLight;

    [Tooltip("Minimum and maximum flash intensity.")]
    public Vector2 intensityRange = new Vector2(2f, 8f);

    [Tooltip("Minimum and maximum flash duration (seconds).")]
    public Vector2 flashDurationRange = new Vector2(0.05f, 0.3f);

    [Tooltip("Delay between flashes (seconds).")]
    public Vector2 timeBetweenFlashes = new Vector2(3f, 8f);

    [Tooltip("Chance (0–1) for double flashes (rapid second flicker).")]
    [Range(0f, 1f)] public float doubleFlashChance = 0.3f;

    [Header("Thunder Settings")]
    [Tooltip("Audio clips for thunder sounds.")]
    public AudioClip[] thunderClips;

    [Tooltip("Delay between lightning flash and thunder (seconds).")]
    public Vector2 thunderDelayRange = new Vector2(0.5f, 4f);

    [Tooltip("Volume range for thunder sounds.")]
    public Vector2 thunderVolumeRange = new Vector2(0.6f, 1f);

    [Header("Global Storm Settings")]
    [Tooltip("Enable/disable the storm.")]
    public bool stormActive = true;

    [Tooltip("Randomize the start delay to avoid instant lightning.")]
    public Vector2 startDelay = new Vector2(0f, 3f);

    private AudioSource audioSource;
    private float baseIntensity;
    private Coroutine stormRoutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (lightningLight != null)
            baseIntensity = lightningLight.intensity;
    }

    void OnEnable()
    {
        if (stormRoutine == null && stormActive)
            stormRoutine = StartCoroutine(StormRoutine());
    }

    void OnDisable()
    {
        if (stormRoutine != null)
            StopCoroutine(stormRoutine);
    }

    IEnumerator StormRoutine()
    {
        yield return new WaitForSeconds(Random.Range(startDelay.x, startDelay.y));

        while (stormActive)
        {
            // Random delay between flashes
            float wait = Random.Range(timeBetweenFlashes.x, timeBetweenFlashes.y);
            yield return new WaitForSeconds(wait);

            StartCoroutine(LightningFlash());

            // Optional double flash
            if (Random.value < doubleFlashChance)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
                StartCoroutine(LightningFlash());
            }
        }
    }

    IEnumerator LightningFlash()
    {
        if (lightningLight == null)
            yield break;

        // Flash parameters
        float flashIntensity = Random.Range(intensityRange.x, intensityRange.y);
        float flashDuration = Random.Range(flashDurationRange.x, flashDurationRange.y);

        // Brighten
        lightningLight.intensity = flashIntensity;
        yield return new WaitForSeconds(flashDuration);

        // Fade out smoothly
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flashDuration;
            lightningLight.intensity = Mathf.Lerp(flashIntensity, baseIntensity, t);
            yield return null;
        }

        lightningLight.intensity = baseIntensity;

        // Schedule thunder
        if (thunderClips.Length > 0)
            StartCoroutine(PlayThunderWithDelay());
    }

    IEnumerator PlayThunderWithDelay()
    {
        float delay = Random.Range(thunderDelayRange.x, thunderDelayRange.y);
        yield return new WaitForSeconds(delay);

        AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
        audioSource.volume = Random.Range(thunderVolumeRange.x, thunderVolumeRange.y);
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip);
    }

    // Public controls
    public void StartStorm()
    {
        stormActive = true;
        if (stormRoutine == null)
            stormRoutine = StartCoroutine(StormRoutine());
    }

    public void StopStorm()
    {
        stormActive = false;
        if (stormRoutine != null)
        {
            StopCoroutine(stormRoutine);
            stormRoutine = null;
        }
        if (lightningLight != null)
            lightningLight.intensity = baseIntensity;
    }
}
