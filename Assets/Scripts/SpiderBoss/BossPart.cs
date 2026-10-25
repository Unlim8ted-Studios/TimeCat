using System.Collections.Generic;
using UnityEngine;

public class BossPart : MonoBehaviour {
    public float health = 50;
    public bool isDestroyed, isInvulnerable;
    [Tooltip("Optional exposed collider. When unset, damage follows the visible animated sprite shapes.")]
    public Collider2D exposedRegion;
    [HideInInspector] public SpiderBossController owner;
    public float MaxHealth { get; private set; }
    SpriteRenderer[] sprites;
    Collider2D[] hitColliders;
    bool[] initialColliderStates;
    Color[] colors;
    bool[] initiallyVisible;
    float flashUntil, destructionVisibleUntil, lossStarted, detachAt;
    bool detached;
    int lossPoseFrame=-1;
    PastLegDetails[] pastDetails;
    static readonly List<Vector2> outline = new List<Vector2>();

    void Awake() {
        MaxHealth = Mathf.Max(1, health);
        hitColliders=GetComponentsInChildren<Collider2D>(true);initialColliderStates=new bool[hitColliders.Length];
        for(int i=0;i<hitColliders.Length;i++)initialColliderStates[i]=hitColliders[i].enabled;
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colors = new Color[sprites.Length];
        initiallyVisible = new bool[sprites.Length];
        pastDetails=new PastLegDetails[sprites.Length];
        for(int i=0;i<sprites.Length;i++) {
            var details=new GameObject("Past exposed leg structure");details.transform.SetParent(sprites[i].transform,false);
            pastDetails[i]=details.AddComponent<PastLegDetails>();pastDetails[i].source=sprites[i];details.SetActive(false);
        }
        for(int i=0;i<sprites.Length;i++) { colors[i]=sprites[i].color; initiallyVisible[i]=sprites[i].enabled && sprites[i].gameObject.activeSelf; }
    }
    public bool TryGetVisibleBounds(out Bounds bounds) {
        bounds = new Bounds(); bool found=false;
        if(isDestroyed || sprites==null) return false;
        foreach(var sr in sprites) if(sr && sr.enabled && sr.gameObject.activeInHierarchy && sr.sprite && sr.color.a>.01f) {
            if(!found) bounds=sr.bounds; else bounds.Encapsulate(sr.bounds);
            found=true;
        }
        return found;
    }
    // Query sprite physics outlines in world space, including animated rotation, scale and sprite flips.
    // The exposed collider, if assigned, narrows this region rather than making invisible art hittable.
    public bool TryGetHitPoint(Vector2 origin, out Vector2 point) {
        point=origin;
        return !isDestroyed && gameObject.activeInHierarchy && QueryHitPoint(sprites,exposedRegion,origin,out point);
    }
    public static bool TryGetSpriteHitPoint(SpriteRenderer sprite,Vector2 origin,out Vector2 point) {
        return QueryHitPoint(new[] { sprite },null,origin,out point);
    }
    static bool QueryHitPoint(SpriteRenderer[] sprites,Collider2D exposedRegion,Vector2 origin,out Vector2 point) {
        point=origin;
        if(sprites==null) return false;
        float best=float.PositiveInfinity; bool found=false;
        foreach(var sr in sprites) {
            if(!sr || !sr.enabled || !sr.gameObject.activeInHierarchy || !sr.sprite || sr.color.a<=.01f) continue;
            int shapes=sr.sprite.GetPhysicsShapeCount();
            for(int shape=0;shape<shapes;shape++) {
                outline.Clear(); sr.sprite.GetPhysicsShape(shape,outline);
                bool inside=false;
                for(int i=0,j=outline.Count-1;i<outline.Count;j=i++) {
                    Vector2 a=WorldPoint(sr,outline[j]), b=WorldPoint(sr,outline[i]);
                    if((a.y>origin.y)!=(b.y>origin.y) && origin.x<(b.x-a.x)*(origin.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                    Vector2 edge=b-a;
                    Vector2 nearest=a+edge*Mathf.Clamp01(Vector2.Dot(origin-a,edge)/Mathf.Max(.000001f,edge.sqrMagnitude));
                    Consider(nearest,origin,ref point,ref best,ref found,exposedRegion);
                }
                if(inside) Consider(origin,origin,ref point,ref best,ref found,exposedRegion);
            }
            // Sprites without physics outlines still have a visible mesh. Use its triangles.
            if(shapes==0) {
                Vector2[] vertices=sr.sprite.vertices; ushort[] triangles=sr.sprite.triangles;
                for(int i=0;i<triangles.Length;i+=3) {
                    Vector2 a=WorldPoint(sr,vertices[triangles[i]]), b=WorldPoint(sr,vertices[triangles[i+1]]), c=WorldPoint(sr,vertices[triangles[i+2]]);
                    float ab=Cross(b-a,origin-a), bc=Cross(c-b,origin-b), ca=Cross(a-c,origin-c);
                    if((ab>=0 && bc>=0 && ca>=0)||(ab<=0 && bc<=0 && ca<=0)) Consider(origin,origin,ref point,ref best,ref found,exposedRegion);
                    Edge(a,b,origin,ref point,ref best,ref found,exposedRegion); Edge(b,c,origin,ref point,ref best,ref found,exposedRegion); Edge(c,a,origin,ref point,ref best,ref found,exposedRegion);
                }
            }
        }
        return found;
    }
    static float Cross(Vector2 a,Vector2 b) { return a.x*b.y-a.y*b.x; }
    static void Edge(Vector2 a,Vector2 b,Vector2 origin,ref Vector2 point,ref float best,ref bool found,Collider2D exposedRegion) {
        Vector2 d=b-a; Consider(a+d*Mathf.Clamp01(Vector2.Dot(origin-a,d)/Mathf.Max(.000001f,d.sqrMagnitude)),origin,ref point,ref best,ref found,exposedRegion);
    }
    static void Consider(Vector2 candidate,Vector2 origin,ref Vector2 point,ref float best,ref bool found,Collider2D exposedRegion) {
        if(exposedRegion && (!exposedRegion.enabled || !exposedRegion.gameObject.activeInHierarchy || !exposedRegion.OverlapPoint(candidate))) return;
        float d=(candidate-origin).sqrMagnitude; if(d<best) { best=d; point=candidate; found=true; }
    }
    static Vector2 WorldPoint(SpriteRenderer sr,Vector2 point) {
        if(sr.flipX) point.x=-point.x; if(sr.flipY) point.y=-point.y;
        return sr.transform.TransformPoint(point);
    }
    public Vector3 HitPosition(Vector3 origin) { Vector2 point; return TryGetHitPoint(origin,out point)?(Vector3)point:transform.position; }
    public void TakeDamage(float damage) {
        if(isDestroyed || damage<=0 || !owner || !owner.player) return;
        Vector2 point;
        if(!TryGetHitPoint(owner.ClawOrigin,out point) || !owner.CanClawHit(point)) return;
        bool armored=owner.CurrentPhase==SpiderBossController.Phase.Armored;
        bool blocked=(!armored && isInvulnerable) || (!armored && owner.CurrentPhase!=SpiderBossController.Phase.ExposedLegs);
        owner.Feedback(point);
        flashUntil=Time.unscaledTime+.15f;
        if(blocked) return;
        // Present armor takes chip damage; the authored arm-loss sequence remains in the past.
        health=Mathf.Max(armored?1:0,health-damage*(armored?owner.armoredDamageMultiplier:1));
        if(health<=0) DestroyPart();
    }
    public void DestroyPart() {
        if(isDestroyed || owner && (!owner.CanLoseArm || !owner.IsTargetArm(this))) return;
        isDestroyed=true; health=0; lossStarted=Time.time;
        destructionVisibleUntil=owner?owner.OnLegDestroyed(this):Time.time;
        // leg1's authored fall reaches its lowest keyed pose at 2.55s, then resets for reuse.
        // Keep its recoil/swing/fall intact and hide before that reset. The left arm only recoils.
        detachAt=lossStarted+(owner && this==owner.rightArm?Mathf.Min(2.55f,destructionVisibleUntil-lossStarted):.35f);
        foreach(var collider in GetComponentsInChildren<Collider2D>(true)) collider.enabled=false;
        if(Time.time>=destructionVisibleUntil) HideDestroyed();
    }
    public void PresentLoss() {
        if(!isDestroyed || !owner || detached || lossPoseFrame==Time.frameCount) return;
        lossPoseFrame=Time.frameCount;
        if(Time.time>=detachAt) { detached=true; HideDestroyed(); }
    }
    public void HideDestroyed() {
        foreach(var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled=false;
        foreach(var c in GetComponentsInChildren<Collider2D>(true)) c.enabled=false;
    }
    public void RefreshPresentation() {
        if(pastDetails!=null)foreach(var detail in pastDetails)if(detail)detail.gameObject.SetActive(owner && owner.isInPast && !isDestroyed);
        if(isDestroyed) { if(detached || Time.time>=destructionVisibleUntil || owner && !owner.isInPast) HideDestroyed(); return; }
        if(sprites==null) return;
        for(int i=0;i<sprites.Length;i++) if(sprites[i]) {
            // Health owns limb visibility; legacy count-based animation curves cannot remove a different leg.
            if(initiallyVisible[i]) { sprites[i].gameObject.SetActive(true); sprites[i].enabled=true; }
            bool past=owner && owner.isInPast;
            Color tint=Time.unscaledTime<flashUntil?Color.white:past?colors[i]*new Color(.91f,.77f,.61f):owner && owner.CurrentPhase==SpiderBossController.Phase.Mutated?new Color(.55f,.8f,.35f):colors[i];
            tint.a=colors[i].a; sprites[i].color=tint;
        }
    }
    public void ResetForEncounter() {
        isDestroyed=detached=false;health=MaxHealth;isInvulnerable=true;
        flashUntil=destructionVisibleUntil=lossStarted=detachAt=0;lossPoseFrame=-1;
        if(hitColliders!=null)for(int i=0;i<hitColliders.Length;i++)if(hitColliders[i])hitColliders[i].enabled=initialColliderStates[i];
        RefreshPresentation();
    }
    public void SetInvulnerable(bool state) { isInvulnerable=state; }
}
