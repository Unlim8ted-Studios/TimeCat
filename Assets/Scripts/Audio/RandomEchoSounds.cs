using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomEchoSounds : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip[] clips;           // List of sounds to pick from
    public float minDelay = 5f;         // Minimum seconds between sounds
    public float maxDelay = 15f;        // Maximum seconds between sounds
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Echo Settings")]
    public bool useEchoFilter = true;
    public float delay = 300f;          // Echo delay (ms)
    public float decayRatio = 0.5f;     // How fast echoes fade
    public float wetMix = 1f;           // Volume of the echo
    public float dryMix = 1f;           // Volume of original sound

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (useEchoFilter)
        {
            AudioEchoFilter echoFilter = gameObject.AddComponent<AudioEchoFilter>();
            echoFilter.delay = delay;
            echoFilter.decayRatio = decayRatio;
            echoFilter.wetMix = wetMix;
            echoFilter.dryMix = dryMix;
        }

        StartCoroutine(PlayRandomSounds());
    }

    private System.Collections.IEnumerator PlayRandomSounds()
    {
        while (true)
        {
            float waitTime = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(waitTime);

            if (clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
