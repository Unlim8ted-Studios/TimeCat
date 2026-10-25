using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SetAudioClipOnStart : MonoBehaviour
{
    [Header("Clip to Play On Start")]
    public AudioClip newClip;

    [Header("Play Settings")]
    public bool playOnStart = true;
    public bool loop = false;

    private AudioSource audioSource;

    void Awake()
    {
        // Cache the AudioSource
        audioSource = GetComponent<AudioSource>();

        // Change clip if one is assigned
        if (newClip != null)
        {
            audioSource.clip = newClip;
        }

        // Apply loop setting
        audioSource.loop = loop;
    }

    void Start()
    {
        // Play if requested
        if (playOnStart && audioSource.clip != null)
        {
            // Slight delay to avoid race condition on scene load
            audioSource.PlayDelayed(0.05f);
        }
    }
}
