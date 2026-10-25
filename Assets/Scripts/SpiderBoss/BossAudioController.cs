using UnityEngine;

public class BossAudioController : MonoBehaviour
{
    public AudioSource source;
    public AudioClip sweepSound;
    public AudioClip stabSound;
    public AudioClip damageSound;
    public AudioClip curlSound;
    public AudioClip escapeSound;
    public AudioClip screechSound;

    void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void PlaySweepSound() => PlayClip(sweepSound);
    public void PlayStabSound() => PlayClip(stabSound);
    public void PlayDamageSound() => PlayClip(damageSound);
    public void PlayCurlSound() => PlayClip(curlSound);
    public void PlayScreechSound() => PlayClip(screechSound);
    public void PlayEscapeSound() => PlayClip(escapeSound);

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        source.PlayOneShot(clip);
    }
}

