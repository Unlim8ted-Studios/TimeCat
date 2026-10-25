using UnityEngine;

[CreateAssetMenu(fileName = "BossStateData", menuName = "Boss/StateData")]
public class BossStateData : ScriptableObject
{
    public float maxHealth = 500f;
    public float coreHealth = 100f;
    public float sweepDamage = 20f;
    public float stabDamage = 30f;
    public float sweepCooldown = 2f;
    public float stabCooldown = 3f;
}
