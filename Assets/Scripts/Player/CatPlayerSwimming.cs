using UnityEngine;

public partial class CatPlayerController {
    float acidContactAt=float.NegativeInfinity,acidSurface;
    bool swimmingInAcid;
    public void ContactAcid(float surface){
        acidSurface=Time.time-acidContactAt<.001f?Mathf.Max(acidSurface,surface):surface;
        acidContactAt=Time.time;
    }
    bool HandleAcidMovement(){
        bool touching=bodyCollider && Time.time-acidContactAt<=Time.fixedDeltaTime*2.5f+.02f;
        float immersion=touching?Mathf.Clamp01((acidSurface-bodyCollider.bounds.min.y)/Mathf.Max(.05f,bodyCollider.bounds.size.y)):0;
        if(!touching || immersion<.08f){
            if(swimmingInAcid){swimmingInAcid=false;rb.gravityScale=gravityScale;}
            return false;
        }
        if(isOnWall || isClimbingOver)CancelWallTraversal();
        rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        swimmingInAcid=true;isGrounded=isOnWall=isClimbingWall=false;
        rb.gravityScale=Mathf.Max(.25f,gravityScale*.3f);
        float x=Mathf.MoveTowards(rb.velocity.x,physicsMoveX*walkSpeed*.55f,18*Time.fixedDeltaTime);
        float y=Mathf.Max(-2.2f,rb.velocity.y);
        // There is no automatic lift: jumping provides a swimming stroke, gravity sinks the cat.
        if(physicsJump && hasJumpAbility)y=Mathf.Clamp(jumpForce*.7f,3,6);
        rb.velocity=new Vector2(x,y);
        if(physicsMoveX>0 && !facingRight)Flip();else if(physicsMoveX<0 && facingRight)Flip();
        physicsJump=false;return true;
    }
}
