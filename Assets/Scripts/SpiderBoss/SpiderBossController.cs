using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

[System.Serializable] public class IslandAttackOption { public string attackAnimationName; public AttackType attackType; public Transform stabPoint; }
[System.Serializable] public class IslandSettings {
    public float sinkAmount = 2, sinkSpeed = 1, respawnDelay = 20;
    public GameObject sinkParticles, stabParticles, respawnParticles;
    public float sinkParticalsExpiration = 2, stabParticalsExpiration = 10, respawnParticalsExpiration = 2;
}
public enum IslandTimeline { Both, Present, Past }
public enum IslandPhase { Any, Phase1, Phase2, Phase3 }
public enum AttackType { Sweep, Stab, Reveal }
[System.Serializable] public class IslandAttackData {
    public string islandName; public Collider2D islandCollider; public Transform islandTransform;
    public List<IslandAttackOption> possibleAttacks = new List<IslandAttackOption>();
    public IslandSettings settings = new IslandSettings(); public IslandTimeline timeline; public IslandPhase phase;
    [HideInInspector] public bool isDestroyed; [HideInInspector] public Vector3 startPos;
    [System.NonSerialized] public int impacts;
    [System.NonSerialized] public bool floating;
    [System.NonSerialized] public Rigidbody2D body;
    [System.NonSerialized] public Quaternion startRotation;
}

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class SpiderBossController : MonoBehaviour {
    public Animator animator;
    public SpiderBossRig2D rig;
    public SpiderBossEscapePassage escapePassage;
    public float biteDamage=30, biteWindup=1.25f, biteReach=7;
    public float biteLoweringDuration=1.25f, biteHoldDuration=.45f, biteRecoveryDuration=1.5f;
    public GameObject PastEnvironment, PresentEnvironment;
    public Transform player; public TimelineSync timelineSync; public BoxCollider2D sweepCollider; public Transform stabPoint;
    public List<BossPart> legs = new List<BossPart>();
    public BossPart leftArm, rightArm;
    public List<IslandAttackData> islandAttacks = new List<IslandAttackData>();
    public float sweepDamage = 25, stabDamage = 35, stabRadius = 1.5f;
    public LayerMask playerLayer;
    public float decisionInterval = 2;
    public bool isInPast, isAttacking, isVulnerable;
    public const int RequiredArmLosses = 2;
    [Header("Encounter")]
    public float activationDistance = 22, coreMaxHealth = 210, attackDamage = 15, attackReach = 2.5f;
    [Range(.01f,1f)] public float armoredDamageMultiplier=.15f;
    public float telegraphDuration = 1, vulnerabilityDuration = 2.2f, acidLifetime = 7;
    public Transform corePoint, escapePoint;
    public GameObject escapeWall, openedPassage;
    public AudioClip outroMusic;
    [Header("Timeline release")]
    public ToggleObjectOnKey timelineTraversal;
    public Collider2D[] timelineLocks = new Collider2D[0];
    public GameObject teleportBarrier;
    public Collider2D teleportBarrierCollider;
    public float barrierFadeDuration = 1.25f;
    ParticleSystem[] barrierParticles;
    ParticleSystem.Particle[][] barrierParticleBuffers;
    UnityEngine.Rendering.Volume[] barrierVolumes;
    float[] barrierVolumeWeights;
    float barrierAlpha, barrierPreviousAlpha;
    bool barrierWanted, firstDefeatReleased;
    Vector3 coreRestLocal, coreExposureTarget;
    float coreExposureStarted;
    bool coreLowered;
    public UnityEvent onEncounterWon = new UnityEvent();
    public UnityEvent onWallCracked = new UnityEvent();
    public enum Phase { Armored, ExposedLegs, Shell, Mutated, Escaping, Complete }
    public Phase CurrentPhase { get; private set; }
    public float CoreHealth { get; private set; }
    TimeTravelCameraSwitcher timeManager;
    BossAudioController audioFX;
    readonly List<GameObject> transient = new List<GameObject>();
    readonly HashSet<IslandAttackData> sinking = new HashSet<IslandAttackData>();
    readonly Dictionary<IslandAttackData,float> islandSinkTimes=new Dictionary<IslandAttackData,float>();
    readonly List<IslandAttackData> restoredIslands=new List<IslandAttackData>();
    float nextAttack, nextClaw, playerHealth = 100, hurtUntil;
    float wallCampStarted=-1,wallDischargeAfter;
    bool wallDischargeReady;
    bool engaged, shellReached, finalReached, impactDone;
    bool acidPoolFilled, reentryRequested, observedPast;
    AcidVolume2D[] acidVolumes=new AcidVolume2D[0];
    public bool IsPouringAcid {get;private set;}
    public bool IsHeadSlam {get;private set;}
    float reentryStarted, reentryUntil;
    [Header("Arena placement")]
    public Transform combatAnchor;
    public Vector2 arenaOffset=new Vector2(3,0);
    Vector3 arenaPosition;
    Quaternion arenaRotation;
    [Header("Timeline entrance")]
    public float reentryDuration=4.85f;
    AnimationClip entranceClip;
    float ReentryLength => Mathf.Max(3.5f,reentryDuration);
    public bool IsReentering => reentryUntil>Time.time;
    int attacks, destroyedLegs;
    Vector3 lockedTarget;
    IslandAttackData targetIsland;
    GameObject warning;
    BossPart attackingArm;
    float animationBusyUntil, armLossUntil;
    public bool IsPlayingArmLoss => Time.time<armLossUntil;
    bool phasePoseQueued;
    bool cutsceneActive, combatWasPaused;
    bool initialBarrierVisible;
    bool feedbackRunning;
    bool encounterMusicStarted;
    float attackStarted;
    PistonMover acidPiston;
    Vector3 acidPistonStart;
    AnimatorOverrideController combatOverrides;
    readonly List<AnimationClip> safeClips=new List<AnimationClip>();
    readonly Dictionary<Transform,Vector3> poolPositions=new Dictionary<Transform,Vector3>();
    readonly Dictionary<Transform,bool> poolActive=new Dictionary<Transform,bool>();
    readonly Dictionary<Collider2D,bool> initialTimelineLocks=new Dictionary<Collider2D,bool>();
    SpriteRenderer[] renderers;
    Color[] originalColors;
    
    static Material effectMaterial;

    void Awake() {
        BossMusicPlayback.FadeOnEntry(1.2f);
        if (!animator) animator = GetComponent<Animator>();
        timeManager = FindObjectOfType<TimeTravelCameraSwitcher>();
        audioFX = GetComponent<BossAudioController>();
        if(!timelineTraversal) foreach(var traversal in FindObjectsOfType<ToggleObjectOnKey>(true))
            if(traversal.targetObjects.Contains(PastEnvironment) && traversal.targetObjects.Contains(PresentEnvironment)) { timelineTraversal=traversal; break; }
        var body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity=Vector2.zero; body.angularVelocity=0;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for(int i=0;i<renderers.Length;i++) originalColors[i] = renderers[i].color;
        CoreHealth = coreMaxHealth;
        foreach(var blocker in timelineLocks)if(blocker)initialTimelineLocks[blocker]=blocker.enabled;
        foreach(var goo in FindObjectsOfType<GooFluidController>(true)) {
            poolPositions[goo.transform]=goo.transform.position;poolActive[goo.transform]=goo.gameObject.activeSelf;
            if(goo.transform.parent && !poolPositions.ContainsKey(goo.transform.parent)){poolPositions[goo.transform.parent]=goo.transform.parent.position;}
        }
        var authoredEvents=GetComponent<runEvent>();
        if(authoredEvents && authoredEvents.events.Count>0)for(int i=0;i<authoredEvents.events[0].GetPersistentEventCount();i++) {
            var piston=authoredEvents.events[0].GetPersistentTarget(i) as PistonMover;
            if(piston){acidPiston=piston;acidPistonStart=piston.transform.position;break;}
        }
        if(acidPiston){
            var volumes=new List<AcidVolume2D>();
            foreach(var effector in acidPiston.GetComponentsInChildren<BuoyancyEffector2D>(true)){
                var volume=effector.GetComponent<AcidVolume2D>();if(!volume)volume=effector.gameObject.AddComponent<AcidVolume2D>();volume.Configure();volumes.Add(volume);
            }
            acidVolumes=volumes.ToArray();
        }
        InstallSafeCombatClips();
        entranceClip=FindClip("reveal");
        if (!corePoint) {
            var head = transform.Find("head");
            corePoint = head ? head : transform;
        }
        coreRestLocal=corePoint.localPosition;
        observedPast=PastEnvironment?PastEnvironment.activeSelf:isInPast;
        InitializeBarrier();initialBarrierVisible=barrierWanted;
        if(timelineTraversal) timelineTraversal.onTraverseSuccess.AddListener(OnTimelineTraversed);
        if (sweepCollider) sweepCollider.enabled = false;
        foreach(var leg in legs) if(leg) leg.owner = this;
        // The authored front-arm rig: leg2 is screen-left, leg1 is screen-right.
        if(!leftArm) { var arm=transform.Find("leg2"); if(arm) leftArm=arm.GetComponent<BossPart>(); }
        if(!rightArm) { var arm=transform.Find("leg1"); if(arm) rightArm=arm.GetComponent<BossPart>(); }
        foreach(var island in islandAttacks) InitializeIsland(island);
        if(!rig) rig=GetComponent<SpiderBossRig2D>();
        if(!rig) rig=gameObject.AddComponent<SpiderBossRig2D>();
        rig.Initialize(this);
        Vector3 authored=transform.parent?transform.parent.TransformPoint(rig.authoredCombatOrigin):rig.authoredCombatOrigin;
        arenaPosition=combatAnchor?combatAnchor.position:authored+(Vector3)arenaOffset;
        arenaRotation=Quaternion.identity;
        if(!escapePassage) escapePassage=GetComponent<SpiderBossEscapePassage>();
        if(!escapePassage) escapePassage=gameObject.AddComponent<SpiderBossEscapePassage>();
        escapePassage.Initialize(this);
    }
    void LateUpdate() {
        if(CurrentPhase >= Phase.Escaping) { if(rig) rig.Evaluate(); return; }
        if(PastEnvironment || PresentEnvironment) {
            // The boss room shares one player and geometry; art roots are authoritative.
            isInPast = PastEnvironment ? PastEnvironment.activeSelf : !PresentEnvironment.activeSelf;
            if(timelineSync) timelineSync.OnTimelineSwap(isInPast);
        } else if(timeManager) {
            isInPast = !timeManager.getIsInPresent();
            var cat = isInPast ? timeManager.pastPlayer : timeManager.presentPlayer;
            if(cat) player = cat.transform;
        } else if(timelineSync) isInPast = timelineSync.IsPlayerInPast;
        if(isInPast!=observedPast) {
            observedPast=isInPast;
            // Also cover timeline changes initiated outside the traversal button.
            if(engaged && !reentryRequested && !IsReentering) OnTimelineTraversed();
        }
        if(!player || !player.gameObject.activeInHierarchy) {
            var cat = FindObjectOfType<CatPlayerController>(); if(cat) player = cat.transform;
        }
        if(!player) return;
        if(!ledgeCaptured) { respawnLedge=player.position; ledgeCaptured=true; }
        if(deathReloading){if(rig)rig.Evaluate();return;}
        if(IsReentering) {
            SampleEntrance(Mathf.Clamp01((Time.time-reentryStarted)/ReentryLength));
            rig.Evaluate();
            foreach(var leg in legs) if(leg) leg.RefreshPresentation();
            return;
        }
        if(reentryUntil>0) {
            reentryUntil=0; rig.ClearOverrides();
            rig.SetCombatAnchor(arenaPosition,arenaRotation);
            nextAttack=Time.time+1.5f; hurtUntil=Time.time+1;
            phasePoseQueued=true;
        }
        if(animator && IsIntroState(animator.GetCurrentAnimatorStateInfo(0)))EnsureEncounterMusic();
        if(IsCutsceneActive) {
            if(!combatWasPaused) { CancelAttack(); combatWasPaused=true; }
            nextAttack=Time.time+2;
            return;
        }
        if(combatWasPaused) { combatWasPaused=false; nextAttack=Time.time+2; hurtUntil=Mathf.Max(hurtUntil,Time.time+1); }
        if(!engaged) {
            if(Vector2.Distance(player.position, transform.position)>activationDistance) return;
            EnsureEncounterMusic();engaged = true; nextAttack = Time.time + 2;
            // Arena placement is explicit; never inherit a displaced recoil/one-arm pose.
            rig.SetCombatAnchor(arenaPosition,arenaRotation);
        }
        // Combat owns impact/pool timing. Old cinematic events must never freeze
        // or hide scene objects after the authored entrance has finished.
        if(animator)animator.fireEvents=false;
        destroyedLegs = 0;
        foreach(var leg in legs) if(leg && leg.isDestroyed) destroyedLegs++;
        if(destroyedLegs>=RequiredArmLosses) shellReached = true;
        if(shellReached && !IsPlayingArmLoss) ReleaseTimeline();
        if(shellReached && !isInPast && !finalReached) { finalReached=true; SetBarrier(true); }
        Phase desired = finalReached && !isInPast ? Phase.Mutated : shellReached ? Phase.Shell : isInPast ? Phase.ExposedLegs : Phase.Armored;
        bool changed=desired!=CurrentPhase;
        if(changed) {
            CancelAttack();
            CurrentPhase=desired; nextAttack=Time.time+1.5f;
            if(desired==Phase.Mutated) { attacks=2;RestoreIslands(); }
            if(desired!=Phase.Shell) { animationBusyUntil=0; foreach(var shake in FindObjectsOfType<CameraShake2D>()) shake.StopShake(); }
            phasePoseQueued=true;
        }
        SetBool("isPresent", !isInPast); SetBool("Phase1", !shellReached || IsPlayingArmLoss); SetBool("Phase2", shellReached && !IsPlayingArmLoss);
        SetInt("numLegsDefeated", shellReached && !IsPlayingArmLoss?2:Mathf.Min(1,destroyedLegs));
        foreach(var leg in legs) if(leg) leg.SetInvulnerable(!IsTargetArm(leg) || CurrentPhase!=Phase.ExposedLegs || Time.time<animationBusyUntil);
        if(phasePoseQueued && Time.time>=animationBusyUntil) {
            phasePoseQueued=false;
            foreach(var shake in FindObjectsOfType<CameraShake2D>()) shake.StopShake();
            if(CurrentPhase==Phase.Shell) {
                PlayState("Base Layer.Phase 1 Past.curl into ball");
                animationBusyUntil=Time.time+ClipDuration("curl into ball",1);
                if(audioFX) audioFX.PlayCurlSound();
            } else PlayIdle();
        }
        if(reentryRequested) {
            reentryRequested=false; CancelAttack(); PlayIdle();
            reentryStarted=Time.time; reentryUntil=Time.time+ReentryLength;
            animationBusyUntil=reentryUntil; nextAttack=reentryUntil+1.5f;
            SampleEntrance(0);
            if(audioFX) audioFX.PlayScreechSound();
            rig.Evaluate(); return;
        }
        if(CurrentPhase==Phase.Shell && Time.time>=animationBusyUntil && !animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Phase 2 Past.ball_idle"))
            PlayState("Base Layer.Phase 2 Past.ball_idle");
        for(int i=0;i<renderers.Length;i++) if(renderers[i]) renderers[i].color = CurrentPhase==Phase.Mutated ? new Color(.55f,.8f,.35f,originalColors[i].a) : originalColors[i];
        foreach(var leg in legs) if(leg) leg.RefreshPresentation();
        if(CurrentPhase==Phase.Mutated && isVulnerable) {
            rig.SetHeadPose(coreExposureTarget,corePoint.rotation,0);
            coreLowered=true;
        } else if(coreLowered) { rig.ReleaseHead(); coreLowered=false; }
        UpdateWallCamping();
        if(!isAttacking && CurrentPhase!=Phase.Shell && Time.time>=nextAttack && Time.time>=animationBusyUntil) {
            // Choose an attacking limb from the displayed arena pose, after root relocation.
            if(rig)rig.Evaluate();StartCoroutine(RunAttack());
        }
        foreach(var leg in legs) if(leg) leg.RefreshPresentation();
        // The authored curl/ball clips already contain the fold, drop and settling motion.
        if(rig) rig.Evaluate();
        if(activeStrikeWindow && Time.time>strikeWindowEnds)activeStrikeWindow=false;
        if(activeStrikeWindow && isAttacking && !attackHasHit) {
            if(rig && rig.HasStrike)attackHasHit=DamageArea(rig.StrikeTip,new Vector2(.8f,.8f),stabDamage*(isInPast?.65f:1));
            var body=player.GetComponent<Collider2D>();
            if(body && !attackHasHit)foreach(var arm in legs) if(arm && arm==attackingArm && !arm.isDestroyed) {
                Vector2 contact;if(arm.TryGetHitPoint(body.bounds.center,out contact) &&
                    DamageArea(contact,new Vector2(.3f,.3f),stabDamage*(isInPast?.65f:1))) {attackHasHit=true;break;}
            }
        }
        foreach(var leg in legs) if(leg) leg.PresentLoss();
        HandleClaw();
    }
    public Vector2 ClawOrigin {
        get {
            if(!player) return transform.position;
            var cat=player.GetComponent<CatPlayerController>();
            var body=player.GetComponent<Collider2D>();
            Vector2 center=body? (Vector2)body.bounds.center : (Vector2)player.position;
            return center+Vector2.right*(cat && !cat.FacingRight?-1:1)*attackReach*.5f;
        }
    }
    public bool CanClawHit(Vector2 point) {
        if(!player || IsCombatSuppressed || Time.timeScale<=0 || CurrentPhase==Phase.Shell || CurrentPhase>=Phase.Escaping) return false;
        if(Vector2.Distance(ClawOrigin,point)>attackReach*.5f) return false;
        var cat=player.GetComponent<CatPlayerController>();
        if(cat) foreach(var hit in Physics2D.LinecastAll(ClawOrigin,point,cat.groundLayer | cat.wallLayer)) {
            if(hit.collider && !hit.collider.isTrigger && !hit.transform.IsChildOf(player) && !hit.transform.IsChildOf(transform)) return false;
        }
        return true;
    }
    void HandleClaw() {
        var cat=player.GetComponent<CatPlayerController>();
        if(IsCombatSuppressed || !cat || Time.time<nextClaw || cat.LastAttackFrame!=Time.frameCount || Time.timeScale<=0) return;
        nextClaw=Time.time+.4f;
        var slash=SpiderCombatFX.Slash(ClawOrigin,attackReach,new Color(1,1,1,.8f));
        slash.transform.rotation=Quaternion.Euler(0,0,cat.FacingRight?25:-25); Destroy(slash,.24f);
        BossPart closest=null; float distance=float.PositiveInfinity;
        foreach(var leg in legs) if(leg && !leg.isDestroyed && IsTargetArm(leg)) {
            Vector2 point;
            if(!leg.TryGetHitPoint(ClawOrigin,out point) || !CanClawHit(point)) continue;
            float d=Vector2.Distance(ClawOrigin,point);
            if(d<distance) { distance=d; closest=leg; }
        }
        if(CurrentPhase==Phase.Mutated && TryHitCore()) return;
        if(closest) closest.TakeDamage(attackDamage);
        else if(CurrentPhase!=Phase.Mutated)TryHitCore();
    }
    bool TryHitCore() {
        var sr=corePoint.GetComponent<SpriteRenderer>();
        if(!sr || !sr.enabled || !sr.gameObject.activeInHierarchy) return false;
        Vector2 point;
        if(!BossPart.TryGetSpriteHitPoint(sr,ClawOrigin,out point) || !CanClawHit(point)) return false;
        TakeDamage(attackDamage); return true;
    }
    public void TakeDamage(float damage) {
        if(!engaged || damage<=0 || CurrentPhase==Phase.Shell || CurrentPhase>=Phase.Escaping || !player) return;
        var coreSprite=corePoint.GetComponent<SpriteRenderer>();
        Vector2 coreHit;
        if(!BossPart.TryGetSpriteHitPoint(coreSprite,ClawOrigin,out coreHit) || !CanClawHit(coreHit)) return;
        Feedback(corePoint.position);
        bool finalPhase=CurrentPhase==Phase.Mutated;
        CoreHealth=Mathf.Max(finalPhase?0:1,CoreHealth-damage*(finalPhase?1:armoredDamageMultiplier));
        if(CoreHealth<=0) { CancelAttack(); CurrentPhase=Phase.Escaping; StartCoroutine(Escape()); }
    }
    public enum MutatedMove { Acid, HeadSlam, HeadSweep, Bite, Laser }
    int mutatedMoveIndex;
    public static MutatedMove SelectMutatedMove(int index,bool poolFilled) {
        if(!poolFilled)return MutatedMove.Acid;
        switch(index%7){case 1:case 5:return MutatedMove.Laser;case 0:case 3:return MutatedMove.HeadSlam;case 4:return MutatedMove.HeadSweep;default:return MutatedMove.Bite;}
    }
    bool resolvingStab,activeStrikeWindow,attackHasHit;
    float strikeWindowEnds;
    IEnumerator RunAttack() {
        try{yield return Attack();}
        finally {
            IsPouringAcid=false;if(IsHeadSlam && rig)rig.ReleaseBodyPose();IsHeadSlam=false;
            if(isAttacking){isAttacking=false;isVulnerable=false;activeStrikeWindow=false;nextAttack=Time.time+1.5f;}
            if(rig){rig.ReleaseHead();rig.CancelStab();}
            if(warning)Destroy(warning);warning=null;
        }
    }
    IEnumerator Attack() {
        if(IsCombatSuppressed) yield break;
        attackStarted=Time.time;isAttacking=true; isVulnerable=false; impactDone=false;attackHasHit=false;activeStrikeWindow=false;
        attacks++;
        if(wallDischargeReady){
            yield return WallDischarge();isAttacking=false;nextAttack=Time.time+1.3f;PlayIdle();yield break;
        }
        targetIsland=ClosestIsland();
        var targetBody=player.GetComponent<Collider2D>();
        lockedTarget=targetBody?targetBody.bounds.center:player.position;
        if(targetIsland!=null && targetIsland.islandCollider) {
            var bounds=targetIsland.islandCollider.bounds;
            lockedTarget.x=Mathf.Clamp(lockedTarget.x,bounds.min.x+.1f,bounds.max.x-.1f);
            lockedTarget.y=bounds.max.y+.08f;
        }
        bool mutated=CurrentPhase==Phase.Mutated;
        var move=SelectMutatedMove(mutatedMoveIndex,acidPoolFilled);
        if(mutated && acidPoolFilled)mutatedMoveIndex++;
        bool acid=mutated && move==MutatedMove.Acid;IsPouringAcid=acid;
        bool laser=mutated && move==MutatedMove.Laser;
        bool bite=mutated?move==MutatedMove.Bite:!acid && attacks%4==0;
        if((mutated && !acid && !laser) || bite) {
            if(mutated && move==MutatedMove.HeadSlam)yield return DestroyIslandAttack();
            else yield return BiteAttack(mutated && move==MutatedMove.HeadSweep,false);
            if(IsCombatSuppressed) yield break;
            if(CurrentPhase==Phase.Mutated) yield return ExposeCore();
            isAttacking=false; nextAttack=Time.time+Mathf.Max(1,decisionInterval); PlayIdle();
            yield break;
        }
        bool sweep=!mutated && !acid && attacks%3==1 && (!rightArm || !rightArm.isDestroyed) &&
            !(CurrentPhase==Phase.ExposedLegs && leftArm && leftArm.isDestroyed);
        warning=SpiderCombatFX.Telegraph(lockedTarget,sweep?7:acid?4:stabRadius*2,acid?new Color(.5f,.85f,.2f):new Color(1,.4f,.15f,.75f));
        // The supplied one-arm stab starts/ends in the left-loss clip's 31.356-degree stance.
        // If leg1 is already gone, keep the right-loss idle and let IK drive the surviving arm.
        bool survivingLeftStab=CurrentPhase==Phase.ExposedLegs && rightArm && rightArm.isDestroyed;
        string attackState=survivingLeftStab?"Base Layer.past_idle_no_arm_2":acid?"Base Layer.Phase 2 Present.barf acid":CurrentPhase==Phase.ExposedLegs && leftArm && leftArm.isDestroyed && !sweep?"Base Layer.past_no_arm_1_stab":
            (isInPast?"Base Layer.Phase 1 Past.All Arms.":"Base Layer.Phase 2 Present.")+(sweep?"island 3 sweep attack":targetIsland!=null && targetIsland.islandName.Contains("3")?"island 3 stab attack":"Island 1 Stab");
        if(laser)PlayIdle();else PlayState(attackState);
        float windup=Mathf.Max(.7f,telegraphDuration);
        float recovery=laser?.65f:Mathf.Min(acid?2.4f:1.6f,SyncAttackAnimation(attackState,windup));
        // Retain the authored body/windup, but aim the live arm at the advertised contact.
        bool ranged=laser;
        if(!acid && !laser && !sweep && rig) {
            rig.BeginStab(lockedTarget,windup,survivingLeftStab?leftArm:null);
            // Don't harmlessly aim at empty air when a platform lies outside arm reach.
            // Use a visibly telegraphed energy lance instead; it cannot damage islands.
            ranged=!rig.HasStrike || Vector2.Distance(lockedTarget,rig.StrikeTarget)>.65f;
            if(ranged) {
                rig.CancelStab();
                if(warning) { var marker=warning.GetComponent<SpiderCombatEffect>();if(marker)marker.SetTint(new Color(.15f,.85f,1,.7f)); }
            }
        }
        if(laser && warning){var marker=warning.GetComponent<SpiderCombatEffect>();if(marker)marker.SetTint(new Color(.15f,.85f,1,.7f));}
        SpiderIKLimb chargedLimb=rig?rig.StrikingLimb:null;
        if(sweep && rig)foreach(var limb in rig.limbs)if(limb.part==rightArm){chargedLimb=limb;break;}
        attackingArm=chargedLimb!=null?chargedLimb.part:null;
        if(chargedLimb!=null && chargedLimb.tip) {
            var cue=SpiderCombatFX.Telegraph(chargedLimb.tip.position,1.2f,new Color(1,.65f,.18f));
            var attach=new GameObject("Attacking arm cue");attach.transform.position=chargedLimb.tip.position;
            cue.transform.SetParent(attach.transform,true);transient.Add(attach);
            StartCoroutine(FollowStrikeCue(attach,chargedLimb,windup));
        }
        yield return new WaitForSeconds(Mathf.Max(.1f,windup-.2f));
        activeStrikeWindow=!acid && !ranged;
        strikeWindowEnds=Time.time+.45f;
        yield return new WaitForSeconds(.2f);
        if(IsCombatSuppressed) yield break;
        if(warning) Destroy(warning); warning=null;
        if(acid) {
            FillAcidPool();
            // Acid fills the pool; only physical stabs damage islands.
        } else if(sweep) {
            if(audioFX) audioFX.PlaySweepSound();
            Vector3 contact=chargedLimb!=null && chargedLimb.tip?chargedLimb.tip.position:lockedTarget;
            var slash=SpiderCombatFX.Slash(contact,3,new Color(1,.8f,.35f));Destroy(slash,.25f);
            if(!attackHasHit)attackHasHit=DamageArea(contact,new Vector2(2.4f,1.1f),sweepDamage);
        } else if(ranged) {
            DrawLimbStrike(lockedTarget);
            DamageArea(lockedTarget,Vector2.one*stabRadius*2,stabDamage*(isInPast?.65f:1));
        } else {
            yield return new WaitForEndOfFrame();
            impactDone=false; resolvingStab=true; PerformStab(); resolvingStab=false;
        }
        if(CurrentPhase==Phase.Mutated) {
            if(recovery>0) yield return new WaitForSeconds(recovery);
            if(acid && !IsCombatSuppressed){IsPouringAcid=false;yield return DestroyIslandAttack();}
            yield return ExposeCore();
        } else yield return new WaitForSeconds(Mathf.Max(.6f,recovery));
        if(rig)rig.CancelStab();
        IsPouringAcid=false;isVulnerable=false; isAttacking=false; PlayIdle();
        nextAttack=Time.time+Mathf.Max(.8f,decisionInterval*(CoreHealth<coreMaxHealth*.3f?.7f:1));
    }
    void UpdateWallCamping(){
        if(CurrentPhase==Phase.Shell || IsCombatSuppressed){wallCampStarted=-1;wallDischargeReady=false;return;}
        float left=float.PositiveInfinity,right=float.NegativeInfinity;
        foreach(var island in islandAttacks)if(island.islandCollider && !island.isDestroyed && island.timeline!=IslandTimeline.Past){left=Mathf.Min(left,island.islandCollider.bounds.min.x);right=Mathf.Max(right,island.islandCollider.bounds.max.x);}
        var cat=player.GetComponent<CatPlayerController>();
        bool camping=(cat && cat.IsClingingToWall) || player.position.x<left+1.2f || player.position.x>right-1.2f;
        if(!camping){wallCampStarted=-1;wallDischargeReady=false;return;}
        if(wallCampStarted<0)wallCampStarted=Time.time;
        wallDischargeReady=Time.time-wallCampStarted>=2.8f && Time.time>=wallDischargeAfter;
    }
    IEnumerator WallDischarge(){
        wallDischargeReady=false;wallDischargeAfter=Time.time+9;wallCampStarted=-1;
        PlayIdle();var body=player.GetComponent<Collider2D>();Vector3 locked=body?body.bounds.center:player.position;
        float bottom=locked.y-4,top=Mathf.Max(locked.y+4,rig.MouthPosition.y+1);
        foreach(var island in islandAttacks)if(island.islandCollider && !island.isDestroyed)bottom=Mathf.Min(bottom,island.islandCollider.bounds.min.y-1);
        var charge=new GameObject("Wall purge warning arcs");transient.Add(charge);
        Vector3 from=new Vector3(locked.x,bottom,0),to=new Vector3(locked.x,top,0);
        CreateBeamLine(charge,from,to,.22f,new Color(1,.5f,.12f,.2f),32758);
        for(float t=0;t<1.65f;t+=Time.deltaTime){
            if(IsCombatSuppressed)yield break;
            if(Mathf.FloorToInt(t*8)!=Mathf.FloorToInt((t-Time.deltaTime)*8)){
                var spark=SpiderCombatFX.Impact(Vector3.Lerp(from,to,Mathf.PingPong(t*2,1)),new Color(1,.55f,.12f),.5f);Destroy(spark,.25f);
            }
            yield return null;
        }
        if(IsCombatSuppressed)yield break;
        if(charge)Destroy(charge);
        var bolt=new GameObject("Wall purge discharge");transient.Add(bolt);
        CreateBeamLine(bolt,from,to,1.15f,new Color(.15f,.7f,1,.25f),32758);
        CreateBeamLine(bolt,from,to,.3f,new Color(.25f,.9f,1,1),32759);
        CreateBeamLine(bolt,from,to,.07f,Color.white,32760);
        SpiderCombatFX.Impact(locked,new Color(.3f,.85f,1),1);
        DamageArea(new Vector2(locked.x,(bottom+top)*.5f),new Vector2(1.2f,top-bottom),stabDamage);
        if(audioFX)audioFX.PlayStabSound();yield return FadeBeam(bolt,.35f);
        yield return new WaitForSeconds(.4f);
    }
    IEnumerator ExposeCore() {
        isVulnerable=true; PlayIdle(); BeginCoreExposure();
        // The exposed body pose is the opening; no explicit vulnerability marker.
        yield return new WaitForSeconds(vulnerabilityDuration);
        isVulnerable=false;
    }
    IEnumerator DestroyIslandAttack(){
        PlayIdle();IsHeadSlam=true;
        // Settle the attached head before aiming. Target an island near the chassis,
        // rather than following a wall-camping player across the arena.
        rig.ReleaseHead();rig.ReleaseBodyPose();
        yield return new WaitForSeconds(.35f);
        if(IsCombatSuppressed){IsHeadSlam=false;yield break;}
        Vector3 start=rig.MouthPosition;Quaternion rotation=Quaternion.identity;
        IslandAttackData chosen=null;Vector3 impact=start;float best=float.PositiveInfinity;
        foreach(var island in islandAttacks){
            if(island==null || !island.islandCollider || !island.islandCollider.enabled || island.isDestroyed || island.timeline==IslandTimeline.Past || island.floating)continue;
            var bounds=island.islandCollider.bounds;
            float left=Mathf.Max(bounds.min.x+.12f,start.x-1.25f),right=Mathf.Min(bounds.max.x-.12f,start.x+1.25f);
            if(left>right)continue;
            Vector3 point=island.islandCollider.ClosestPoint(new Vector2(Mathf.Clamp(start.x,left,right),bounds.max.y+2));point.z=start.z;point.y+=.03f;
            if(point.y>=start.y || start.y-point.y>14)continue;
            float score=Mathf.Abs(point.x-start.x)+Mathf.Abs(point.x-player.position.x)*.05f;
            if(score<best){chosen=island;impact=point;best=score;}
        }
        if(chosen==null){IsHeadSlam=false;yield break;}
        targetIsland=chosen;
        warning=SpiderCombatFX.Telegraph(impact,Mathf.Clamp(chosen.islandCollider.bounds.size.x,2,4),new Color(1,.4f,.1f,.8f));
        // The windup lifts the entire attached head; supports retain their world anchors.
        Vector3 raised=start+Vector3.up*.45f;
        for(float t=0;t<1.25f;t+=Time.deltaTime){if(IsCombatSuppressed)yield break;rig.SetMouthPose(Vector3.Lerp(start,raised,Mathf.SmoothStep(0,1,t/1.25f)),rotation);rig.SetHeadPose(corePoint.position,rotation,t/1.25f);yield return null;}
        for(float t=0;t<1.1f;t+=Time.deltaTime){if(IsCombatSuppressed)yield break;rig.SetMouthPose(Vector3.Lerp(raised,impact,Mathf.SmoothStep(0,1,t/1.1f)),rotation);yield return null;}
        // Correct against the rendered mouth every frame, not an earlier animation/root offset.
        for(float t=0;t<1;t+=Time.deltaTime){if(IsCombatSuppressed)yield break;rig.SetMouthPose(impact,rotation);yield return null;if(Vector2.Distance(rig.MouthPosition,impact)<.12f)break;}
        if(warning)Destroy(warning);warning=null;
        if(Vector2.Distance(chosen.islandCollider.ClosestPoint(rig.MouthPosition),rig.MouthPosition)<.5f){
            if(audioFX)audioFX.PlayStabSound();SpiderCombatFX.Impact(impact,new Color(1,.65f,.25f),2);
            ShatterIsland(chosen);if(!attackHasHit)attackHasHit=DamageArea(rig.MouthPosition,new Vector2(2.4f,1.4f),biteDamage);
            yield return HeadImpactWave();
        }
        yield return new WaitForSeconds(.3f);
        Vector3 retreat=rig.MouthPosition;
        for(float t=0;t<1.5f;t+=Time.deltaTime){if(IsCombatSuppressed)yield break;rig.SetMouthPose(Vector3.Lerp(retreat,start,Mathf.SmoothStep(0,1,t/1.5f)),rotation);yield return null;}
        rig.ReleaseBodyPose();rig.ReleaseHead();IsHeadSlam=false;
    }
    void ShatterIsland(IslandAttackData island){
        if(CurrentPhase!=Phase.Mutated || island==null || island.isDestroyed || !island.islandCollider )return;
        island.impacts=2;MakeCrack(island);onWallCracked.Invoke();BeginIslandSink(island);
    }
    IEnumerator BiteAttack(bool headSweep=false,bool headSlam=false) {
        PlayIdle();IsHeadSlam=headSlam;
        Vector3 bodyRest=transform.position;Quaternion bodyRotation=transform.rotation;
        Vector3 rest=corePoint.position;
        Quaternion rotation=corePoint.rotation;
        Vector3 mouth=rig.MouthPosition;
        Vector3 target=player.position;
        var playerBody=player.GetComponent<Collider2D>(); if(playerBody) target=playerBody.bounds.center;
        if(headSlam) {
            float best=float.PositiveInfinity;IslandAttackData reachable=null;Vector3 surface=target;
            foreach(var island in islandAttacks)if(island!=null && island.islandCollider && island.islandCollider.enabled && !island.isDestroyed && island.timeline!=IslandTimeline.Past){
                var bounds=island.islandCollider.bounds;
                float left=Mathf.Max(bounds.min.x+.15f,mouth.x-6.5f+.1f),right=Mathf.Min(bounds.max.x-.15f,mouth.x+6.5f-.1f);
                if(left>right)continue;
                float x=Mathf.Clamp(player.position.x,left,right);
                Vector3 point=island.islandCollider.ClosestPoint(new Vector2(x,bounds.max.y+3));point.z=mouth.z;point.y+=.04f;
                if(mouth.y-point.y>12 || point.y>mouth.y+.15f)continue;
                float score=Mathf.Abs(point.x-player.position.x)+(island.floating?2:0)+island.impacts*.1f;
                if(score<best){best=score;reachable=island;surface=point;}
            }
            targetIsland=reachable;
            if(reachable!=null)target=surface;
            else {IsHeadSlam=false;yield break;}
        }
        Vector3 lowering=SpiderBossRig2D.ClampHeadOffset(new Vector3(target.x-mouth.x,Mathf.Min(-.8f,target.y-mouth.y),0),rig.ActiveDrop,rig.ActiveLean);
        if(headSlam)lowering=target-mouth;
        Vector3 lowered=rest+lowering;Vector3 bodyImpact=bodyRest+lowering;
        warning=SpiderCombatFX.Telegraph(mouth+lowering,headSweep?3.4f:headSlam?4:2,new Color(1,.4f,.1f,.65f));
        float windup=Mathf.Max(1.1f,biteWindup);
        for(float t=0;t<windup;t+=Time.deltaTime) {
            if(IsCombatSuppressed) yield break;
            if(headSlam)rig.SetBodyPose(bodyRest,bodyRotation);
            rig.SetHeadPose(rest,rotation,Mathf.SmoothStep(0,1,t/windup));
            yield return null;
        }
        if(headSlam){
            // Aim from the settled, attached head rather than an earlier animated pose.
            Vector3 impactDelta=target-rig.MouthPosition;
            bodyImpact=bodyRest+impactDelta;
        }
        float descent=Mathf.Max(CurrentPhase==Phase.Mutated?1.6f:1,biteLoweringDuration);
        for(float t=0;t<descent;t+=Time.deltaTime) {
            if(IsCombatSuppressed) yield break;
            if(headSlam)rig.SetBodyPose(Vector3.Lerp(bodyRest,bodyImpact,Mathf.SmoothStep(0,1,t/descent)),bodyRotation);
            rig.SetHeadPose(Vector3.Lerp(rest,lowered,Mathf.SmoothStep(0,1,t/descent)),rotation,1);
            yield return null;
        }
        rig.SetHeadPose(lowered,rotation,.25f);
        // Keep the loaded pose until the chassis/IK has actually settled onto the surface.
        if(headSlam)for(float t=0;t<1.2f;t+=Time.deltaTime){if(IsCombatSuppressed)yield break;rig.SetBodyPose(bodyImpact,bodyRotation);rig.SetHeadPose(lowered,rotation,.25f);if(Vector2.Distance(rig.MouthPosition,target)<.25f)break;yield return null;}
        // Briefly settle the braced body before the jaws close; damage follows the actual mouth.
        yield return new WaitForSeconds(.2f);
        if(IsCombatSuppressed) yield break;
        if(warning) Destroy(warning); warning=null;
        if(audioFX) audioFX.PlayStabSound();
        if(headSweep) {
            for(float t=0;t<.8f;t+=.06f) {
                if(IsCombatSuppressed)yield break;
                float side=Mathf.Sin(t/.8f*Mathf.PI*2)*rig.maximumBodyLean;
                rig.SetHeadPose(lowered+Vector3.right*side,rotation,.65f);
                SpiderCombatFX.Slash(rig.MouthPosition,1.6f,new Color(1,.65f,.3f));
                if(!attackHasHit)attackHasHit=DamageArea(rig.MouthPosition,new Vector2(2.4f,1.4f),sweepDamage);
                yield return new WaitForSeconds(.06f);
            }
        } else {
            if(!attackHasHit)attackHasHit=DamageArea(rig.MouthPosition,new Vector2(2,1.4f),biteDamage*(isInPast?.65f:1));
            if(headSlam) {
                if(targetIsland!=null && targetIsland.islandCollider && Vector2.Distance(targetIsland.islandCollider.ClosestPoint(rig.MouthPosition),rig.MouthPosition)<=.65f) {
                    SpiderCombatFX.Impact(rig.MouthPosition,new Color(1,.65f,.25f),2);
                    ShatterIsland(targetIsland);
                }
                yield return HeadImpactWave();
            }
        }
        yield return new WaitForSeconds(Mathf.Max(.3f,biteHoldDuration));
        float recovery=Mathf.Max(1.2f,biteRecoveryDuration);
        for(float t=0;t<recovery;t+=Time.deltaTime) {
            if(IsCombatSuppressed) yield break;
            if(headSlam)rig.SetBodyPose(Vector3.Lerp(bodyImpact,bodyRest,Mathf.SmoothStep(0,1,t/recovery)),bodyRotation);
            rig.SetHeadPose(Vector3.Lerp(lowered,rest,Mathf.SmoothStep(0,1,t/recovery)),rotation,.25f*(1-t/recovery));
            yield return null;
        }
        if(headSlam)rig.ReleaseBodyPose();IsHeadSlam=false;rig.ReleaseHead();
    }
    IEnumerator HeadImpactWave() {
        Vector3 center=rig.MouthPosition;
        // Project the impact onto the arena platform; these energy waves do not sink islands.
        if(targetIsland!=null && targetIsland.islandCollider)center.y=targetIsland.islandCollider.bounds.max.y+.2f;
        SpiderCombatFX.Impact(rig.MouthPosition,new Color(1,.6f,.25f),1.5f);
        float elapsed=0;
        while(elapsed<.8f) {
            if(IsCombatSuppressed)yield break;
            float distance=elapsed*7;
            foreach(float direction in new[]{-1f,1f}) {
                Vector3 point=center+Vector3.right*distance*direction;
                SpiderCombatFX.Slash(point,.9f,new Color(1,.55f,.2f));
                if(!attackHasHit)attackHasHit=DamageArea(point,new Vector2(1,1.1f),stabDamage);
            }
            elapsed+=.07f;yield return new WaitForSeconds(.07f);
        }
    }
    IEnumerator FollowStrikeCue(GameObject cue,SpiderIKLimb limb,float duration) {
        float end=Time.time+duration;
        var trace=cue.AddComponent<LineRenderer>();trace.sharedMaterial=EffectMaterial();trace.useWorldSpace=true;trace.positionCount=3;
        trace.startColor=new Color(1,.38f,.06f,.8f);trace.endColor=new Color(1,.85f,.3f,1);trace.widthMultiplier=.075f;trace.numCapVertices=4;SetEffectSorting(trace,32755);
        while(cue && limb.root && limb.tip && Time.time<end){cue.transform.position=limb.tip.position;
            trace.SetPosition(0,limb.root.position);trace.SetPosition(1,limb.joint?limb.joint.position:limb.root.position);trace.SetPosition(2,limb.tip.position);yield return null;}
        if(cue)Destroy(cue);
    }
    void EnsureEncounterMusic(){
        if(encounterMusicStarted || BossMusicPlayback.OutroPlaying)return;
        foreach(var music in FindObjectsOfType<MusicFaderController>())if(music.initialClip && music.initialClip.name=="LabBossIntro"){
            music.CrossfadeToNewMusic();encounterMusicStarted=true;return;
        }
    }
    void InstallSafeCombatClips() {
        if(!animator || !animator.runtimeAnimatorController)return;
        combatOverrides=new AnimatorOverrideController(animator.runtimeAnimatorController);
        var clips=new List<KeyValuePair<AnimationClip,AnimationClip>>();combatOverrides.GetOverrides(clips);
        for(int i=0;i<clips.Count;i++) {
            var original=clips[i].Value?clips[i].Value:clips[i].Key;
            string name=original.name.ToLowerInvariant();
            if(!name.Contains("stab") && !name.Contains("sweep") && name!="barf acid")continue;
            var safe=Instantiate(original);safe.name=original.name;
            var events=new List<AnimationEvent>();foreach(var e in original.events)if(e.functionName=="PerformStab" || e.functionName=="FillAcidPool")events.Add(e);
            safe.events=events.ToArray();safeClips.Add(safe);clips[i]=new KeyValuePair<AnimationClip,AnimationClip>(clips[i].Key,safe);
        }
        combatOverrides.ApplyOverrides(clips);animator.runtimeAnimatorController=combatOverrides;
    }
    public void ResetEncounterState() {
        // Called under the death fade; it must not stop the respawn coroutine or music.
        shellReached=finalReached=firstDefeatReleased=acidPoolFilled=reentryRequested=false;
        reentryStarted=reentryUntil=animationBusyUntil=armLossUntil=0;
        attacks=destroyedLegs=mutatedMoveIndex=0;CoreHealth=coreMaxHealth;
        isInPast=observedPast=false;CurrentPhase=Phase.Armored;
        if(PastEnvironment)PastEnvironment.SetActive(false);if(PresentEnvironment)PresentEnvironment.SetActive(true);
        if(timelineSync)timelineSync.OnTimelineSwap(false);
        foreach(var entry in initialTimelineLocks)if(entry.Key)entry.Key.enabled=entry.Value;
        if(acidPiston){acidPiston.DisablePiston();acidPiston.transform.position=acidPistonStart;}
        foreach(var entry in poolPositions)if(entry.Key && (!entry.Key.parent || !poolPositions.ContainsKey(entry.Key.parent)))entry.Key.position=entry.Value;
        foreach(var entry in poolPositions)if(entry.Key && entry.Key.parent && poolPositions.ContainsKey(entry.Key.parent))entry.Key.position=entry.Value;
        foreach(var entry in poolActive)if(entry.Key)entry.Key.gameObject.SetActive(entry.Value);
        foreach(var goo in FindObjectsOfType<GooFluidController>(true))goo.ResetDamageAndSurface();
        RestoreIslands();SetBarrier(initialBarrierVisible);
        if(rig)rig.ResetEncounterPose();
        animator.speed=1;SetBool("isPresent",true);SetBool("Phase1",true);SetBool("Phase2",false);SetInt("numLegsDefeated",0);
        int idle=Animator.StringToHash("Base Layer.Phase 1 Present.idle");
        if(animator.isInitialized && animator.HasState(0,idle)){animator.Play(idle,0,0);animator.Update(0);}
        foreach(var leg in legs)if(leg)leg.ResetForEncounter();
        engaged=true;phasePoseQueued=false;combatWasPaused=false;cutsceneActive=false;
        if(rig)rig.SetCombatAnchor(arenaPosition,arenaRotation);
        nextAttack=Time.time+4;hurtUntil=Time.time+4;
    }
    public void PerformStab() {
        if(IsCombatSuppressed || !isAttacking || impactDone || CurrentPhase>=Phase.Escaping) return;
        // Animation events cannot bypass the warning period.
        if(warning || !resolvingStab) return;
        impactDone=true;
        // Telegraphs never cause remote hits: damage follows the visible striking tip.
        if(!rig || !rig.HasStrike) return;
        Vector3 contact=rig.StrikeTip;
        if(audioFX) audioFX.PlayStabSound();
        var impact=SpiderCombatFX.Impact(contact,new Color(1,.8f,.35f)); Destroy(impact,.75f);
        if(!attackHasHit)attackHasHit=DamageArea(contact,Vector2.one*stabRadius*2,stabDamage*(isInPast?.65f:1));
        if(CurrentPhase==Phase.Mutated && targetIsland!=null && targetIsland.islandCollider &&
            Vector2.Distance(targetIsland.islandCollider.ClosestPoint(contact),contact)<=.65f)
            DamageIsland(targetIsland);
    }
    void DamageIsland(IslandAttackData island) {
        if(island==null || !island.islandCollider || island.isDestroyed || sinking.Contains(island) || CurrentPhase!=Phase.Mutated) return;
        island.impacts++;
        if(escapePassage) escapePassage.DamageWall();
        if(island.impacts==1) { MakeCrack(island); onWallCracked.Invoke(); }
        // Two confirmed physical stabs crack, then collapse the island.
        if(island.impacts>=2 && SafeIslands()>2) BeginIslandSink(island);
    }
    void BeginCoreExposure() {
        coreExposureStarted=Time.time;
        var safe=ClosestIsland();
        float floor=player.position.y-1;
        float x=player.position.x;
        if(safe!=null && safe.islandCollider) {
            var bounds=safe.islandCollider.bounds;
            floor=bounds.max.y;
            x=Mathf.Clamp(x,bounds.min.x+.4f,bounds.max.x-.4f);
        } else {
            var cat=player.GetComponent<CatPlayerController>();
            if(cat) { var ground=Physics2D.Raycast(player.position,Vector2.down,10,cat.groundLayer); if(ground.collider) floor=ground.point.y; }
        }
        coreExposureTarget=new Vector3(x,floor+1.3f,corePoint.position.z);
    }
    void DrawLimbStrike(Vector3 target) {
        BossPart active=null; Vector2 tip=transform.position; float best=float.PositiveInfinity;
        foreach(var leg in legs) if(leg && !leg.isDestroyed) {
            Vector2 candidate; if(!leg.TryGetHitPoint(target,out candidate)) continue;
            float d=Vector2.Distance(candidate,target); if(d<best) { best=d; active=leg; tip=candidate; }
        }
        if(CurrentPhase==Phase.Mutated && rig)tip=rig.MouthPosition;
        else if(!active)return;
        var beam=new GameObject("Spider energy lance"); transient.Add(beam);
        CreateBeamLine(beam,tip,target,.3f,new Color(.1f,.75f,1,.2f),32758);
        CreateBeamLine(beam,tip,target,.11f,new Color(.15f,.9f,1,.85f),32759);
        CreateBeamLine(beam,tip,target,.035f,Color.white,32760);
        var flare=SpiderCombatFX.Impact(target,new Color(.3f,1,1,.8f)); flare.transform.SetParent(beam.transform,true);
        StartCoroutine(FadeBeam(beam,.24f));
    }
    void CreateBeamLine(GameObject root,Vector3 from,Vector3 to,float width,Color color,int order) {
        var go=new GameObject("Beam glow"); go.transform.SetParent(root.transform,false);
        var line=go.AddComponent<LineRenderer>(); line.useWorldSpace=true;
        line.positionCount=2; line.SetPosition(0,from); line.SetPosition(1,to);
        line.numCapVertices=8; line.startWidth=width; line.endWidth=width*.35f;
        line.startColor=color; line.endColor=color; line.sharedMaterial=EffectMaterial();
        SetEffectSorting(line,order);
    }
    IEnumerator FadeBeam(GameObject root,float duration) {
        var lines=root.GetComponentsInChildren<LineRenderer>();
        float started=Time.time;
        while(root && Time.time-started<duration) {
            float fade=1-(Time.time-started)/duration;
            foreach(var line in lines) if(line) { var c=line.startColor; c.a=fade*(line.startWidth>.2f?.22f:1); line.startColor=c; line.endColor=c; }
            yield return null;
        }
        if(root) Destroy(root);
    }
    public void StartSweep() { }
    public void EndSweep() { if(sweepCollider) sweepCollider.enabled=false; }
    bool DamageArea(Vector2 center,Vector2 size,float damage) {
        if(Time.timeScale<=0 || IsCombatSuppressed || !player || Time.time<hurtUntil || CurrentPhase>=Phase.Escaping) return false;
        foreach(var collider in player.GetComponentsInChildren<Collider2D>()) {
            if(!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)continue;
            // Damage is two-dimensional. Sprite depth and elliptical corner rejection
            // previously discarded legitimate contacts visible on screen.
            Vector2 nearest=collider.ClosestPoint(center)-center;
            if(Mathf.Abs(nearest.x)>size.x*.5f || Mathf.Abs(nearest.y)>size.y*.5f)continue;
            hurtUntil=Time.time+.65f;playerHealth=Mathf.Max(0,playerHealth-damage);
            PlayerHurtFeedback.Notify(player.GetComponent<CatPlayerController>(),playerHealth,100,damage);
            SpiderCombatFX.Impact(collider.bounds.center,new Color(1,.45f,.25f),.45f);
            var rb=player.GetComponent<Rigidbody2D>();if(rb)rb.AddForce(new Vector2(player.position.x<center.x?-3:3,3),ForceMode2D.Impulse);
            if(playerHealth<=0)ReloadAfterDeath();
            return true;
        }
        return false;
    }
    bool deathReloading;
    Vector3 respawnLedge;
    bool ledgeCaptured;
    float deathFade;
    Texture2D deathTitle;
    public bool ApplyGooDamage(CatPlayerController cat,float damage) {
        if(!cat || !player || cat.transform!=player || cat.IsFrozen || deathReloading)return false;
        if(CurrentPhase<Phase.Escaping && (IsCutsceneActive || IsReentering))return false;
        playerHealth=Mathf.Max(0,playerHealth-Mathf.Max(0,damage));
        PlayerHurtFeedback.Notify(cat,playerHealth,100,damage,true);
        if(playerHealth<=0)ReloadAfterDeath();return true;
    }
    bool defeatedBeforeDeath;
    void ReloadAfterDeath() {
        if(deathReloading)return;
        defeatedBeforeDeath=CurrentPhase>=Phase.Escaping;deathReloading=true;
        if(!defeatedBeforeDeath)CancelAttack();
        StartCoroutine(DeathAndRespawn());
    }
    IEnumerator DeathAndRespawn() {
        var cat=player.GetComponent<CatPlayerController>();
        var catAnimator=player.GetComponent<Animator>();
        bool wasAnimating=catAnimator && catAnimator.enabled;
        if(cat)cat.Freeze();
        if(catAnimator)catAnimator.enabled=false;
        Vector3 scale=player.localScale;
        Quaternion rotation=player.localRotation;
        // A readable collapse, followed by a black hold and a gentle reveal.
        for(float t=0;t<1;t+=Time.unscaledDeltaTime) {
            float collapse=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.65f));
            player.localRotation=rotation*Quaternion.Euler(0,0,-65*collapse);
            player.localScale=Vector3.Scale(scale,new Vector3(1+.12f*collapse,1-.35f*collapse,1));
            deathFade=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.4f)/.6f));
            yield return null;
        }
        deathFade=1;
        yield return new WaitForSecondsRealtime(.45f);
        if(!defeatedBeforeDeath)ResetEncounterState();
        AcidVolume2D.ResetDamage();
        player.position=respawnLedge;player.localScale=scale;player.localRotation=rotation;
        var body=player.GetComponent<Rigidbody2D>();if(body){body.position=respawnLedge;body.velocity=Vector2.zero;body.angularVelocity=0;}
        playerHealth=100;PlayerHurtFeedback.ResetFor(cat);hurtUntil=Time.time+4;nextAttack=Time.time+4;
        if(catAnimator)catAnimator.enabled=wasAnimating;
        Physics2D.SyncTransforms();
        for(float t=0;t<.65f;t+=Time.unscaledDeltaTime){deathFade=1-Mathf.SmoothStep(0,1,t/.65f);yield return null;}
        deathFade=0;if(cat)cat.unFreeze();deathReloading=false;
    }
    void DrawDeathFade() {
        if(deathFade<=0)return;
        GUI.depth=-9000;var old=GUI.color;GUI.color=new Color(0,0,0,deathFade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        if(!deathTitle)deathTitle=Resources.Load<Texture2D>("BossFeedback/YouDied");
        if(deathTitle){float width=Mathf.Min(Screen.width*.6f,640);GUI.color=new Color(1,1,1,deathFade);GUI.DrawTexture(new Rect((Screen.width-width)*.5f,Screen.height*.43f,width,width*110/640),deathTitle,ScaleMode.ScaleToFit);}
        GUI.color=old;
    }
    // The authored pour event and attack fallback share one encounter-wide latch.
    public void FillAcidPool() {
        if(acidPoolFilled || CurrentPhase!=Phase.Mutated || IsCutsceneActive || IsReentering) return;
        acidPoolFilled=true;
        var events=GetComponent<runEvent>();
        if(events) events.Run(0); // Existing pool piston and pouring sound.
    }
    IslandAttackData ClosestIsland() {
        IslandAttackData result=null; float best=float.MaxValue;
        foreach(var island in islandAttacks) {
            if(island==null || !island.islandCollider || island.isDestroyed) continue;
            if(isInPast && island.timeline==IslandTimeline.Present || !isInPast && island.timeline==IslandTimeline.Past) continue;
            float d=Vector2.Distance(player.position,island.islandCollider.ClosestPoint(player.position));
            if(d<best) { best=d; result=island; }
        } return result;
    }
    int SafeIslands() { int n=0; foreach(var i in islandAttacks) if(i!=null && i.islandCollider && i.timeline!=IslandTimeline.Past && !i.isDestroyed) n++; return n; }
    void MakeCrack(IslandAttackData island) {
        var crack=SpiderCombatFX.Crack(island.islandCollider.bounds.center+Vector3.up*island.islandCollider.bounds.extents.y,Mathf.Min(3,island.islandCollider.bounds.size.x));
 crack.transform.SetParent(island.islandTransform,true); transient.Add(crack);
    }
    void InitializeIsland(IslandAttackData island) {
        if(island==null)return;
        if(island.settings==null)island.settings=new IslandSettings();
        if(island.islandCollider) {
            island.body=island.islandCollider.attachedRigidbody;
            island.islandTransform=island.body?island.body.transform:island.islandCollider.transform;
        }
        if(island.islandTransform) {island.startPos=island.islandTransform.position;island.startRotation=island.islandTransform.rotation;}
        island.floating=island.islandCollider is CircleCollider2D || (island.islandName!=null && island.islandName.ToLowerInvariant().Contains("floating"));
        if(island.floating && !island.body && island.islandTransform)island.body=island.islandTransform.gameObject.AddComponent<Rigidbody2D>();
        if(island.body && !island.floating) {
            // Original floating platforms were dynamic. Gravity was immediately pulling
            // them down again after the respawn transform had been restored.
            island.body.bodyType=RigidbodyType2D.Kinematic;island.body.gravityScale=0;
            island.body.velocity=Vector2.zero;island.body.angularVelocity=0;
        }
        island.floating=island.islandCollider is CircleCollider2D || (island.islandName!=null && island.islandName.ToLowerInvariant().Contains("floating"));
        island.isDestroyed=false;EnableFloatingPhysics(island);
    }
    void OwnIslandPhysics(IslandAttackData island) {
        if(!island.body && island.islandCollider)island.body=island.islandCollider.attachedRigidbody;
        if(island.body && !island.floating) {island.body.bodyType=RigidbodyType2D.Kinematic;island.body.gravityScale=0;
            island.body.velocity=Vector2.zero;island.body.angularVelocity=0;}
    }
    void EnableFloatingPhysics(IslandAttackData island){
        if(!island.floating || !island.body)return;
        island.body.bodyType=RigidbodyType2D.Dynamic;island.body.gravityScale=1;
        island.body.drag=.35f;island.body.angularDrag=1.5f;island.body.constraints=RigidbodyConstraints2D.None;
        island.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        island.body.interpolation=RigidbodyInterpolation2D.Interpolate;
    }
    void FixedUpdate(){
        foreach(var island in islandAttacks){
            if(island==null || !island.floating || island.isDestroyed || !island.body || !island.islandCollider || island.body.bodyType!=RigidbodyType2D.Dynamic)continue;
            var bounds=island.islandCollider.bounds;float submerged=0;
            foreach(var volume in acidVolumes){float surface;if(volume && volume.TryGetSurface(bounds.center.x,out surface))submerged=Mathf.Max(submerged,Mathf.Clamp01((surface-bounds.min.y)/Mathf.Max(.1f,bounds.size.y)));}
            if(submerged<=0)continue;
            float weight=island.body.mass*Physics2D.gravity.magnitude*island.body.gravityScale;
            island.body.AddForce(Vector2.up*weight*2.2f*submerged-island.body.velocity*island.body.mass*3.5f*submerged);
            island.body.AddTorque(-island.body.angularVelocity*.02f*submerged);
        }
    }
    void OnEnable() {
        if(!Application.isPlaying)return;
        foreach(var island in islandAttacks)if(island!=null) {
            OwnIslandPhysics(island);
            if(island.isDestroyed && island.islandTransform && !sinking.Contains(island)) {
                sinking.Add(island);islandSinkTimes[island]=Time.time;
            }
        }
    }
    void PositionIsland(IslandAttackData island,Vector3 position) {
        OwnIslandPhysics(island);
        if(island.body) {island.body.bodyType=RigidbodyType2D.Kinematic;island.body.gravityScale=0;island.body.position=position;island.body.rotation=island.startRotation.eulerAngles.z;
            island.body.velocity=Vector2.zero;island.body.angularVelocity=0;}
        island.islandTransform.SetPositionAndRotation(position,island.startRotation);
    }
    void BeginIslandSink(IslandAttackData island) {
        sinking.Add(island);island.isDestroyed=true;islandSinkTimes[island]=Time.time;
        SpawnIslandEffect(island.settings.sinkParticles,island.islandCollider.bounds.center,island.settings.sinkParticalsExpiration);
    }
    void UpdateIslands() {
        restoredIslands.Clear();
        foreach(var island in sinking) {
            if(!island.islandTransform || !island.islandCollider) { restoredIslands.Add(island);continue; }
            float elapsed=Time.time-islandSinkTimes[island];
            float duration=1/Mathf.Max(.5f,island.settings.sinkSpeed);
            Vector3 lowered=island.startPos+Vector3.down*Mathf.Max(2,island.settings.sinkAmount);
            float respawnAt=1+duration+Mathf.Clamp(island.settings.respawnDelay,5,8);
            if(elapsed<respawnAt) {
                PositionIsland(island,Vector3.Lerp(island.startPos,lowered,Mathf.Clamp01((elapsed-1)/duration)));
                island.islandCollider.enabled=elapsed<1+duration;
            } else {
                float rise=Mathf.Clamp01((elapsed-respawnAt)/.75f);
                PositionIsland(island,Vector3.Lerp(lowered,island.startPos,Mathf.SmoothStep(0,1,rise)));
                island.islandCollider.enabled=rise>=1;
                if(rise>=1) { island.impacts=0;island.isDestroyed=false;EnableFloatingPhysics(island);restoredIslands.Add(island);
                    SpawnIslandEffect(island.settings.respawnParticles,island.startPos,island.settings.respawnParticalsExpiration); }
            }
        }
        foreach(var island in restoredIslands) {sinking.Remove(island);islandSinkTimes.Remove(island);}
    }
    void RestoreIslands() {
        foreach(var island in islandAttacks) if(island.islandTransform && island.islandCollider) {
            PositionIsland(island,island.startPos);island.islandCollider.enabled=true;
            island.isDestroyed=false;island.impacts=0;EnableFloatingPhysics(island);
        }
        sinking.Clear();islandSinkTimes.Clear();
    }
    void SpawnIslandEffect(GameObject prefab,Vector3 point,float duration) {
        if(prefab) { var effect=Instantiate(prefab,point,Quaternion.identity); Destroy(effect,Mathf.Max(1,duration)); }
    }
    void CancelAttack() {
        IsPouringAcid=IsHeadSlam=false;activeStrikeWindow=false;attackingArm=null;StopAllCoroutines(); if(animator) animator.speed=1; feedbackRunning=false;
        if(rig) rig.ClearOverrides();
        if(coreLowered) { if(rig) rig.ReleaseHead(); coreLowered=false; }
        // Island lifecycles run independently of attack cancellation and cutscenes.
        foreach(var go in transient) if(go) Destroy(go); transient.Clear();
        if(warning) Destroy(warning); warning=null; EndSweep(); isAttacking=false; isVulnerable=false;
    }
    IEnumerator Escape() {
        PlayState("Base Layer.Phase 2 Present.idle");
        SetBarrier(false);RestoreIslands();
        var cat=player?player.GetComponent<CatPlayerController>():null;
        if(cat)cat.unFreeze(); // The safe escape sequence never suspends the cat in midair.
        if(audioFX)audioFX.PlayEscapeSound();
        BossMusicPlayback.PlayOutro(outroMusic);
        if(escapePassage){escapePassage.DamageWall();rig.SetClimbSurface(escapePassage.BreachPoint.x);}
        Vector3 start=transform.position;
        Quaternion rotation=transform.rotation;
        Vector3 headOffset=corePoint.position-start;
        Vector3 breach=escapePassage?escapePassage.BreachPoint:escapePoint?escapePoint.position:start+Vector3.right*7;
        Vector3 contact=breach-headOffset;
        Vector3 brace=contact-Vector3.right*2;
        // Brace, recoil and drive the attached chassis through the fractured wall.
        for(float t=0;t<.65f;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/.65f);rig.SetBodyPose(start+Vector3.left*(Mathf.Sin(u*Mathf.PI)*.35f),rotation);yield return null;}
        for(float t=0;t<1.1f;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/1.1f);rig.SetBodyPose(Vector3.Lerp(start,brace,u),Quaternion.Slerp(rotation,Quaternion.identity,u));yield return null;}
        for(float t=0;t<.4f;t+=Time.deltaTime){float u=t/.4f;rig.SetBodyPose(Vector3.Lerp(brace,contact,u*u),Quaternion.identity);yield return null;}
        rig.SetBodyPose(contact,Quaternion.identity);
        if(escapePassage)escapePassage.Open();else if(escapeWall)escapeWall.SetActive(false);
        SpiderCombatFX.Impact(breach,new Color(.65f,.75f,.58f),2);
        yield return new WaitForSeconds(.25f);
        // Physically clear the breach, then climb the wall and leave above the camera.
        Vector3 climbStart=contact+Vector3.right*3;
        for(float t=0;t<1.2f;t+=Time.deltaTime){rig.SetBodyPose(Vector3.Lerp(contact,climbStart,Mathf.SmoothStep(0,1,t/1.2f)),Quaternion.identity);yield return null;}
        float lowest=corePoint.position.y;foreach(var renderer in renderers)if(renderer && renderer.enabled)lowest=Mathf.Min(lowest,renderer.bounds.min.y);
        var camera=GameplayCamera();float cameraTop=camera?camera.transform.position.y+camera.orthographicSize:climbStart.y+8;
        float rise=Mathf.Max(14,cameraTop-lowest+5);Vector3 departed=climbStart+Vector3.up*rise;
        float climbDuration=Mathf.Max(2.5f,rise/5);
        for(float t=0;t<climbDuration;t+=Time.deltaTime){rig.SetBodyPose(Vector3.Lerp(climbStart,departed,Mathf.SmoothStep(0,1,t/climbDuration)),Quaternion.identity);yield return null;}
        rig.SetBodyPose(departed,Quaternion.identity);
        if(openedPassage)openedPassage.SetActive(true);
        foreach(var collider in GetComponentsInChildren<Collider2D>())collider.enabled=false;
        CurrentPhase=Phase.Complete;if(cat && !deathReloading)cat.unFreeze();onEncounterWon.Invoke();
    }
    public void Feedback(Vector3 point) {
        if(audioFX) audioFX.PlayDamageSound();
        if(!feedbackRunning && CurrentPhase<Phase.Escaping) StartCoroutine(HitPause());
        var spark=SpiderCombatFX.Impact(point,isInPast?new Color(.5f,.9f,1):new Color(1,.8f,.4f),.5f); Destroy(spark,.75f);
        var vfx=GetComponent<BossVFXController>(); if(vfx) vfx.PlayHitVFX(point);
    }
    IEnumerator HitPause() {
        feedbackRunning=true;
        float speed=animator.speed; animator.speed=0;
        yield return new WaitForSecondsRealtime(.045f);
        animator.speed=speed; feedbackRunning=false;
    }
    public void SetLegInvulnerability(bool state) { foreach(var leg in legs) if(leg) leg.SetInvulnerable(state); }
    static Material EffectMaterial() {
        if(!effectMaterial) { var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"); if(!shader) shader=Shader.Find("Sprites/Default"); if(shader) effectMaterial=new Material(shader); }
        return effectMaterial;
    }
    static void SetEffectSorting(Renderer renderer,int order) {
        renderer.sortingOrder=order;
        var layers=SortingLayer.layers;
        if(layers.Length>0) { var highest=layers[0]; foreach(var layer in layers) if(layer.value>highest.value) highest=layer; renderer.sortingLayerID=highest.id; }
    }
    public void ToggleTimeline() { if(timelineTraversal) timelineTraversal.TryTraverse(); }
    void ReleaseTimeline() {
        if(firstDefeatReleased || finalReached) return;
        firstDefeatReleased=true;
        foreach(var blocker in timelineLocks) if(blocker) blocker.enabled=false;
        SetBarrier(false);
        if(timelineTraversal) { timelineTraversal.gameObject.SetActive(true); timelineTraversal.enabled=true; }
    }
    void InitializeBarrier() {
        if(!teleportBarrier) foreach(var particles in FindObjectsOfType<ParticleSystem>(true))
            if(particles.gameObject.name=="no travel") { teleportBarrier=particles.gameObject; break; }
        if(!teleportBarrier) return;
        if(!teleportBarrierCollider) teleportBarrierCollider=teleportBarrier.GetComponent<Collider2D>();
        barrierParticles=teleportBarrier.GetComponentsInChildren<ParticleSystem>(true);
        barrierParticleBuffers=new ParticleSystem.Particle[barrierParticles.Length][];
        for(int i=0;i<barrierParticles.Length;i++) barrierParticleBuffers[i]=new ParticleSystem.Particle[barrierParticles[i].main.maxParticles];
        barrierVolumes=teleportBarrier.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true);
        barrierVolumeWeights=new float[barrierVolumes.Length];
        for(int i=0;i<barrierVolumes.Length;i++) barrierVolumeWeights[i]=barrierVolumes[i].weight;
        barrierWanted=teleportBarrier.activeSelf;
        barrierAlpha=barrierPreviousAlpha=barrierWanted?1:0;
    }
    void OnTimelineTraversed() {
        if(CurrentPhase>=Phase.Escaping) return;
        CancelAttack();
        reentryRequested=engaged;
        if(shellReached) finalReached=true;
        SetBarrier(true);
    }
    void SetBarrier(bool visible) {
        barrierWanted=visible;
        if(teleportBarrierCollider) teleportBarrierCollider.enabled=visible;
        if(!teleportBarrier) return;
        if(visible) {
            teleportBarrier.SetActive(true); barrierAlpha=barrierPreviousAlpha=1;
            if(barrierParticles!=null) foreach(var particles in barrierParticles) {
                if(!particles.isPlaying) particles.Play();
            }
            if(barrierVolumes!=null) for(int i=0;i<barrierVolumes.Length;i++) barrierVolumes[i].weight=barrierVolumeWeights[i];
        } else if(barrierParticles!=null) foreach(var particles in barrierParticles)
            particles.Stop(false,ParticleSystemStopBehavior.StopEmitting);
    }
    void Update() {
        UpdateIslands();
        if(isAttacking && !deathReloading && CurrentPhase<Phase.Escaping && Time.time-attackStarted>16) {
            CancelAttack();PlayIdle();nextAttack=Time.time+1.5f;hurtUntil=Mathf.Max(hurtUntil,Time.time+1);
        }
        // Independent of attack coroutines: changing phase cannot interrupt the barrier fade.
        if(!teleportBarrier || barrierWanted || barrierAlpha<=0) return;
        barrierAlpha=Mathf.MoveTowards(barrierAlpha,0,Time.deltaTime/Mathf.Max(.1f,barrierFadeDuration));
        float ratio=barrierPreviousAlpha>.0001f?barrierAlpha/barrierPreviousAlpha:0;
        if(barrierParticles!=null) for(int index=0;index<barrierParticles.Length;index++) {
            var system=barrierParticles[index];
            var particles=barrierParticleBuffers[index];
            int count=system.GetParticles(particles);
            for(int i=0;i<count;i++) { var color=particles[i].startColor; color.a=(byte)Mathf.RoundToInt(color.a*ratio); particles[i].startColor=color; }
            system.SetParticles(particles,count);
        }
        if(barrierVolumes!=null) for(int i=0;i<barrierVolumes.Length;i++) barrierVolumes[i].weight=barrierVolumeWeights[i]*barrierAlpha;
        barrierPreviousAlpha=barrierAlpha;
        if(barrierAlpha<=0) {
            if(barrierParticles!=null) foreach(var particles in barrierParticles) particles.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            teleportBarrier.SetActive(false);
        }
    }
    void OnDestroy() {foreach(var clip in safeClips)if(clip){if(Application.isPlaying)Destroy(clip);else DestroyImmediate(clip);}if(combatOverrides){if(Application.isPlaying)Destroy(combatOverrides);else DestroyImmediate(combatOverrides);} if(timelineTraversal) timelineTraversal.onTraverseSuccess.RemoveListener(OnTimelineTraversed); }
    public bool IsTargetArm(BossPart part) { return part && (part==leftArm || part==rightArm); }
    public bool CanLoseArm {
        get {
            if(shellReached || IsCombatSuppressed || CurrentPhase!=Phase.ExposedLegs) return false;
            int lost=0; foreach(var leg in legs) if(leg && leg.isDestroyed) lost++;
            return lost<RequiredArmLosses;
        }
    }
    public bool IsCutsceneActive {
        get {
            if(cutsceneActive) return true;
            var cat=player?player.GetComponent<CatPlayerController>():null;
            if(cat && cat.IsFrozen) return true;
            if(!animator || !animator.isActiveAndEnabled) return false;
            if(IsIntroState(animator.GetCurrentAnimatorStateInfo(0))) return true;
            return animator.IsInTransition(0) && IsIntroState(animator.GetNextAnimatorStateInfo(0));
        }
    }
    public bool IsCombatSuppressed => deathReloading || !isActiveAndEnabled || IsCutsceneActive || IsReentering || reentryRequested || Time.time<animationBusyUntil;
    static bool IsIntroState(AnimatorStateInfo state) {
        return state.IsName("Base Layer.initial") || state.IsName("Base Layer.reveal") || state.IsName("Base Layer.reveal_past");
    }
    public void BeginCutscene() {
        cutsceneActive=true; combatWasPaused=true;
        CancelAttack();
    }
    public void EndCutscene() {
        cutsceneActive=false;
        nextAttack=Time.time+2;
        hurtUntil=Mathf.Max(hurtUntil,Time.time+1);
    }
    public float OnLegDestroyed(BossPart part) {
        CancelAttack();
        int number=0; foreach(var leg in legs) if(leg && leg.isDestroyed) number++;
        bool right=part==rightArm;
        string clip=right?"past_lose_arm_2":"past_lose_arm_1";
        string state=right?"Base Layer.Phase 1 Past.no right arm.past_lose_arm_2":"Base Layer.Phase 1 Past.no left arm.past_lose_arm_1";
        float duration=ClipDuration(clip,1);
        animationBusyUntil=Time.time+duration; armLossUntil=animationBusyUntil; nextAttack=animationBusyUntil+1;
        PlayState(state);
        phasePoseQueued=true;
        if(number>=RequiredArmLosses) shellReached=true;
        return animationBusyUntil;
    }
    float ClipDuration(string name,float fallback) {
        if(animator.runtimeAnimatorController) foreach(var clip in animator.runtimeAnimatorController.animationClips) if(clip.name==name) return Mathf.Max(.1f,clip.length);
        return fallback;
    }
    void PlayState(string name) {
        animator.speed=1;
        int hash=Animator.StringToHash(name); if(!animator.HasState(0,hash)) return;
        foreach(var p in animator.parameters) if(p.type==AnimatorControllerParameterType.Trigger) animator.ResetTrigger(p.nameHash);
        if(animator.GetCurrentAnimatorStateInfo(0).fullPathHash==hash && !animator.IsInTransition(0)) return;
        if(animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash==hash) return;
        float blend=IsPlayingArmLoss?.06f:isAttacking?.08f:CurrentPhase==Phase.Shell?.25f:.18f;
        animator.CrossFadeInFixedTime(hash,blend,0,0);
    }
    AnimationClip FindClip(string name) {
        if(animator && animator.runtimeAnimatorController)
            foreach(var clip in animator.runtimeAnimatorController.animationClips) if(clip.name==name) return clip;
        return null;
    }
    AnimationClip FindStateClip(string state) {
        string name=state.Substring(state.LastIndexOf('.')+1);
        return FindClip(name=="past_no_arm_1_stab"?"past_no_arm_1_stab_bench_1":name);
    }
    void SampleEntrance(float progress) {
        if(entranceClip) {
            // Pose-only sampling: reuse the actual walk-in without replaying camera/player events.
            entranceClip.SampleAnimation(gameObject,Mathf.Lerp(3.8166666f,8.666667f,progress));
        } else rig.SetBodyPose(Vector3.Lerp(arenaPosition+new Vector3(8.2f,-.75f,0),arenaPosition,Mathf.SmoothStep(0,1,progress)),arenaRotation);
        foreach(var leg in legs) if(leg) leg.RefreshPresentation();
    }
    float SyncAttackAnimation(string state,float windup) {
        var clip=FindStateClip(state); if(!clip) return .6f;
        // Impact timings measured from the authored poses/events, not the end of the clip.
        float impact=clip.name=="island 3 sweep attack"?.5f:clip.name=="barf acid"?.6666667f:
            clip.name=="past_no_arm_1_stab_bench_1"?.9f:Mathf.Min(.5f,clip.length*.55f);
        foreach(var animationEvent in clip.events) if(animationEvent.functionName=="PerformStab") { impact=animationEvent.time; break; }
        animator.speed=Mathf.Clamp(impact/Mathf.Max(.1f,windup),.1f,1);
        // The last 2.45 seconds of the pour clip are its old exit-to-offscreen sequence.
        float end=clip.name=="barf acid"?4.3f:clip.length;
        return Mathf.Max(0,(end-impact)/animator.speed);
    }
    void PlayIdle() {
        foreach(var shake in FindObjectsOfType<CameraShake2D>()) shake.StopShake();
        if(CurrentPhase==Phase.Mutated) PlayState("Base Layer.Phase 2 Present.idle");
        else if(CurrentPhase==Phase.ExposedLegs) PlayState(leftArm && leftArm.isDestroyed?"Base Layer.past_idle_no_arm1":rightArm && rightArm.isDestroyed?"Base Layer.past_idle_no_arm_2":"Base Layer.past_idle");
        else PlayState("Base Layer.Phase 1 Present.idle");
    }
    void Trigger(string name) { foreach(var p in animator.parameters) if(p.name==name && p.type==AnimatorControllerParameterType.Trigger) { animator.SetTrigger(name); break; } }
    void SetBool(string name,bool value) { foreach(var p in animator.parameters) if(p.name==name && p.type==AnimatorControllerParameterType.Bool) { animator.SetBool(name,value); break; } }
    void SetInt(string name,int value) { foreach(var p in animator.parameters) if(p.name==name && p.type==AnimatorControllerParameterType.Int) { animator.SetInteger(name,value); break; } }
    void OnDisable() { CancelAttack();RestoreIslands(); }
    Camera GameplayCamera() {
        var main=Camera.main;
        if(main && main.enabled && !main.targetTexture) return main;
        Camera best=null;
        foreach(var camera in Camera.allCameras) {
            if(!camera.enabled || camera.targetTexture || camera.cameraType!=CameraType.Game) continue;
            if(!best || camera.CompareTag("MainCamera") || camera.depth>best.depth) best=camera;
        }
        return best;
    }
    void DrawBar(Rect rect,float fraction,Color fill) {
        Color old=GUI.color;
        GUI.color=new Color(.035f,.045f,.055f,1); GUI.DrawTexture(rect,Texture2D.whiteTexture);
        GUI.color=fill; GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,Mathf.Max(0,rect.width-4)*Mathf.Clamp01(fraction),Mathf.Max(0,rect.height-4)),Texture2D.whiteTexture);
        GUI.color=old;
    }
    void DrawHudLine(Vector2 from,Vector2 to,Color color) {
        var oldMatrix=GUI.matrix; var oldColor=GUI.color;
        GUI.color=color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
        GUI.DrawTexture(new Rect(from.x,from.y,Vector2.Distance(from,to),1),Texture2D.whiteTexture);
        GUI.matrix=oldMatrix; GUI.color=oldColor;
    }
    void OnGUI() {
        if(deathReloading){DrawDeathFade();return;}

        if(!engaged || IsCutsceneActive || CurrentPhase>=Phase.Escaping) return;
        var camera=GameplayCamera(); if(!camera) return;
        Rect pixels=camera.pixelRect;
        Rect viewport=new Rect(pixels.x,Screen.height-pixels.yMax,pixels.width,pixels.height);
        // Group clips every HUD pixel to the camera's cleared game image, including after resizing.
        GUI.BeginGroup(viewport);
        float width=Mathf.Min(420,viewport.width*.55f), x=(viewport.width-width)*.5f;
        float fraction=CoreHealth/Mathf.Max(1,coreMaxHealth);
        if(CurrentPhase==Phase.ExposedLegs) {
            float remaining=0,total=0; foreach(var leg in legs) if(leg && IsTargetArm(leg)) { total+=leg.MaxHealth; remaining+=Mathf.Max(0,leg.health); }
            fraction=total>0?remaining/total:1;
        }
        GUI.Label(new Rect(x,12,width,22),"Mechanical Spider");
        DrawBar(new Rect(x,38,width,20),fraction,CurrentPhase==Phase.ExposedLegs?Color.cyan:CurrentPhase==Phase.Shell?Color.gray:Color.green);
        GUI.Label(new Rect(18,viewport.height-36,115,24),"Health: "+Mathf.CeilToInt(playerHealth));
        if(CurrentPhase==Phase.ExposedLegs) foreach(var leg in legs) if(leg && IsTargetArm(leg)) {
            Bounds bounds; if(!leg.TryGetVisibleBounds(out bounds)) continue;
            Vector2 anchor;
            if(!leg.TryGetHitPoint(player? (Vector2)player.position : (Vector2)bounds.center,out anchor)) continue;
            Vector3 screen=camera.WorldToScreenPoint(new Vector3(anchor.x,anchor.y,bounds.center.z));
            if(screen.z<=0 || !pixels.Contains(new Vector2(screen.x,screen.y))) continue;
            float ax=screen.x-pixels.x, ay=pixels.yMax-screen.y;
            float lx=Mathf.Clamp(ax+(leg==leftArm?-70:14),4,viewport.width-68);
            float ly=Mathf.Clamp(ay-30,75,viewport.height-50);
            DrawHudLine(new Vector2(lx+32,ly+9),new Vector2(ax,ay),Color.cyan);
            DrawBar(new Rect(lx,ly,64,9),leg.health/leg.MaxHealth,Color.cyan);
            GUI.Label(new Rect(lx,ly-20,90,20),leg==leftArm?"Left arm":"Right arm");
        }
        GUI.EndGroup();
    }
}
