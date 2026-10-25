using UnityEngine;
using System.Collections;

public abstract class BossAttack
{
    protected SpiderBossController boss;

    public BossAttack(SpiderBossController boss)
    {
        this.boss = boss;
    }

    public abstract IEnumerator Execute();
}
