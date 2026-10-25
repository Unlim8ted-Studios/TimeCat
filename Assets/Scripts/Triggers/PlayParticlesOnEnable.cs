using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PlayParticlesOnEnable : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        ps.Clear();
        ps.Play();
    }

    void OnDisable()
    {
        ps.Stop();
    }
}
