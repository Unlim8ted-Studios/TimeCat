using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeOpacity : MonoBehaviour
{
    [Tooltip("Default fade duration in seconds.")]
    public float duration = 1f;

    private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private Coroutine fadeCoroutine;

    void Awake()
    {
        spriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
    }

    /// <summary>
    /// Gradually changes the opacity of this object and all child sprites
    /// to the target value (0–1) over the set duration.
    /// Automatically stops any previous fade in progress.
    /// </summary>
    /// <param name="targetOpacity">Target alpha value (0 = transparent, 1 = opaque).</param>
    public void FadeTo(float targetOpacity)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToCoroutine(targetOpacity, duration));
    }

    private IEnumerator FadeToCoroutine(float targetOpacity, float duration)
    {
        if (spriteRenderers.Count == 0)
            yield break;

        // Capture starting colors
        List<Color> startColors = new List<Color>();
        foreach (var sr in spriteRenderers)
            startColors.Add(sr.color);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                if (spriteRenderers[i] == null) continue;

                Color c = startColors[i];
                c.a = Mathf.Lerp(startColors[i].a, targetOpacity, t);
                spriteRenderers[i].color = c;
            }

            yield return null;
        }

        // Snap to final target value
        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = targetOpacity;
            sr.color = c;
        }

        fadeCoroutine = null;
    }
}
