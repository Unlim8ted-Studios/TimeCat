using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpiderIKLimb {
    public BossPart part;
    public Transform root, joint, tip, target, pole;
    public SpiderIKTarget2D targetSettings;
    [Range(0,1)] public float weight;
    public float bendDirection = 1;
    [HideInInspector] public Quaternion restRoot, restJoint;
    [NonSerialized] public Quaternion displayedRoot, displayedJoint;
    [NonSerialized] public Vector3 displayedPosition;
    [NonSerialized] public bool poseReady;
    [NonSerialized] public Quaternion authoredRoot, authoredJoint;
    [NonSerialized] public Vector3 authoredPosition, authoredScale, authoredJointPosition, authoredJointScale;
    [NonSerialized] public bool restoreAuthored;
}

// Analytic planar IK. Targets can be keyed by Animator, edited in Scene view or driven by attacks.
[DefaultExecutionOrder(-10000)]
public class SpiderBossRig2D : MonoBehaviour {
    public List<SpiderIKLimb> limbs = new List<SpiderIKLimb>();
    public Transform head;
    public bool previewInEditor;
    [Range(0,1)] public float proceduralWeight = 1;
    SpiderBossController boss;
    SpiderIKLimb striking;
    Vector3 strikeStart, strikeEnd;
    float strikeTime, strikeWindup;
    bool headOverride, bodyOverride, mouthOverride;
    Vector3 mouthPosition;
    Vector3 headPosition, bodyPosition;
    Quaternion bodyRotation;
    Transform upperFang, lowerFang;
    float jawOpen, shellWeight;
    float climbSurfaceX;
    bool climbSurfaceSet;
    [Header("Attached head and pose blending")]
    public float maximumBodyLowering=2.6f;
    public float ActiveDrop => boss && boss.CurrentPhase==SpiderBossController.Phase.Mutated?8.5f:maximumBodyLowering;
    public float ActiveLean => boss && boss.CurrentPhase==SpiderBossController.Phase.Mutated?1.35f:maximumBodyLean;
    public void ResetEncounterPose() {
        RestoreAnimationRoot();ClearOverrides();supportPlants.Clear();plantedFeet=null;
        bodyLowering=loweringVelocity=Vector3.zero;oneArmRecenter=recenterVelocity=0;bodyPositionReady=bodyAngleReady=false;
        bodyPositionVelocity=Vector3.zero;lastEvaluationFrame=-1;
        foreach(var limb in limbs){limb.poseReady=false;limb.restoreAuthored=false;}
    }
    public float firstPhaseHeightLift=2.2f;
    public float maximumBodyLean=.45f;
    public float loweringSpeed=2.2f;
    public float poseAngularSpeed=240;
    Vector3 bodyLowering, loweringVelocity;
    Vector3[] plantedFeet, escapeFeet;
    Vector3 escapeOrigin;
    sealed class SupportPlant {
        public Vector3 tip, rootPosition, rootScale, jointPosition, jointScale;
        public float bend;
    }
    readonly Dictionary<SpiderIKLimb,SupportPlant> supportPlants=new Dictionary<SpiderIKLimb,SupportPlant>();
    Vector3 displayedBodyPosition, bodyPositionVelocity;
    bool bodyPositionReady;
    bool SupportsActive => combatAnchored && boss && !boss.IsCutsceneActive && !boss.IsReentering &&
        boss.CurrentPhase!=SpiderBossController.Phase.Shell && boss.CurrentPhase<SpiderBossController.Phase.Escaping;
    bool IsSupport(SpiderIKLimb limb) => boss && limb.part && limb.part!=boss.leftArm && limb.part!=boss.rightArm;
    int lastEvaluationFrame=-1;
    Vector3 restHeadLocal;
    float acidAttachmentUntil;
    bool combatAnchored;
    // Root coordinates shared by the authored idle/attack clips.
    public Vector3 authoredCombatOrigin=new Vector3(25.68f,5.76f,0);
    Vector3 combatOffset;
    Vector3 animationPosition, animationScale;
    Quaternion animationRotation;
    bool rootPoseApplied;
    float oneArmRecenter,recenterVelocity;
    float displayedBodyAngle;bool bodyAngleReady;
    public Vector3 StrikeRoot => striking!=null && striking.root?striking.root.position:strikeEnd;
    public Vector3 StrikeJoint => striking!=null && striking.joint?striking.joint.position:StrikeRoot;
    public Vector3 StrikeTarget => strikeEnd;
    public bool HasStrike => striking!=null;
    public SpiderIKLimb StrikingLimb => striking;
    public Vector3 StrikeTip => striking!=null && striking.tip ? striking.tip.position : strikeEnd;
    public bool StrikeReachedTarget(float tolerance) => striking!=null && striking.tip && Vector2.Distance(striking.tip.position,strikeEnd)<=tolerance;
    static Sprite fangSprite;

