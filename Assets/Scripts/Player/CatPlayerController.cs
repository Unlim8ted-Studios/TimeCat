using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public partial class CatPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float slowedSpeed = 2.3f;
    public float walkSpeed = 6.57f;
    private float origrunSpeed = 15.97f;
    private float oriwalkSpeed = 6.57f;
    public float runSpeed = 15.97f;
    [Range(.01f,1f)] public float groundFriction = .7f;
    public float jumpForce = 7.63f;
    public float wallJumpForceX = 6f;
    public float wallJumpForceY = 6f;
    public float airAcceleration = 5.56f;
    public float maxAirSpeed = 12f;
    public float gravityScale = 2.11f;
    public bool hasJumpAbility = true;
    public bool canRun = true;

    [Header("Idle Sit Logic")]
    public float idleToSitDelay = 2.95f;
    public bool preventSitting = false;

    [Header("Controls")]
    public KeyCode upKey = KeyCode.W; // Key to press for climbing up
    public KeyCode downKey = KeyCode.S; // Key to press for climbing down
    public KeyCode leftKey = KeyCode.A; // Key to press for climbing left
    public KeyCode rightKey = KeyCode.D; // Key to press for climbing right
    public KeyCode JumpKey = KeyCode.Space; // Key to press for jumping
    public KeyCode timeTravelKey = KeyCode.F; // Key to press for time travel
    public KeyCode AttackKey = KeyCode.Mouse0; // Key to press for attacking
    public KeyCode SprintKey = KeyCode.LeftShift; // Key to press for sprinting
    public KeyCode InteractKey = KeyCode.E; // Key to press for interacting with boxes


    [Header("Climbing")]
    public float climbSpeed = 2f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public Transform leftWallCheck;
    public Transform rightWallCheck;
    public float wallCheckDistance = 1.3f;
    public Transform topCheck;
    public float topCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Time Travel & Box")]
    public bool isPresent = true;
    public CatPlayerController otherPlayer;
    public float holdDistance = 1.2f;
    public float boxDetectRange = 1.5f;
    public float maxHoldDistance = 2.0f;
   // public LayerMask boxBlockingLayer;
    public TextMesh interactTextUI;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private bool facingRight = true;
    public bool FacingRight => facingRight;
    public int LastAttackFrame { get; private set; } = -1;
    private float idleTimer = 29.95f;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isClimbingWall;
    private bool isOnWall;
    private bool isLookingAwayFromWall;
    private bool isClimbingOver;

    private GameObject heldBox;
    private GameObject boxNearby;
    private bool canPickUp = false;
    private bool isFrozen = false;
    public bool IsFrozen => isFrozen;

    private RaycastHit2D leftWallHit;
    private RaycastHit2D rightWallHit;

    [Header("Flashlight")]
    public GameObject flashlight;         // assign in Inspector
    public Vector2 flashlightOffset = new Vector2(0.6f, 0.1f); // offset relative to body
    private bool hasFlashlight = false;

    [Header("Mobile (optional)")]
    public float sprintStickThreshold = 0.8f;  // sprint if stick magnitude >= this
    private bool useMobile = false;

    // one-frame edge flags set by UI buttons
    private bool mJumpPressed, mInteractPressed, mTimePressed;
    [Header("Mobile UI")]
    [SerializeField] private GameObject mobileUIPrefab;  // prefab with FixedJoystick + buttons as children
    private FixedJoystick mobileJoy;
    private Button jumpBtn;
    private Button interactBtn;
    private Button timeBtn;

    float physicsMoveX, physicsMoveY;
    bool physicsRunning, physicsJump;
    Collider2D bodyCollider;
    readonly RaycastHit2D[] wallResults=new RaycastHit2D[32];
    readonly Collider2D[] groundResults=new Collider2D[32];

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        CacheClimbAnimator();
        // Old prefab instances omitted these serialized fields and can load them as zero.
        if(groundFriction<=0) groundFriction=.7f;
        if(gravityScale<=0) gravityScale=rb.gravityScale>0?rb.gravityScale:2.11f;
        idleTimer = idleToSitDelay;

        if (interactTextUI != null)
            interactTextUI.gameObject.SetActive(false);

        #if UNITY_ANDROID || UNITY_IOS
            tryBindMobile();
        #endif

        if (flashlight != null)
        {
            hasFlashlight = true;
            flashlight.transform.SetParent(transform);
            flashlight.transform.localPosition = flashlightOffset;
        }

        Debug.Log("Is using mobile:" + useMobile);

    }

    void Update()
    {
        if (isFrozen) return;

#if UNITY_ANDROID || UNITY_IOS
            if (!useMobile){
                tryBindMobile();
            } 
#endif

        float moveX = 0f, moveY = 0f;
        bool webTouch=Application.platform==RuntimePlatform.WebGLPlayer && TimeCatWebBridge.TouchMode;
        bool isAttacking = TimeCatWebBridge.ConsumeAttack() || (Input.GetKeyDown(AttackKey) && !IsPointerOverUIElement());
        if (isAttacking && Time.timeScale > 0) LastAttackFrame = Time.frameCount;

        if(webTouch){moveX=TimeCatWebBridge.Movement.x;moveY=TimeCatWebBridge.Movement.y;}
        else if (useMobile && mobileJoy)
        {
            Vector2 dir = mobileJoy.Direction;

            // Deadzone clamp (optional)
            if (Mathf.Abs(dir.x) < 0.1f) dir.x = 0;
            if (Mathf.Abs(dir.y) < 0.1f) dir.y = 0;

            moveX = dir.x;  // left/right
            moveY = dir.y;  // up/down
        }
        else
        {
            if (Input.GetKey(leftKey)) moveX -= 1f;
            if (Input.GetKey(rightKey)) moveX += 1f;
            if (Input.GetKey(upKey)) moveY += 1f;
            if (Input.GetKey(downKey)) moveY -= 1f;
        }


        // sprint logic: keyboard = Shift; mobile = stick past threshold
        bool isRunning = canRun && (
            (webTouch && TimeCatWebBridge.Movement.magnitude >= sprintStickThreshold) ||
            (useMobile && mobileJoy && mobileJoy.Direction.magnitude >= sprintStickThreshold) ||
            (!useMobile && Input.GetKey(SprintKey))
        );

        // edge-triggered buttons (jump / interact / time)
        bool jumpPressed = TimeCatWebBridge.ConsumeJump() || (useMobile ? mJumpPressed : Input.GetKeyDown(JumpKey));
        bool interactPressed = TimeCatWebBridge.ConsumeInteract() || (useMobile ? mInteractPressed : Input.GetKeyDown(InteractKey));
        bool timePressed = useMobile ? mTimePressed : Input.GetKeyDown(timeTravelKey);

        physicsMoveX=moveX; physicsMoveY=moveY; physicsRunning=isRunning;
        physicsJump |= jumpPressed;
        HandleIdle(moveX, isAttacking);

        // Raycast to detect box if not holding one
        if (heldBox == null)
        {
            Vector2 rayOrigin = transform.position;
            Vector2 rayDir = facingRight ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDir, boxDetectRange, LayerMask.GetMask("Ground"));
            if (hit.collider != null && (hit.collider.CompareTag("Box") || hit.collider.CompareTag("TimeBox")))
            {
                canPickUp = true;
                boxNearby = hit.collider.gameObject;

                if (interactTextUI != null)
                    interactTextUI.gameObject.SetActive(true);
            }
            else
            {
                canPickUp = false;
                boxNearby = null;

                if (interactTextUI != null)
                    interactTextUI.gameObject.SetActive(false);
            }
        }

        // E toggles pickup/drop
        if (interactPressed)
        {
            if (heldBox != null)
                DropBox();
            else if (canPickUp && boxNearby != null)
                PickUpBox();

        }

        // Move held box with safety checks

        if (heldBox != null)
        {
            // 1) Decide target position
            Vector2 rawTargetPos;
            if (useMobile || webTouch)
            {
                // Just put the box in front of the player on mobile
                Vector2 forward = facingRight ? Vector2.right : Vector2.left;
                rawTargetPos = (Vector2)transform.position + forward * holdDistance + Vector2.up * 0.1f; // slight lift to avoid ground snag
            }
            else
            {
                // Original mouse aim for desktop
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;
                rawTargetPos = (Vector2)transform.position + dir * holdDistance;
        
                float dist = Vector2.Distance(transform.position, rawTargetPos);
                if (dist > maxHoldDistance || rawTargetPos.y < transform.position.y - 0.2f)
                {
                    DropBox();
                    return;
                }
            }
        
            // 2) Safety & movement (shared)
            BoxCollider2D boxCol = heldBox.GetComponent<BoxCollider2D>();
            Rigidbody2D  boxRb   = heldBox.GetComponent<Rigidbody2D>();
            Vector2 boxSize = boxCol.size * heldBox.transform.localScale;
        
            Collider2D[] hits = Physics2D.OverlapBoxAll(rawTargetPos, boxSize, 0f, 0);
            bool blocked = false;
            bool blockedUnderPlayer = false;
        
            foreach (var hit in hits)
            {
                if (hit.gameObject != heldBox)
                {
                    blocked = true;
                    if (hit.gameObject == gameObject && rawTargetPos.y < transform.position.y)
                    {
                        blockedUnderPlayer = true;
                        break;
                    }
                }
            }
        
            if (blockedUnderPlayer)
            {
                DropBox();
                return;
            }
        
            Vector2 targetPos;
            if (blocked)
            {
                // Push back slightly if obstructed
                Vector2 pushDir = (Vector2)transform.position - rawTargetPos;
                targetPos = (Vector2)transform.position + pushDir.normalized * (holdDistance * 0.6f);
            }
            else
            {
                targetPos = rawTargetPos;
            }
        
            // 3) Move via velocity for stability
            Vector2 velocity = (targetPos - (Vector2)heldBox.transform.position) * 20f;
            boxRb.velocity = velocity;
            boxRb.angularVelocity = 0f;
        
            // 4) Sync TimeBox
            if (heldBox.CompareTag("TimeBox") && otherPlayer != null)
            {
                otherPlayer.ReplaceBoxInTime(heldBox.name, heldBox.transform.position);
            }
        }

        // Animator
        if (Time.timeScale > 0)
        {
            animator.SetFloat("moveX", moveX);
            animator.SetBool("isRunning", isRunning);
            animator.SetBool("isAttacking", isAttacking);
            animator.SetBool("isSitting", idleTimer >= idleToSitDelay && !preventSitting);
            animator.SetBool("facingRight", facingRight);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isClimbingWall", isClimbingWall);
            animator.SetBool("isOnWall", isOnWall);
            animator.SetBool("isLookingAwayFromWall", isLookingAwayFromWall);
            UpdateClimbAnimation();
        }
        
        mJumpPressed = false;
        mInteractPressed = false;
        mTimePressed = false; // reset edge buttons
    }

    private void tryBindMobile()
    {
        GameObject jObj       = GameObject.Find("Fixed Joystick");
        GameObject jumpObj    = GameObject.Find("jump");
        GameObject interactObj= GameObject.Find("interact");
        GameObject timeObj    = GameObject.Find("time");

        if (jObj && jumpObj && interactObj && timeObj &&
            jObj.CompareTag("Mobile") && jumpObj.CompareTag("Mobile") &&
            interactObj.CompareTag("Mobile") && timeObj.CompareTag("Mobile"))
        {
            mobileJoy   = jObj.GetComponent<FixedJoystick>();
            jumpBtn     = jumpObj.GetComponent<Button>();
            interactBtn = interactObj.GetComponent<Button>();
            timeBtn     = timeObj.GetComponent<Button>();

            if (mobileJoy && jumpBtn && interactBtn && timeBtn)
            {
                useMobile = true;
                jumpBtn.onClick.AddListener(() => mJumpPressed = true);
                interactBtn.onClick.AddListener(() => mInteractPressed = true);
                timeBtn.onClick.AddListener(() => mTimePressed = true);
            }
        }
    }

    void HandleMovement(float moveX, bool isRunning, bool jumpPressed)
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckRadius + 0.2f, groundLayer | wallLayer);
        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;
            float slopeAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            // 90f offset so the cat stays upright relative to slope normal
            Quaternion targetRot = Quaternion.Euler(0, 0, slopeAngle - 90f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);

        }

        if (!isGrounded || isOnWall)transform.rotation = Quaternion.identity;

        if(!isClimbingOver && !isClimbingWall && wallJumpControlUntil<=Time.time) {
            float speed=isRunning?runSpeed:walkSpeed;
            rb.velocity=new Vector2(HorizontalVelocity(rb.velocity.x,moveX,speed,isGrounded,
                groundFriction,airAcceleration,maxAirSpeed,Time.fixedDeltaTime),rb.velocity.y);
        }

        if (jumpPressed && isGrounded && hasJumpAbility)
        {
            if (rb.constraints == (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation))
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    public static float HorizontalVelocity(float current,float input,float speed,bool grounded,
        float friction,float acceleration,float airLimit,float deltaTime) {
        if(grounded) return Mathf.Lerp(current,input*speed,friction>0?friction:.7f);
        return Mathf.MoveTowards(current,input*airLimit,acceleration*deltaTime);
    }

    float environmentalHealth=100;
    bool environmentalDeath;
    public void TakeEnvironmentalDamage(float amount) {
        if(isFrozen || environmentalDeath || amount<=0)return;
        var boss=FindObjectOfType<SpiderBossController>();
        if(boss && boss.ApplyGooDamage(this,amount))return;
        // Boss cutscenes must not fall through into a separate health pool.
        if(boss && boss.player==transform)return;
        environmentalHealth=Mathf.Max(0,environmentalHealth-amount);
        PlayerHurtFeedback.Notify(this,environmentalHealth,100,amount,true);
        if(environmentalHealth<=0) {environmentalDeath=true;SceneManager.LoadScene(gameObject.scene.buildIndex);}
    }

    public void resetJumpFlag(){
    // Prevent buffered jump from carrying over into the other timeline
    mJumpPressed = false;
    }
    void HandleIdle(float moveX, bool isAttacking)
    {
        if (Mathf.Abs(moveX) < 0.01f && !isAttacking && isGrounded && rb.velocity.y == 0 && !isClimbingWall)
            idleTimer += Time.deltaTime;
        else
            idleTimer = 0f;
    }

    void Flip()
    {
        if (Time.timeScale > 0)
        {

            if (!isOnWall && !isClimbingOver)
            {
                facingRight = !facingRight;
                spriteRenderer.flipX = !facingRight;
            }
            // also flip flashlight side and rotation
            if (hasFlashlight && flashlight != null)
            {
                Vector2 offset = flashlightOffset;
                offset.x = facingRight ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
                flashlight.transform.localPosition = offset;
                Vector3 euler = flashlight.transform.localEulerAngles;
                euler.z = (euler.z + 180f) % 360f;  // subtract 180 on Z
                flashlight.transform.localEulerAngles = euler;
            }
        }
    }

    void FlipIfNeeded(float direction)
    {
        if (Time.timeScale > 0)
        {
            if (direction > 0 && !facingRight) Flip();
            else if (direction < 0 && facingRight) Flip();
        }
    }
    void PickUpBox()
    {
        if (boxNearby == null) return;
        heldBox = boxNearby;

        // Disable collision between box and player while held
        Physics2D.IgnoreCollision(heldBox.GetComponent<Collider2D>(), GetComponent<Collider2D>(), true);

        Rigidbody2D boxRB = heldBox.GetComponent<Rigidbody2D>();
        if (boxRB != null)
        {
            // boxRB.isKinematic = true;
            boxRB.velocity = Vector2.zero;
        }

        if (!isPresent && heldBox.CompareTag("TimeBox") && otherPlayer != null)
        {
            otherPlayer.ReplaceBoxInTime(heldBox.name, heldBox.transform.position);
        }

        if (interactTextUI != null)
        {
            interactTextUI.gameObject.SetActive(false);
        }

        boxNearby = null;
        canPickUp = false;
    }
    public GameObject GetHeldBox()
    {
        return heldBox;
    }

    public void ForceDropBoxSilently()
    {
        if (heldBox == null) return;

        Rigidbody2D rb = heldBox.GetComponent<Rigidbody2D>();
        if (rb != null) rb.isKinematic = false;

        heldBox = null;
    }

    public void DestroyBoxWithName(string boxName)
    {
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("TimeBox");
        foreach (GameObject b in boxes)
        {
            if (b.name == boxName)
            {
                Destroy(b);
                return;
            }
        }
    }

    public void ReceiveBoxFromTime(GameObject newBox)
    {
        Rigidbody2D rb = newBox.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector2.zero;
        }
        heldBox = newBox;
    }

    public void DropBox()
    {
        if (heldBox == null) return;
        Collider2D col = heldBox.GetComponent<Collider2D>();
        if (col) col.enabled = true;
        Physics2D.IgnoreCollision(col, GetComponent<Collider2D>(), false); // re-enable collision

        Rigidbody2D boxRB = heldBox.GetComponent<Rigidbody2D>();

        heldBox = null;
    }

    public void ReplaceBoxInTime(string boxName, Vector2 pos)
    {
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("TimeBox");
        foreach (GameObject b in boxes)
        {
            if (b.name == boxName)
            {
                b.transform.position = pos;
                Collider2D col = b.GetComponent<Collider2D>();
                if (col != null) col.enabled = true; // re-enable collider
                return;
            }
        }
    }

    public void giveJumpAbility()
    {
        hasJumpAbility = true;
    }
    public void removeJumpAbility()
    {
        hasJumpAbility = false;
    }
    public void giveSprintAbility()
    {
        canRun = true;
    }
    public void removeSprintAbility()
    {
        canRun = false;
    }
    public void makeSlow()
    {
        origrunSpeed = runSpeed;
        oriwalkSpeed = walkSpeed;
        runSpeed = 4;
        walkSpeed = 4;
        canRun = false;
    }

    public void makenotSlow()
    {
        runSpeed = origrunSpeed;
        walkSpeed = oriwalkSpeed;
        canRun = true;
    }

    public void sit()
    {
        CancelWallTraversal();
        isFrozen = true;
        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        animator.SetBool("isSitting", true);
        animator.SetTrigger("Sit");
    }
    public void stand()
    {
        isFrozen = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        animator.SetBool("isSitting", false);
    }
    public IEnumerator PlayAnimationAndFreeze(string animationTriggerName)
    {
        CancelWallTraversal();
        isFrozen = true;

        // Freeze Rigidbody2D movement
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Play the animation
        animator.SetTrigger(animationTriggerName);

        // Wait for animation to finish
        // STEP 1: Wait until the animation state actually starts playing
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsTag(animationTriggerName) || info.IsName(animationTriggerName);
        });

        // STEP 2: Wait until the animation finishes (normalizedTime >= 1)
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.normalizedTime >= 1f && !animator.IsInTransition(0);
        });


        // Unfreeze Rigidbody2D
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        isFrozen = false;
    }

    public void FreezeAndPlayAnimation(string animationTriggerName)
    {
        StartCoroutine(PlayAnimationAndFreeze(animationTriggerName));
    }

    public void Freeze()
    {
        CancelWallTraversal();
        isFrozen = true;
        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    public void FreezeMoveX(){
        animator.SetFloat("moveX", 0);

    }
    public void unFreeze()
    {
        isFrozen = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }


    public void unfreezeRB()
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void Jump(float jumpforce)
    {
        rb.velocity = new Vector2(0, jumpforce);
    }

    private bool IsPointerOverUIElement()
    {
        if(!EventSystem.current) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // true if *any* hit is on the UI layer
        foreach (var r in results)
        {
            if (r.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }
        return false;
    }


    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (topCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(topCheck.position, topCheckRadius);
        }

        if (leftWallCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftWallCheck.position, leftWallCheck.position + Vector3.left * wallCheckDistance);
        }

        if (rightWallCheck != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(rightWallCheck.position, rightWallCheck.position + Vector3.right * wallCheckDistance);
        }

        Gizmos.color = Color.red;
        Vector3 direction = Application.isPlaying ? (facingRight ? Vector3.right : Vector3.left) : Vector3.right;
        Gizmos.DrawWireSphere(transform.position, boxDetectRange);
    }

}
