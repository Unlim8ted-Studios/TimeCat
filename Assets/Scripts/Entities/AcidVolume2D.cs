using System.Collections.Generic;
using UnityEngine;
// Damage belongs to each visible pool volume, including the original flat buoyancy tiles.
[RequireComponent(typeof(Collider2D))]
public sealed class AcidVolume2D : MonoBehaviour {
    public float damagePerSecond=15,damageInterval=.5f;
    static readonly Dictionary<CatPlayerController,float> nextDamage=new Dictionary<CatPlayerController,float>();
    readonly Collider2D[] contacts=new Collider2D[32];
    Collider2D volume;
    readonly HashSet<CatPlayerController> immersed=new HashSet<CatPlayerController>();
    readonly List<CatPlayerController> released=new List<CatPlayerController>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetTicks(){nextDamage.Clear();}
    public static void ResetDamage(){nextDamage.Clear();}
    void Awake(){Configure();}
    void OnEnable(){Configure();}
    public void Configure(){
        volume=GetComponent<Collider2D>();
        volume.isTrigger=true;volume.usedByEffector=false;
        // Old pool tiles explicitly bounced the player on contact, independently of buoyancy.
        foreach(var trigger in GetComponents<ColliderEventTrigger>()){
            if(trigger.onPressed==null)continue;
            for(int i=0;i<trigger.onPressed.GetPersistentEventCount();i++)
                if(trigger.onPressed.GetPersistentTarget(i) is CatPlayerController && trigger.onPressed.GetPersistentMethodName(i)=="Jump")
                    trigger.onPressed.SetPersistentListenerState(i,UnityEngine.Events.UnityEventCallState.Off);
        }
        // The native effector lifts the cat above the liquid and fights the floating island controller.
        foreach(var effector in GetComponents<BuoyancyEffector2D>())effector.enabled=false;
    }
    public bool TryGetSurface(float worldX,out float y){
        y=0;if(!isActiveAndEnabled || !volume || !volume.enabled)return false;
        var bounds=volume.bounds;if(worldX<bounds.min.x || worldX>bounds.max.x)return false;
        y=bounds.max.y;return true;
    }
    void FixedUpdate(){
        if(!volume || !volume.enabled || Time.timeScale<=0)return;
        var filter=new ContactFilter2D();filter.SetLayerMask(~0);filter.useTriggers=false;
        int count=volume.OverlapCollider(filter,contacts);
        for(int i=0;i<count;i++){
            var cat=contacts[i].GetComponentInParent<CatPlayerController>();if(cat)immersed.Add(cat);
        }
        released.Clear();var bounds=volume.bounds;
        foreach(var cat in immersed){
            if(!cat || !cat.gameObject.activeInHierarchy){released.Add(cat);continue;}
            var shape=cat.GetComponent<Collider2D>();
            if(!shape || shape.bounds.max.x<bounds.min.x || shape.bounds.min.x>bounds.max.x || shape.bounds.min.y>=bounds.max.y){released.Add(cat);continue;}
            // Sinking below a shallow trigger's bottom is still immersion, not an escape from the pool.
            DamageCat(shape,damagePerSecond,damageInterval,bounds.max.y);
        }
        foreach(var cat in released)immersed.Remove(cat);
    }
    void OnTriggerStay2D(Collider2D other){
        var cat=other.GetComponentInParent<CatPlayerController>();if(cat)immersed.Add(cat);
        DamageCat(other,damagePerSecond,damageInterval,volume?volume.bounds.max.y:float.NegativeInfinity);
    }
    void OnDisable(){immersed.Clear();released.Clear();}
    public static void DamageCat(Collider2D collider,float rate,float interval,float surface=float.NegativeInfinity){
        if(!Application.isPlaying || Time.timeScale<=0 || !collider)return;
        var cat=collider.GetComponentInParent<CatPlayerController>();if(!cat || cat.IsFrozen)return;
        if(!float.IsNegativeInfinity(surface))cat.ContactAcid(surface);
        float next;if(nextDamage.TryGetValue(cat,out next) && Time.time<next)return;
        interval=Mathf.Max(.1f,interval);nextDamage[cat]=Time.time+interval;
        cat.TakeEnvironmentalDamage(Mathf.Max(0,rate)*interval);
    }
}