    bool initialized;
    public void Initialize(SpiderBossController owner) {
        if(initialized) return; initialized=true;
        boss=owner;
        if(!head) head=owner.corePoint;
        if(limbs.Count==0) BuildRig();
        if(head) restHeadLocal=head.localPosition;
        BuildFangs();
    }
    [ContextMenu("Build IK targets from spider artwork")]
    public void BuildRig() {
        if(!boss) boss=GetComponent<SpiderBossController>();
        if(!boss) return;
        if(!head) head=boss.corePoint?boss.corePoint:transform.Find("head");
        if(head) restHeadLocal=head.localPosition;
        foreach(var limb in limbs) if(limb.root) { limb.root.localRotation=limb.restRoot; if(limb.joint) limb.joint.localRotation=limb.restJoint; }
        limbs.Clear();
        var container=transform.Find("IK Targets");
        if(!container) { container=new GameObject("IK Targets").transform; container.SetParent(transform,false); }
        foreach(var part in boss.legs) {
            if(!part) continue;
            var root=part.transform; Transform joint=null;
            foreach(Transform child in root) if(child.GetComponent<SpriteRenderer>()) { joint=child; break; }
            Transform endParent=joint?joint:root;
            var sr=endParent.GetComponent<SpriteRenderer>();
            if(!sr || !sr.sprite) continue;
            var tip=endParent.Find("IK Tip");
            if(!tip) { tip=new GameObject("IK Tip").transform; tip.SetParent(endParent,false); }
            Vector2 farthest=Vector2.zero;
            foreach(var vertex in sr.sprite.vertices) if(vertex.sqrMagnitude>farthest.sqrMagnitude) farthest=vertex;
            if(sr.flipX) farthest.x=-farthest.x; if(sr.flipY) farthest.y=-farthest.y;
            tip.localPosition=farthest;
            var target=container.Find(root.name+" Target");
            if(!target) { target=new GameObject(root.name+" Target").transform; target.SetParent(container,false); }
            target.position=tip.position;
            var pole=container.Find(root.name+" Pole");
            if(!pole) { pole=new GameObject(root.name+" Pole").transform; pole.SetParent(container,false); }
            pole.position=(joint?joint.position:root.position)+Vector3.up;
            var settings=target.GetComponent<SpiderIKTarget2D>(); if(!settings) settings=target.gameObject.AddComponent<SpiderIKTarget2D>(); settings.pole=pole;
            float bend=1;
            if(joint) { Vector2 a=joint.position-root.position,b=tip.position-joint.position; bend=-Mathf.Sign(a.x*b.y-a.y*b.x); }
            limbs.Add(new SpiderIKLimb { part=part,root=root,joint=joint,tip=tip,target=target,pole=pole,targetSettings=settings,bendDirection=bend,restRoot=root.localRotation,restJoint=joint?joint.localRotation:Quaternion.identity });
        }
    }
    public static Vector2 SolveJoint(Vector2 root,Vector2 target,float upper,float lower,float bend) {
        Vector2 delta=target-root;
        float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.0001f,Mathf.Max(.0002f,upper+lower-.0001f));
        Vector2 direction=delta.sqrMagnitude>.000001f?delta.normalized:Vector2.right;
        float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
        float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        return root+direction*along+new Vector2(-direction.y,direction.x)*height*Mathf.Sign(bend==0?1:bend);
    }
    public void CancelStab() { striking=null; }
    public void BeginStab(Vector3 target,float windup,BossPart preferred=null) {
        striking=null; strikeEnd=target; float closest=float.PositiveInfinity;
        foreach(var limb in limbs) if(limb.part && !limb.part.isDestroyed && limb.tip && (!preferred || limb.part==preferred) &&
            (limb.part==boss.leftArm || limb.part==boss.rightArm)) {
            float reach=limb.joint?Vector2.Distance(limb.root.position,limb.joint.position)+Vector2.Distance(limb.joint.position,limb.tip.position):Vector2.Distance(limb.root.position,limb.tip.position);
            float distance=Vector2.Distance(limb.tip.position,target)+Mathf.Max(0,Vector2.Distance(limb.root.position,target)-reach)*20;
            if(distance<closest) { closest=distance; striking=limb; }
        }
        if(striking==null) return;
        Vector3 from=striking.root.position;
        float length=striking.joint?Vector2.Distance(from,striking.joint.position)+Vector2.Distance(striking.joint.position,striking.tip.position):Vector2.Distance(from,striking.tip.position);
        target=from+Vector3.ClampMagnitude(target-from,Mathf.Max(.01f,length-.01f));
        // Reject targets outside the anatomical swing range rather than turning an arm over.
        Quaternion beforeRoot=striking.root.localRotation;
        Quaternion beforeJoint=striking.joint?striking.joint.localRotation:Quaternion.identity;
        Solve(striking,target,1);
        bool outside=Mathf.Abs(Mathf.DeltaAngle(striking.restRoot.eulerAngles.z,striking.root.localEulerAngles.z))>65.1f ||
            (striking.joint && Mathf.Abs(Mathf.DeltaAngle(striking.restJoint.eulerAngles.z,striking.joint.localEulerAngles.z))>80.1f);
        striking.root.localRotation=beforeRoot;if(striking.joint)striking.joint.localRotation=beforeJoint;
        if(outside) {striking=null;return;}
        strikeStart=striking.tip.position; strikeEnd=target; strikeTime=Time.time; strikeWindup=Mathf.Max(.2f,windup);
    }
    public Vector3 MouthPosition {
        get { var sr=head?head.GetComponent<SpriteRenderer>():null; return sr?new Vector3(sr.bounds.center.x,sr.bounds.min.y+.15f,sr.bounds.center.z):transform.position; }
    }
    public static Vector3 ClampHeadOffset(Vector3 offset,float drop,float lean) {
        return new Vector3(Mathf.Clamp(offset.x,-Mathf.Abs(lean),Mathf.Abs(lean)),Mathf.Clamp(offset.y,-Mathf.Abs(drop),.15f),0);
    }
    public void SetHeadPose(Vector3 position,Quaternion rotation,float open=0) {
        if(!headOverride && plantedFeet==null) {
            plantedFeet=new Vector3[limbs.Count];
            for(int i=0;i<limbs.Count;i++) if(limbs[i].tip) plantedFeet[i]=limbs[i].tip.position;
        }
        headOverride=true; headPosition=position; jawOpen=open;
    }
    public void SetShellWeight(float value) { shellWeight=Mathf.Clamp01(value); }
    public void SetClimbSurface(float x) { climbSurfaceX=x; climbSurfaceSet=true; }
    public void ReleaseHead() { headOverride=false; jawOpen=0; }
    public void ReleaseBodyPose(){bodyOverride=mouthOverride=false;}
    public void SetMouthPose(Vector3 position,Quaternion rotation) { mouthOverride=bodyOverride=true;mouthPosition=position;bodyRotation=rotation; }
    public void SetBodyPose(Vector3 position,Quaternion rotation) { mouthOverride=false;bodyOverride=true; bodyPosition=position; bodyRotation=rotation; }
    public void SetCombatAnchor(Vector3 position,Quaternion rotation) {
        Vector3 origin=transform.parent?transform.parent.TransformPoint(authoredCombatOrigin):authoredCombatOrigin;
        combatOffset=position-origin;
        combatAnchored=true;
    }
    void Update() {
        RestoreAnimationRoot();
        // Restore the animation input before the next Animator evaluation; never feed IK back into itself.
        foreach(var limb in limbs) if(limb.restoreAuthored && limb.root) {
            limb.root.localRotation=limb.authoredRoot;
            limb.root.localPosition=limb.authoredPosition;
            limb.root.localScale=limb.authoredScale;
            if(limb.joint) {
                limb.joint.localRotation=limb.authoredJoint;
                limb.joint.localPosition=limb.authoredJointPosition;
                limb.joint.localScale=limb.authoredJointScale;
            }
            limb.restoreAuthored=false;
        }
    }
    void RestoreAnimationRoot() {
        if(!rootPoseApplied) return;
        // Restore exact Animator input; never subtract offsets from a pose Animator replaced.
        transform.localPosition=animationPosition;
        transform.localRotation=animationRotation;
        transform.localScale=animationScale;
        rootPoseApplied=false;
    }
    public void ClearOverrides() { striking=null; headOverride=false; bodyOverride=mouthOverride=false; jawOpen=0; shellWeight=0; climbSurfaceSet=false; }
    public void Evaluate() {
        if(Application.isPlaying && lastEvaluationFrame==Time.frameCount) return;
        RestoreAnimationRoot();
        animationPosition=transform.localPosition;
        animationRotation=transform.localRotation;
        animationScale=transform.localScale;
        rootPoseApplied=true;
        bool fresh=!Application.isPlaying || lastEvaluationFrame!=Time.frameCount;
        float dt=Application.isPlaying?(fresh?Mathf.Min(Time.deltaTime,.05f):0):1f/60;
        lastEvaluationFrame=Time.frameCount;
        if(combatAnchored && !bodyOverride && boss && !boss.IsCutsceneActive) {
            // Keep authored recoil, body tilt, lunges and curl/drop. Only relocate the whole
            // performance when the encounter is placed away from its original arena.
            transform.position+=combatOffset;
            Vector3 arenaOrigin=transform.parent?transform.parent.TransformPoint(authoredCombatOrigin):authoredCombatOrigin;
            var bounded=transform.position;bounded.x=Mathf.Clamp(bounded.x,arenaOrigin.x+combatOffset.x-2,arenaOrigin.x+combatOffset.x+1.35f);transform.position=bounded;
            // Reject half-turns from animation blending/reflected Euler decompositions.
            // Authored recoil is within +/-32 degrees; keep it without allowing a body flip.
            float tiltLimit=boss.CurrentPhase==SpiderBossController.Phase.Mutated?12:40;
            float wantedAngle=Mathf.Clamp(Mathf.DeltaAngle(0,animationRotation.eulerAngles.z),-tiltLimit,tiltLimit);
            if(!bodyAngleReady) {displayedBodyAngle=wantedAngle;bodyAngleReady=true;}
            displayedBodyAngle=Mathf.MoveTowardsAngle(displayedBodyAngle,wantedAngle,100*dt);
            transform.localRotation=Quaternion.Euler(0,0,displayedBodyAngle);
            transform.localScale=new Vector3(Mathf.Abs(animationScale.x),Mathf.Abs(animationScale.y),Mathf.Abs(animationScale.z));
        }
        // The left-loss idle was authored three units left of the normal arena.
        // Recenter that stance smoothly instead of letting it become permanent drift.
        float desiredRecenter=0;
        if(combatAnchored && boss && boss.CurrentPhase==SpiderBossController.Phase.ExposedLegs &&
            boss.leftArm && boss.leftArm.isDestroyed && !boss.IsPlayingArmLoss && !boss.IsReentering) {
            Vector3 origin=transform.parent?transform.parent.TransformPoint(authoredCombatOrigin):authoredCombatOrigin;
            desiredRecenter=Mathf.Clamp(origin.x+combatOffset.x-transform.position.x,0,3.1f);
        }
        if(fresh) oneArmRecenter=Mathf.SmoothDamp(oneArmRecenter,desiredRecenter,ref recenterVelocity,.3f,10,dt);
        if(!bodyOverride) transform.position+=Vector3.right*oneArmRecenter;
        if(combatAnchored && boss && !bodyOverride && !boss.IsReentering && !boss.IsCutsceneActive &&
            boss.CurrentPhase<SpiderBossController.Phase.Shell)transform.position+=Vector3.up*firstPhaseHeightLift;
        if(bodyOverride) { if(!mouthOverride)transform.position=bodyPosition; transform.rotation=bodyRotation; }
        // The authored acid clip translates the head independently of the chassis.
        // Preserve its tilt/jaw performance while keeping the neck attached, including blend-out.
        if(boss && (boss.IsPouringAcid || boss.IsHeadSlam))acidAttachmentUntil=Time.time+.3f;
        if(head && acidAttachmentUntil>Time.time)head.localPosition=restHeadLocal;
        if(mouthOverride)transform.position+=mouthPosition-MouthPosition;
        // Move the attached chassis, never detach or teleport the head transform.
        Vector3 wanted=headOverride && head && !bodyOverride?ClampHeadOffset(headPosition-head.position,ActiveDrop,ActiveLean):Vector3.zero;
        if(fresh) bodyLowering=ClampHeadOffset(Vector3.SmoothDamp(bodyLowering,wanted,ref loweringVelocity,.18f,boss && boss.CurrentPhase==SpiderBossController.Phase.Mutated?5:loweringSpeed,dt),ActiveDrop,ActiveLean);
        if(!bodyOverride) { transform.position+=bodyLowering; }
        if(!headOverride && bodyLowering.magnitude<.01f) plantedFeet=null;
        bool supportsActive=SupportsActive;
        if(bodyOverride && boss && boss.CurrentPhase==SpiderBossController.Phase.Escaping && escapeFeet==null) {
            escapeOrigin=transform.position;escapeFeet=new Vector3[limbs.Count];
            for(int i=0;i<limbs.Count;i++)if(limbs[i].tip)escapeFeet[i]=limbs[i].tip.position;
        }
        if(!supportsActive) {supportPlants.Clear();bodyPositionReady=false;bodyPositionVelocity=Vector3.zero;}
        else {
            // Blend the chassis before solving the planted feet, never blend the solved joints.
            Vector3 desiredPosition=transform.position;
            if(!bodyPositionReady){displayedBodyPosition=desiredPosition;bodyPositionReady=true;}
            if(fresh)displayedBodyPosition=Vector3.SmoothDamp(displayedBodyPosition,desiredPosition,ref bodyPositionVelocity,.14f,12,dt);
            transform.position=displayedBodyPosition;
        }
        foreach(var limb in limbs) {
            if(!limb.part || limb.part.isDestroyed || !limb.root || !limb.tip || !limb.target) continue;
            float weight=limb.targetSettings?Mathf.Max(limb.weight,limb.targetSettings.weight):limb.weight; Vector3 goal=limb.target.position;
            if(limb==striking) {
                float elapsed=Time.time-strikeTime;
                if(elapsed>strikeWindup+1.2f) { striking=null; }
                else {
                    float launch=Mathf.Clamp01((elapsed-(strikeWindup-.4f))/.4f);
                    Vector3 lifted=strikeStart+Vector3.up*Mathf.Sin(Mathf.Clamp01(elapsed/strikeWindup)*Mathf.PI*.5f)*.8f;
                    goal=Vector3.Lerp(lifted,strikeEnd,launch);
                    weight=Mathf.Clamp01(elapsed/.15f)*(1-Mathf.Clamp01((elapsed-strikeWindup-.2f)/1f));
                }
            }
            if(plantedFeet!=null && limbs.IndexOf(limb)<plantedFeet.Length && (headOverride || bodyLowering.magnitude>.01f)) {
                goal=plantedFeet[limbs.IndexOf(limb)]; weight=1;
            }
            bool supporting=supportsActive && IsSupport(limb) && limb!=striking;
            SupportPlant plant=null;
            if(supporting) {
                if(!supportPlants.TryGetValue(limb,out plant)) {
                    Vector2 upper=limb.joint?(Vector2)(limb.joint.position-limb.root.position):Vector2.right;
                    Vector2 lower=limb.joint?(Vector2)(limb.tip.position-limb.joint.position):Vector2.up;
                    float cross=upper.x*lower.y-upper.y*lower.x;
                    plant=new SupportPlant {tip=limb.tip.position,rootPosition=limb.root.localPosition,rootScale=limb.root.localScale,
                        jointPosition=limb.joint?limb.joint.localPosition:Vector3.zero,jointScale=limb.joint?limb.joint.localScale:Vector3.one,
                        bend=Mathf.Abs(cross)>.0001f?-Mathf.Sign(cross):limb.bendDirection};
                    supportPlants.Add(limb,plant);
                }
                goal=plant.tip;
                weight=1;
            }
            if(shellWeight>0 && head) {
                int index=limbs.IndexOf(limb);
                float angle=index*Mathf.PI*2/Mathf.Max(1,limbs.Count);
                goal=head.position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*1.1f; weight=shellWeight;
            }
            if(bodyOverride && boss && boss.CurrentPhase==SpiderBossController.Phase.Escaping) {
                int index=limbs.IndexOf(limb);
                float travel=Mathf.Abs(transform.position.x-escapeOrigin.x);
                float step=Mathf.SmoothStep(0,1,Mathf.Clamp01((travel-1-index%2*.6f)/2));
                Vector3 wallPlant=new Vector3(climbSurfaceSet?climbSurfaceX:transform.position.x+2,transform.position.y+(index%3-1)*1.4f,0);
                float climb=transform.position.y-escapeOrigin.y;
                if(climb>1){
                    float stride=climb/1.4f+index%2*.5f;float cycle=stride-Mathf.Floor(stride);
                    float swing=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,cycle));
                    wallPlant.y=escapeOrigin.y+(Mathf.Floor(stride)-index%2*.5f+swing)*1.4f+(index%3-1)*1.4f;
                    wallPlant.x-=Mathf.Sin(swing*Mathf.PI)*.35f;
                }
                goal=escapeFeet!=null && index<escapeFeet.Length?Vector3.Lerp(escapeFeet[index],wallPlant,step):wallPlant;weight=1;
            }
            bool authoredLoss=boss && boss.IsPlayingArmLoss;
            bool combatLimits=boss && !authoredLoss && !boss.IsCutsceneActive && !boss.IsReentering &&
                boss.CurrentPhase!=SpiderBossController.Phase.Shell && boss.CurrentPhase<SpiderBossController.Phase.Escaping;
            if(fresh) {
                limb.authoredRoot=limb.root.localRotation;
                limb.authoredJoint=limb.joint?limb.joint.localRotation:Quaternion.identity;
                limb.authoredPosition=limb.root.localPosition;
                limb.authoredScale=limb.root.localScale;
                limb.authoredJointPosition=limb.joint?limb.joint.localPosition:Vector3.zero;
                limb.authoredJointScale=limb.joint?limb.joint.localScale:Vector3.one;
                limb.restoreAuthored=supporting || !authoredLoss && (combatLimits || weight*proceduralWeight>.001f);
            }
            if(supporting) {
                // Animation may move the body, but cannot move a support's attachment or endpoint.
                limb.root.localPosition=plant.rootPosition;limb.root.localScale=plant.rootScale;
                if(limb.joint){limb.joint.localPosition=plant.jointPosition;limb.joint.localScale=plant.jointScale;}
                limb.bendDirection=plant.bend;
                SolveSupport(limb,goal);
            } else if(!authoredLoss) Solve(limb,goal,weight*proceduralWeight);
            if(combatLimits && !supporting) {
                limb.root.localRotation=ClampCombatRotation(limb.restRoot,limb.root.localRotation,65);
                if(limb.joint)limb.joint.localRotation=ClampCombatRotation(limb.restJoint,limb.joint.localRotation,80);
            }
            if(Application.isPlaying && !supporting && !authoredLoss && (combatLimits || weight*proceduralWeight>.001f) && limb.poseReady && fresh) {
                limb.root.localRotation=Quaternion.RotateTowards(limb.displayedRoot,limb.root.localRotation,Mathf.Min(180,poseAngularSpeed)*dt);
                if(limb.joint) limb.joint.localRotation=Quaternion.RotateTowards(limb.displayedJoint,limb.joint.localRotation,Mathf.Min(180,poseAngularSpeed)*dt);
                limb.root.localPosition=Vector3.Lerp(limb.displayedPosition,limb.root.localPosition,1-Mathf.Exp(-12*dt));
            }
            if(fresh) {
                limb.displayedRoot=limb.root.localRotation;
                limb.displayedJoint=limb.joint?limb.joint.localRotation:Quaternion.identity;
                limb.displayedPosition=limb.root.localPosition; limb.poseReady=true;
            }
        }
        if(upperFang) { upperFang.gameObject.SetActive(jawOpen>.01f); upperFang.localRotation=Quaternion.Euler(0,0,-jawOpen*30); }
        if(lowerFang) { lowerFang.gameObject.SetActive(jawOpen>.01f); lowerFang.localRotation=Quaternion.Euler(0,0,jawOpen*30); }
    }
    public static Quaternion ClampCombatRotation(Quaternion rest,Quaternion desired,float limit) {
        float angle=rest.eulerAngles.z+Mathf.Clamp(Mathf.DeltaAngle(rest.eulerAngles.z,desired.eulerAngles.z),-limit,limit);
        return Quaternion.Euler(0,0,angle);
    }
    public static void SolveSupport(SpiderIKLimb limb,Vector3 target) {
        float distance=Vector2.Distance(limb.root.position,target);
        if(limb.joint) {
            float upper=Vector2.Distance(limb.root.position,limb.joint.position),lower=Vector2.Distance(limb.joint.position,limb.tip.position);
            if(upper<.001f || lower<.001f)return;
            // Ease into telescoping before straight extension so the elbow never
            // hits a singularity and abruptly switches between bent and straight.
            float needed=Mathf.Abs(distance-upper)+.12f;
            float blend=Mathf.Clamp01(.5f+.5f*(lower-needed)/.2f);
            float neededLower=Mathf.Lerp(needed,lower,blend)+.2f*blend*(1-blend);
            neededLower=Mathf.Min(neededLower,upper+distance-.002f);
            limb.joint.localScale*=Mathf.Max(.001f,neededLower)/lower;
            lower=Vector2.Distance(limb.joint.position,limb.tip.position);
            Vector2 joint=SolveJoint(limb.root.position,target,upper,lower,limb.bendDirection);
            RotateToward(limb.root,limb.joint.position,joint);
            RotateToward(limb.joint,limb.tip.position,target);
        } else {
            float length=Vector2.Distance(limb.root.position,limb.tip.position);
            if(length<.001f || distance<.001f)return;
            limb.root.localScale*=distance/length;
            RotateToward(limb.root,limb.tip.position,target);
        }
    }
    void Solve(SpiderIKLimb limb,Vector3 target,float weight) {
        if(weight<=0) return;
        Vector2 goal=Vector2.Lerp(limb.tip.position,target,Mathf.Clamp01(weight));
        if(limb.joint) {
            float upper=Vector2.Distance(limb.root.position,limb.joint.position),lower=Vector2.Distance(limb.joint.position,limb.tip.position);
            if(upper<.001f || lower<.001f) return;
            float bend=limb.bendDirection;
            // Hold the authored elbow side, including when the chain reaches straight extension.
            Vector2 origin=limb.root.position;
            Vector2 delta=goal-origin;
            float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.0001f,upper+lower-.0001f);
            goal=origin+(delta.sqrMagnitude>.000001f?delta.normalized:Vector2.right)*reach;
            Vector2 joint=SolveJoint(limb.root.position,goal,upper,lower,bend);
            RotateToward(limb.root,limb.joint.position,joint);
            RotateToward(limb.joint,limb.tip.position,goal);
        } else RotateToward(limb.root,limb.tip.position,goal);
    }
    static void RotateToward(Transform pivot,Vector2 current,Vector2 target) {
        Vector2 a=current-(Vector2)pivot.position,b=target-(Vector2)pivot.position;
        if(a.sqrMagnitude<.000001f || b.sqrMagnitude<.000001f) return;
        float angle=Mathf.Atan2(a.x*b.y-a.y*b.x,Vector2.Dot(a,b))*Mathf.Rad2Deg;
        // A mirrored ancestor reverses local angular motion. Work in local planar
        // coordinates instead of assigning a world quaternion through a reflected rig.
        if(pivot.parent && pivot.parent.localToWorldMatrix.determinant<0) angle=-angle;
        pivot.localRotation=Quaternion.Euler(0,0,pivot.localEulerAngles.z+angle);
    }
    void BuildFangs() {
        if(!head || head.Find("IK bite fangs")) return;
        if(!fangSprite) {
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++) for(int x=0;x<32;x++) texture.SetPixel(x,y,Mathf.Abs(x-15.5f)<(31-y)*.3f?Color.white:Color.clear);
            texture.Apply(); fangSprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,1),32);
        }
        var container=new GameObject("IK bite fangs").transform; container.SetParent(head,false);
        var headSprite=head.GetComponent<SpriteRenderer>();
        float bottom=headSprite && headSprite.sprite?headSprite.sprite.bounds.min.y:-.6f;
        for(int i=0;i<2;i++) {
            var fang=new GameObject("Bite fang").transform; fang.SetParent(container,false); fang.localPosition=new Vector3(i==0?-.17f:.17f,bottom+.08f,0); fang.localScale=new Vector3(.17f,.3f,1);
            var sr=fang.gameObject.AddComponent<SpriteRenderer>(); sr.sprite=fangSprite; sr.color=new Color(.85f,1,.7f);
            if(headSprite) { sr.sortingLayerID=headSprite.sortingLayerID; sr.sortingOrder=headSprite.sortingOrder+5; sr.sharedMaterial=headSprite.sharedMaterial; }
            if(i==0) upperFang=fang; else lowerFang=fang; fang.gameObject.SetActive(false);
        }
    }
    void OnDrawGizmosSelected() {
        // These markers are the actual fixed world-space endpoints, not the
        // Animator-owned target transforms that travel with the spider.
        foreach(var plant in supportPlants.Values) {
            Gizmos.color=new Color(.35f,1,.6f);Gizmos.DrawWireSphere(plant.tip,.16f);
        }
        foreach(var limb in limbs) if(limb.root && limb.tip) {
            Gizmos.color=Color.cyan; if(limb.joint) { Gizmos.DrawLine(limb.root.position,limb.joint.position); Gizmos.DrawLine(limb.joint.position,limb.tip.position); } else Gizmos.DrawLine(limb.root.position,limb.tip.position);
            if(limb.target) { Gizmos.color=Color.yellow; Gizmos.DrawWireSphere(limb.target.position,.13f); }
        }
    }
}
