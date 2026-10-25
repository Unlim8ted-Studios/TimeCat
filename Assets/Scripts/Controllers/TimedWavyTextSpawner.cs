using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimedWavyTextSpawner : MonoBehaviour
{
    public enum OriginSlot { Position1, Position2 }

    [System.Serializable]
    public class OriginSettings
    {
        public OriginSlot slot = OriginSlot.Position1;

        [Tooltip("Local offset from the spawner transform.")]
        public Vector3 localOffset = new Vector3(0, 1f, 0);

        [Tooltip("Default drift direction (units/sec).")]
        public Vector2 baseDirection = new Vector2(0.25f, 0.55f);

        [Header("Default Wave (used if event doesn't override)")]
        public float waveAmplitude = 0.12f;
        public float waveFrequency = 2.0f;
        public float wavePhase = 0f;

        [Header("Default Scale (used if event doesn't override)")]
        public float startScale = 0.75f;
        public float peakScale = 1.12f;

        [Header("Default Duration (used if event doesn't override)")]
        public float duration = 1.6f;

        [Header("Default Color (used if event doesn't override)")]
        public Color color = Color.white;
    }

    [System.Serializable]
    public class TimedTextEvent
    {
        public float timeFromStart = 0f;

        [TextArea]
        public string text = "Lyric line";

        public OriginSlot origin = OriginSlot.Position1;

        [Tooltip("If true, use OriginSettings values for these fields when left at 0/defaults.")]
        public bool useOriginDefaults = true;

        public Color color = default;          // if default + useOriginDefaults => origin color
        public float duration = 0f;            // 0 => origin duration
        public Vector2 baseDirection = Vector2.zero; // zero => origin baseDirection

        public float waveAmplitude = 0f;       // 0 => origin waveAmplitude
        public float waveFrequency = 0f;       // 0 => origin waveFrequency
        public float wavePhase = 0f;           // 0 => origin wavePhase (note: if you want a real 0 phase override, set useOriginDefaults=false)

        public float startScale = 0f;          // 0 => origin startScale
        public float peakScale = 0f;           // 0 => origin peakScale
    }

    [Header("Font / Rendering")]
    public Font font;
    public int fontSize = 64;
    public float characterSize = 0.1f;
    public int sortingOrder = 5;
    
    public string sortingLayerName = "Default";

    [Header("Origins")]
    public OriginSettings originPos1 = new OriginSettings { slot = OriginSlot.Position1, localOffset = new Vector3(-0.35f, 1.0f, 0f), baseDirection = new Vector2(0.22f, 0.55f) };
    public OriginSettings originPos2 = new OriginSettings { slot = OriginSlot.Position2, localOffset = new Vector3(0.35f, 0.85f, 0f), baseDirection = new Vector2(-0.20f, 0.60f) };

    [Header("Events")]
    public List<TimedTextEvent> events = new List<TimedWavyTextSpawner.TimedTextEvent>()
{
    // Origin Position 1
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 9f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "Sorry to wake you up,\nfat, strange looking one.",
        useOriginDefaults = true,
        wavePhase = 0.0f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 17f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "You've got red and grey bits\nlying round, looks fun.",
        useOriginDefaults = true,
        wavePhase = 0.7f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 27f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "Tempting, sure,\nbut a bit too tough.",
        useOriginDefaults = true,
        wavePhase = 1.4f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 31f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "Tried to chew one,\nwasn't soft enough.",
        useOriginDefaults = true,
        wavePhase = 2.1f
    },

    // Origin Position 2
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 36f,
        origin = TimedWavyTextSpawner.OriginSlot.Position2,
        text = "You ate my button,\nyou fur-brained fool.",
        useOriginDefaults = true,
        // punchier moment:
        duration = 2.1f,
        waveAmplitude = 0.20f,
        waveFrequency = 2.8f,
        peakScale = 1.28f,
        wavePhase = 0.4f
    },

    // Origin Position 1
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 39f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "You left it lying down,\nlooking cool.",
        useOriginDefaults = true,
        wavePhase = 2.8f
    },

    // Origin Position 2
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 44f,
        origin = TimedWavyTextSpawner.OriginSlot.Position2,
        text = "Not cool.\nIt's a quantum flux.",
        useOriginDefaults = true,
        duration = 2.0f,
        waveAmplitude = 0.16f,
        waveFrequency = 2.2f,
        peakScale = 1.18f,
        wavePhase = 1.0f
    },

    // Origin Position 1
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 49f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "Guess I'll need\nnine lives for luck.",
        useOriginDefaults = true,
        duration = 1.9f,
        waveAmplitude = 0.14f,
        waveFrequency = 2.0f,
        wavePhase = 3.5f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 59f,
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "Any way out,\nany door that's kind.",
        useOriginDefaults = true,
        duration = 2.3f,
        waveAmplitude = 0.10f,
        waveFrequency = 1.6f,
        peakScale = 1.14f,
        wavePhase = 4.2f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 71f, // 1:11
        origin = TimedWavyTextSpawner.OriginSlot.Position1,
        text = "This place hums wrong,\nlike it's losing its mind.",
        useOriginDefaults = true,
        duration = 2.6f,
        baseDirection = new Vector2(0.18f, 0.42f), // slightly heavier/slower
        waveAmplitude = 0.08f,
        waveFrequency = 1.3f,
        peakScale = 1.10f,
        wavePhase = 4.9f
    },

    // Origin Position 2
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 85f, // 1:25
        origin = TimedWavyTextSpawner.OriginSlot.Position2,
        text = "Maybe one piston door\ndown the hall.",
        useOriginDefaults = true,
        duration = 2.2f,
        waveAmplitude = 0.14f,
        waveFrequency = 1.8f,
        peakScale = 1.18f,
        wavePhase = 2.0f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 91f, // 1:31
        origin = TimedWavyTextSpawner.OriginSlot.Position2,
        text = "Half alive,\nhalf rust,\nhalf wall.",
        useOriginDefaults = true,
        duration = 2.4f,
        waveAmplitude = 0.12f,
        waveFrequency = 1.5f,
        peakScale = 1.20f,
        wavePhase = 2.7f
    },
    new TimedWavyTextSpawner.TimedTextEvent {
        timeFromStart = 94f, // 1:34
        origin = TimedWavyTextSpawner.OriginSlot.Position2,
        text = "Keep still.\nI'll wake it slow.",
        useOriginDefaults = true,
        duration = 2.7f,
        baseDirection = new Vector2(-0.12f, 0.38f), // careful drift
        waveAmplitude = 0.06f,
        waveFrequency = 1.0f,
        peakScale = 1.10f,
        wavePhase = 3.4f
    },
};


    Coroutine playRoutine;
    readonly List<GameObject> activeTexts = new List<GameObject>();
    float playStartTime;
    bool isPlaying;

    void OnDisable() => StopAndClear();

    public void Play()
    {
        StopAndClear();
        isPlaying = true;
        playStartTime = Time.time;
        playRoutine = StartCoroutine(PlayTimeline());
    }

    public void StopAndClear()
    {
        isPlaying = false;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        for (int i = activeTexts.Count - 1; i >= 0; i--)
            if (activeTexts[i] != null) Destroy(activeTexts[i]);

        activeTexts.Clear();
    }

    IEnumerator PlayTimeline()
    {
        var sorted = new List<TimedTextEvent>(events);
        sorted.Sort((a, b) => a.timeFromStart.CompareTo(b.timeFromStart));

        foreach (var e in sorted)
        {
            float targetTime = playStartTime + Mathf.Max(0f, e.timeFromStart);
            while (isPlaying && Time.time < targetTime) yield return null;
            if (!isPlaying) yield break;

            SpawnEventText(e);
        }

        playRoutine = null;
    }

    OriginSettings GetOrigin(OriginSlot slot) =>
        slot == OriginSlot.Position2 ? originPos2 : originPos1;

