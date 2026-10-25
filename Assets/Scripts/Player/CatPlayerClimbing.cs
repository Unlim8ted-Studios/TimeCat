using UnityEngine;
using System.Collections.Generic;

public partial class CatPlayerController {
    float wallDetachUntil,wallJumpControlUntil,wallHopUntil;
    const float WalkableNormalY=.57f;
    float lastWalkableGroundTime=float.NegativeInfinity;
    float slopeSupportUntil=float.NegativeInfinity;
    readonly ContactPoint2D[] traversalGroundContacts=new ContactPoint2D[32];
    int wallSide;
    Collider2D attachedWall;
    Vector2 mantleStart,mantleCorner,mantleEnd;
    float mantleElapsed,mantleDuration;
    bool hasMantleParameter,hasClimbSpeedParameter;
    readonly RaycastHit2D[] climbHits=new RaycastHit2D[32];
    readonly Collider2D[] climbOverlaps=new Collider2D[32];
    float ClimbSkin => bodyCollider?Mathf.Max(Physics2D.defaultContactOffset*2,Mathf.Clamp(bodyCollider.bounds.size.y*.035f,.008f,.045f)):.02f;
    ContactFilter2D ClimbFilter(bool wallsOnly=false){
        var filter=new ContactFilter2D();filter.SetLayerMask(wallsOnly && wallLayer.value!=0?wallLayer:groundLayer|wallLayer);filter.useTriggers=false;return filter;
    }
    public bool IsClingingToWall => isOnWall;
    ContactFilter2D TraversalCollisionFilter(){var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));filter.useTriggers=false;return filter;}
    bool TraversalObstacle(Collider2D collider){return !OwnCollider(collider) && !Physics2D.GetIgnoreCollision(bodyCollider,collider);}
    bool ClearTraversalSweep(Vector2 destination){
        Vector2 delta=destination-rb.position;float length=delta.magnitude;if(length<.00001f)return true;
        int count=bodyCollider.Cast(delta/length,TraversalCollisionFilter(),climbHits,length);
        for(int i=0;i<count;i++)if(TraversalObstacle(climbHits[i].collider) && Vector2.Dot(climbHits[i].normal,-delta/length)>.1f && climbHits[i].distance<length-.001f)return false;
        return true;
    }
    Vector2 SafeClimbVelocity(Vector2 velocity){
        float length=velocity.magnitude*Time.fixedDeltaTime;if(length<.00001f)return velocity;
        Vector2 direction=velocity.normalized;float allowed=length;
        int count=bodyCollider.Cast(direction,TraversalCollisionFilter(),climbHits,length+ClimbSkin);
        for(int i=0;i<count;i++)if(TraversalObstacle(climbHits[i].collider) && Vector2.Dot(climbHits[i].normal,-direction)>.1f)allowed=Mathf.Min(allowed,Mathf.Max(0,climbHits[i].distance-ClimbSkin));
        return velocity*(allowed/length);
    }
    bool OwnCollider(Collider2D collider){return !collider || collider.isTrigger || collider.attachedRigidbody==rb || collider.GetComponentInParent<CatPlayerController>()==this;}
    void CacheClimbAnimator(){
        if(!animator)return;
        foreach(var parameter in animator.parameters){if(parameter.name=="isMantling")hasMantleParameter=true;if(parameter.name=="climbAnimationSpeed")hasClimbSpeedParameter=true;}
    }
    void UpdateClimbAnimation(){
        if(hasMantleParameter)animator.SetBool("isMantling",isClimbingOver);
        if(hasClimbSpeedParameter)animator.SetFloat("climbAnimationSpeed",isClimbingOver?1:Mathf.Clamp(Mathf.Abs(rb.velocity.y)/Mathf.Max(.1f,climbSpeed),.15f,1.35f));
    }
    void CancelWallTraversal(){
        isOnWall=isClimbingWall=isClimbingOver=isTouchingWall=isLookingAwayFromWall=false;attachedWall=null;wallSide=0;mantleElapsed=0;wallHopUntil=0;
        if(rb)rb.gravityScale=gravityScale>0?gravityScale:2.11f;
        if(animator){animator.SetBool("isOnWall",false);animator.SetBool("isClimbingWall",false);if(hasMantleParameter)animator.SetBool("isMantling",false);}
    }
    void OnDisable(){CancelWallTraversal();physicsMoveX=physicsMoveY=0;physicsJump=false;}
    bool ProbeGround(){
        if(!bodyCollider)return false;
        // Uphill movement has positive world Y velocity. Test separation from the
        // surface normal instead, so walking uphill remains grounded.
        int contacts=bodyCollider.GetContacts(ClimbFilter(),traversalGroundContacts);
        for(int i=0;i<contacts;i++){
            var contact=traversalGroundContacts[i];
            if(contact.normal.y<WalkableNormalY || Vector2.Dot(rb.velocity,contact.normal)>.5f)continue;
            lastWalkableGroundTime=Time.time;
            if(Mathf.Abs(contact.normal.x)>.08f)slopeSupportUntil=Time.time+.15f;
            return true;
        }
        int count=bodyCollider.Cast(Vector2.down,ClimbFilter(),climbHits,ClimbSkin*2);
        for(int i=0;i<count;i++){
            var hit=climbHits[i];
            if(OwnCollider(hit.collider) || hit.normal.y<WalkableNormalY || Vector2.Dot(rb.velocity,hit.normal)>.5f)continue;
            // Ignore synthetic upward normals from casts starting inside a wall.
            if(hit.point.y>bodyCollider.bounds.min.y+ClimbSkin*2)continue;
            lastWalkableGroundTime=Time.time;
            if(Mathf.Abs(hit.normal.x)>.08f)slopeSupportUntil=Time.time+.15f;
            return true;
        }
        return false;
    }
    RaycastHit2D FindWall(Transform unusedProbe,Vector2 direction){
        if(!bodyCollider)return default;
        var bounds=bodyCollider.bounds;float reach=bounds.extents.x+ClimbSkin*2;
        RaycastHit2D nearest=default;
        for(int sample=0;sample<2;sample++){
            float height=sample*.22f;
            Vector2 origin=(Vector2)bounds.center+Vector2.up*(height*bounds.size.y);
            int count=gameObject.scene.GetPhysicsScene2D().Raycast(origin,direction,reach,ClimbFilter(true),wallResults);
            for(int i=0;i<count;i++){
                var hit=wallResults[i];if(OwnCollider(hit.collider) || Vector2.Dot(hit.normal,-direction)<.94f || Mathf.Abs(hit.normal.y)>.28f)continue;
                if(bodyCollider.Distance(hit.collider).distance>ClimbSkin*2.2f)continue;
                if(!nearest.collider || hit.distance<nearest.distance)nearest=hit;
            }
        }
        return nearest;
    }
    void FixedUpdate(){
        if(!rb || isFrozen || Time.timeScale<=0){physicsJump=false;return;}
        if(HandleAcidMovement())return;
        isGrounded=ProbeGround();
        if(isClimbingOver){StepMantle();physicsJump=false;return;}
        leftWallHit=FindWall(leftWallCheck,Vector2.left);rightWallHit=FindWall(rightWallCheck,Vector2.right);
        isTouchingWall=leftWallHit.collider || rightWallHit.collider;
        int side=rightWallHit.collider?1:leftWallHit.collider?-1:0;
        if(leftWallHit.collider && rightWallHit.collider)side=wallSide!=0?wallSide:physicsMoveX<0?-1:1;
        var hit=side>0?rightWallHit:leftWallHit;
        // Preserve contact across tile seams; upward input never needs sideways pressure.
        if(!hit.collider && isOnWall && attachedWall && attachedWall.enabled && bodyCollider.Distance(attachedWall).distance<=ClimbSkin*3){
            var point=attachedWall.ClosestPoint(bodyCollider.bounds.center);
            if(Mathf.Abs(point.x-bodyCollider.bounds.center.x)>bodyCollider.bounds.extents.x*.65f){side=wallSide;hit=new RaycastHit2D();}
        }
        bool retained=!hit.collider && side!=0 && isOnWall && attachedWall && attachedWall.enabled;
        Vector2 wallPoint=retained?attachedWall.ClosestPoint(bodyCollider.bounds.center):hit.point;
        bool away=side!=0 && physicsMoveX*side<-.2f;
        bool intent=Mathf.Abs(physicsMoveY)>.1f || (!isGrounded && physicsMoveX*side>.1f) || isOnWall;
        bool attached=(hit.collider || retained) && intent && Time.time>=wallDetachUntil && !(isGrounded && physicsMoveY<-.1f);
        isLookingAwayFromWall=isOnWall && away;
        // A grounded cat runs along slopes; climbing starts only with deliberate up input.
        bool walkingGround=Time.time-lastWalkableGroundTime<.12f && physicsMoveY<=.15f;
        bool walkingSlope=Time.time<slopeSupportUntil && wallHopUntil<=Time.time;
        // Brief tile seams and upright ramp edges cannot steal the walking state.
        // Reaching walkable ground also releases a stale wall grip.
        if(walkingSlope || (walkingGround && (!isOnWall || isGrounded)))attached=false;
        if(attached){
            wallSide=side;if(hit.collider)attachedWall=hit.collider;isOnWall=true;rb.rotation=0;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            isLookingAwayFromWall=away;
            if(physicsJump && hasJumpAbility){
                if(away){
                    // Push off only when explicitly looking away; preserve momentum long enough to clear the wall.
                    CancelWallTraversal();wallDetachUntil=Time.time+.24f;wallJumpControlUntil=Time.time+.22f;
                    rb.velocity=SafeClimbVelocity(new Vector2(-side*Mathf.Max(3.5f,wallJumpForceX),Mathf.Max(jumpForce,wallJumpForceY)));FlipIfNeeded(-side);
                }else{
                    // Jump upward along the wall, retaining the grip and its facing.
                    wallHopUntil=Time.time+.32f;rb.velocity=SafeClimbVelocity(new Vector2(0,Mathf.Max(jumpForce,wallJumpForceY)));
                }
                physicsJump=false;return;
            }
            var bounds=bodyCollider.bounds;
            float desiredCenter=wallPoint.x-side*(bounds.extents.x+Mathf.Max(ClimbSkin*.45f,Physics2D.defaultContactOffset*2));
            float correction=Mathf.Clamp((desiredCenter-bounds.center.x)/Time.fixedDeltaTime,-climbSpeed,climbSpeed);
            float vertical=Mathf.Abs(physicsMoveY)>.15f?physicsMoveY*climbSpeed:0;
            if(wallHopUntil>Time.time)vertical=Mathf.Max(vertical,Mathf.Max(jumpForce,wallJumpForceY)*Mathf.Clamp01((wallHopUntil-Time.time)/.32f));
            if(vertical>0 && CeilingAbove()){vertical=0;wallHopUntil=0;}
            if(vertical>0 && hit.collider && TryStartMantle(hit)){StepMantle();physicsJump=false;return;}
            rb.gravityScale=0;rb.velocity=SafeClimbVelocity(new Vector2(correction,vertical));
            isClimbingWall=Mathf.Abs(vertical)>.05f;
            // The authored climbing sprites face the viewer; use a consistent wall side.
            if(facingRight!=(side>0)){isOnWall=false;FlipIfNeeded(side);isOnWall=true;}
            spriteRenderer.flipX=side<0;
        }else{
            if(isOnWall){wallHopUntil=0;CancelWallTraversal();}
            isOnWall=isClimbingWall=false;rb.gravityScale=gravityScale;
            if(physicsMoveX>0 && !facingRight)Flip();else if(physicsMoveX<0 && facingRight)Flip();
            HandleMovement(physicsMoveX,physicsRunning,physicsJump);
        }
        physicsJump=false;
    }
    bool CeilingAbove(){
        Bounds bounds=bodyCollider.bounds;
        float head=spriteRenderer && spriteRenderer.sprite?Mathf.Max(bounds.max.y,spriteRenderer.bounds.max.y):bounds.max.y;
        Vector2 origin=new Vector2(bounds.center.x,head-ClimbSkin);
        int count=gameObject.scene.GetPhysicsScene2D().Raycast(origin,Vector2.up,Mathf.Max(ClimbSkin*2,climbSpeed*Time.fixedDeltaTime+ClimbSkin),ClimbFilter(),climbHits);
        for(int i=0;i<count;i++)if(!OwnCollider(climbHits[i].collider) && climbHits[i].normal.y<-.6f)return true;
        return false;
    }
    string occupiedBy;
    bool FreeAt(Vector2 root){
        Bounds bounds=bodyCollider.bounds;Vector2 center=root+((Vector2)bounds.center-rb.position);
        int count=gameObject.scene.GetPhysicsScene2D().OverlapBox(center,(Vector2)bounds.size*.999f,0,TraversalCollisionFilter(),climbOverlaps);
        for(int i=0;i<count;i++)if(TraversalObstacle(climbOverlaps[i])){occupiedBy=climbOverlaps[i].name+" bounds="+climbOverlaps[i].bounds+" center="+center+" size="+bounds.size;return false;}
        return true;
    }
    string lastLedgeCheck;
    bool TryStartMantle(RaycastHit2D wall){
        lastLedgeCheck="checking";
        Bounds bounds=bodyCollider.bounds;float height=bounds.size.y;
        float head=spriteRenderer && spriteRenderer.sprite?Mathf.Max(bounds.max.y,spriteRenderer.bounds.max.y):bounds.max.y;
        Vector2 sideOrigin=new Vector2(bounds.center.x,head-ClimbSkin);
        int continued=gameObject.scene.GetPhysicsScene2D().Raycast(sideOrigin,Vector2.right*wallSide,bounds.extents.x+ClimbSkin*3,ClimbFilter(true),climbHits);
        for(int i=0;i<continued;i++)if(!OwnCollider(climbHits[i].collider) && Mathf.Abs(climbHits[i].normal.x)>.6f){lastLedgeCheck="wall at head "+head;return false;}
        float overX=wall.point.x+wallSide*(bounds.extents.x+ClimbSkin*3);
        Vector2 origin=new Vector2(overX,head+height*.25f);
        int count=gameObject.scene.GetPhysicsScene2D().Raycast(origin,Vector2.down,height*1.75f,ClimbFilter(),climbHits);
        RaycastHit2D ledge=default;
        for(int i=0;i<count;i++){
            var candidate=climbHits[i];if(OwnCollider(candidate.collider) || candidate.normal.y<.7f || candidate.point.y<bounds.center.y-height*.2f || candidate.point.y>head+ClimbSkin)continue;
            if(!ledge.collider || candidate.distance<ledge.distance)ledge=candidate;
        }
        if(!ledge.collider){lastLedgeCheck="no ledge head="+head+" center="+bounds.center.y+" hits="+count;return false;}
        Vector2 offset=(Vector2)bounds.center-rb.position;
        mantleStart=rb.position;mantleEnd=new Vector2(overX-offset.x,ledge.point.y-bounds.min.y+rb.position.y+Mathf.Max(ClimbSkin,Physics2D.defaultContactOffset*3));
        mantleCorner=new Vector2(mantleStart.x,mantleEnd.y);
        if(!FreeAt(mantleCorner) || !FreeAt(mantleEnd)){lastLedgeCheck="occupied corner="+mantleCorner+" end="+mantleEnd+" object="+occupiedBy;return false;}
        // Test the whole lift and crossing, preventing mantles through overhangs.
        for(int i=1;i<=8;i++)if(!FreeAt(Vector2.Lerp(mantleStart,mantleCorner,i/8f)) || !FreeAt(Vector2.Lerp(mantleCorner,mantleEnd,i/8f))){lastLedgeCheck="blocked path "+i;return false;}
        mantleDuration=Mathf.Clamp(Vector2.Distance(mantleStart,mantleCorner)/Mathf.Max(.2f,climbSpeed),.28f,.65f)+.18f;
        mantleElapsed=0;isClimbingOver=true;isOnWall=true;isClimbingWall=false;rb.gravityScale=0;rb.velocity=Vector2.zero;return true;
    }
    void StepMantle(){
        if(!attachedWall || !attachedWall.enabled){CancelWallTraversal();return;}
        mantleElapsed+=Time.fixedDeltaTime;float phase=Mathf.Clamp01(mantleElapsed/mantleDuration);
        Vector2 next=phase<.7f?Vector2.Lerp(mantleStart,mantleCorner,Mathf.SmoothStep(0,1,phase/.7f)):Vector2.Lerp(mantleCorner,mantleEnd,Mathf.SmoothStep(0,1,(phase-.7f)/.3f));
        if(!FreeAt(next) || !ClearTraversalSweep(next)){CancelWallTraversal();wallDetachUntil=Time.time+.12f;return;}
        rb.gravityScale=0;rb.velocity=Vector2.zero;rb.MovePosition(next);
        if(phase>=1){CancelWallTraversal();rb.velocity=Vector2.zero;wallDetachUntil=Time.time+.15f;}
    }
}
