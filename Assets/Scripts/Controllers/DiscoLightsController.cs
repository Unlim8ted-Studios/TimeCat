using UnityEngine;
using System.Collections.Generic;

public class DiscoLightsController : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Beats per minute of the song.")]
    public float bpm = 120f;

    [Tooltip("How many degrees to rotate per second (base rotation).")]
    public float baseRotationSpeed = 30f;

    [Header("Beat Kick")]
    [Tooltip("Extra rotation added on each beat.")]
    public float beatRotationAmount = 25f;

    [Tooltip("How quickly the beat kick fades (higher = snappier).")]
    public float beatDecay = 6f;

    [Header("Wobble (Optional)")]
    public bool wobble = true;
    public float wobbleAmplitude = 6f;
    public float wobbleFrequency = 2f;

    [Header("Axis")]
    public Vector3 rotationAxis = Vector3.forward;

    float beatTimer;
    float beatKick;

    void Update()
    {
        if (bpm <= 0f) return;

        float secondsPerBeat = 60f / bpm;
        beatTimer += Time.deltaTime;

        // Beat trigger
        if (beatTimer >= secondsPerBeat)
        {
            beatTimer -= secondsPerBeat;
            beatKick += beatRotationAmount;
        }

        // Decay beat kick
        beatKick = Mathf.Lerp(beatKick, 0f, Time.deltaTime * beatDecay);

        // Base rotation
        float rotationThisFrame = baseRotationSpeed * Time.deltaTime;

        // Add beat kick
        rotationThisFrame += beatKick * Time.deltaTime;

        // Optional wobble
        if (wobble)
        {
            float wobbleOffset =
                Mathf.Sin(Time.time * wobbleFrequency * Mathf.PI * 2f)
                * wobbleAmplitude
                * Time.deltaTime;

            rotationThisFrame += wobbleOffset;
        }

        transform.Rotate(rotationAxis.normalized, rotationThisFrame, Space.Self);
    }
}