void SpawnEventText(TimedTextEvent e)
{
    OriginSettings o = GetOrigin(e.origin);

    // Resolve event values (optionally fallback to origin defaults)
    Color color = (!e.useOriginDefaults || e.color != default) ? e.color : o.color;
    if (color == default) color = o.color;

    float duration = (e.duration > 0f) ? e.duration : o.duration;
    Vector2 baseDir = (e.baseDirection.sqrMagnitude > 0.0001f) ? e.baseDirection : o.baseDirection;
    float amp = (e.waveAmplitude > 0f) ? e.waveAmplitude : o.waveAmplitude;
    float freq = (e.waveFrequency > 0f) ? e.waveFrequency : o.waveFrequency;
    float phase = (e.wavePhase != 0f || !e.useOriginDefaults) ? e.wavePhase : o.wavePhase;
    float startScale = (e.startScale > 0f) ? e.startScale : o.startScale;
    float peakScale = (e.peakScale > 0f) ? e.peakScale : o.peakScale;

    GameObject obj = new GameObject("TimedText");
    obj.transform.position = transform.TransformPoint(o.localOffset);

    TextMesh tm = obj.AddComponent<TextMesh>();
    tm.text = e.text;
    tm.font = font;
    tm.fontSize = fontSize;
    tm.characterSize = characterSize;
    tm.color = new Color(color.r, color.g, color.b, 0f);
    tm.anchor = TextAnchor.MiddleCenter;

    MeshRenderer r = obj.GetComponent<MeshRenderer>();
    if (font != null) r.material = font.material;
    r.sortingLayerName = sortingLayerName;
    r.sortingOrder = sortingOrder;

    activeTexts.Add(obj);
    StartCoroutine(
        AnimateWavy(obj, tm, duration, baseDir, amp, freq, phase, startScale, peakScale, color)
    );
}

    IEnumerator AnimateWavy(GameObject obj, TextMesh tm,
        float duration, Vector2 baseDirection, float waveAmplitude, float waveFrequency, float wavePhase,
        float startScale, float peakScale, Color color)
    {
        float t = 0f;

        Vector2 dir = baseDirection.sqrMagnitude < 0.0001f ? Vector2.up : baseDirection;
        Vector2 dirNorm = dir.normalized;
        Vector2 perp = new Vector2(-dirNorm.y, dirNorm.x);

        Vector3 startPos = obj.transform.position;

        while (t < duration)
        {
            float progress = t / duration;

            Vector3 drift = (Vector3)(dir * t);

            float wave = Mathf.Sin((t * Mathf.PI * 2f * waveFrequency) + wavePhase);
            Vector3 waveOffset = (Vector3)(perp * (wave * waveAmplitude));

            obj.transform.position = startPos + drift + waveOffset;

            float scale = Mathf.Lerp(startScale, peakScale, Mathf.Sin(progress * Mathf.PI));
            obj.transform.localScale = new Vector3(scale, scale, scale);

            float alpha = Mathf.Sin(progress * Mathf.PI);
            tm.color = new Color(color.r, color.g, color.b, alpha);

            t += Time.deltaTime;
            yield return null;
        }

        activeTexts.Remove(obj);
        if (obj != null) Destroy(obj);
    }
}
