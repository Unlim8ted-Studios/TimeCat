using UnityEngine;

public class BossVFXController : MonoBehaviour
{
    public GameObject hitEffectPrefab;
    public GameObject explosionPrefab;
    public GameObject timeShiftEffect;

    public void PlayHitVFX(Vector3 pos)
    {
        if (hitEffectPrefab)
            GameObject.Instantiate(hitEffectPrefab, pos, Quaternion.identity);
    }

    public void PlayExplosion(Vector3 pos)
    {
        if (explosionPrefab)
            GameObject.Instantiate(explosionPrefab, pos, Quaternion.identity);
    }

    public void PlayTimeShiftFX(Vector3 pos)
    {
        if (timeShiftEffect)
            GameObject.Instantiate(timeShiftEffect, pos, Quaternion.identity);
    }
}
