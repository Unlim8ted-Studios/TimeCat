using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class PostProcessFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Speed of normal fading (units per second).")]
    public float fadeSpeed = 1f;

    private Volume volume;
    private Coroutine fadeCoroutine;
    private Coroutine loopCoroutine;
    private bool isLooping = false;

    void Awake()
    {
        volume = GetComponent<Volume>();
    }

    // --- Basic one-way fades ---
    public void FadeIn() => StartFade(1f);
    public void FadeOut() => StartFade(0f);
    public void FadeTo(float targetWeight) => StartFade(Mathf.Clamp01(targetWeight));

    private void StartFade(float target)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);

        fadeCoroutine = StartCoroutine(FadeVolumeWeight(target));
    }

    private IEnumerator FadeVolumeWeight(float target)
    {
        while (!Mathf.Approximately(volume.weight, target))
        {
            volume.weight = Mathf.MoveTowards(volume.weight, target, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        volume.weight = target;
        fadeCoroutine = null;
    }

    // --- Continuous looping fade between 0 and 1 ---
    public void StartLoop()
    {
        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);

        isLooping = true;
        loopCoroutine = StartCoroutine(LoopFadeRoutine());
    }

    public void StopLoop()
    {
        isLooping = false;
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    private IEnumerator LoopFadeRoutine()
    {
        float direction = 1f; // 1 = fading in, -1 = fading out

        while (isLooping)
        {
            volume.weight += direction * fadeSpeed * Time.deltaTime;

            // Clamp and reverse direction when hitting bounds
            if (volume.weight >= 1f)
            {
                volume.weight = 1f;
                direction = -1f;
            }
            else if (volume.weight <= 0f)
            {
                volume.weight = 0f;
                direction = 1f;
            }

            yield return null;
        }
    }
}
