using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class CatPlayerController_Standing : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 30f;
    public float airControl = 0.6f;
    public float jumpForce = 13f;
    public float gravityScale = 3f;

    [Header("Dash & Slide")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 0.5f;
    public float slideSpeed = 14f;
    public float slideDuration = 0.4f;
    public float postDashSprintTime = 1.0f;

    [Header("Coyote & Buffer")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Input Keys")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.S;
    public KeyCode dashKey = KeyCode.LeftShift;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private bool facingRight = true;
    private bool isGrounded;
    private bool isDashing;
    private bool isSliding;
    private bool canDash = true;
    private bool sprintingAfterDash;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float sprintTimer;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        HandleInput();
        HandleFlip();
        HandleGroundCheck();
        HandleJumpBufferAndCoyote();

        if (Input.GetKeyDown(jumpKey))
            jumpBufferTimer = jumpBufferTime;

        if (jumpBufferTimer > 0 && coyoteTimer > 0 && !isSliding && !isDashing)
            Jump();

        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;

        if (Input.GetKeyDown(dashKey) && canDash && !isSliding)
        {
            if (isGrounded && Input.GetKey(downKey))
                StartCoroutine(SlideRoutine());
            else
                StartCoroutine(DashRoutine());
        }

        if (sprintingAfterDash)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0) sprintingAfterDash = false;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (isDashing || isSliding) return;

        float targetSpeed = moveInput * (sprintingAfterDash ? runSpeed : walkSpeed);
        float speedDiff = targetSpeed - rb.velocity.x;

        float accel = (Mathf.Abs(targetSpeed) > 0.01f)
            ? (isGrounded ? acceleration : acceleration * airControl)
            : deceleration;

        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accel, 0.9f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);

        float clampedX = Mathf.Clamp(rb.velocity.x, -runSpeed * 1.5f, runSpeed * 1.5f);
        rb.velocity = new Vector2(clampedX, rb.velocity.y);
    }

    void HandleInput()
    {
        moveInput = 0f;
        if (Input.GetKey(leftKey)) moveInput -= 1f;
        if (Input.GetKey(rightKey)) moveInput += 1f;
    }

    void HandleFlip()
    {
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) coyoteTimer = coyoteTime;
    }

    void HandleJumpBufferAndCoyote()
    {
        if (!isGrounded)
            coyoteTimer -= Time.deltaTime;
        else
            coyoteTimer = coyoteTime;
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        anim.SetTrigger("Jump");
        jumpBufferTimer = 0;
        coyoteTimer = 0;
    }

    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;
        anim.SetBool("isDashing", true);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        float dir = facingRight ? 1f : -1f;
        float t = 0f;

        while (t < dashDuration)
        {
            t += Time.deltaTime;
            rb.velocity = new Vector2(Mathf.Lerp(dashSpeed, runSpeed, t / dashDuration), 0f) * dir;
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        anim.SetBool("isDashing", false);
        sprintingAfterDash = true;
        sprintTimer = postDashSprintTime;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    IEnumerator SlideRoutine()
    {
        isSliding = true;
        anim.SetBool("isSliding", true);

        float dir = facingRight ? 1f : -1f;
        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;
            rb.velocity = new Vector2(dir * slideSpeed, rb.velocity.y);
            yield return null;
        }

        isSliding = false;
        anim.SetBool("isSliding", false);
    }

    void Flip()
    {
        facingRight = !facingRight;
        sr.flipX = !facingRight;
    }

    void UpdateAnimator()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isJumping", !isGrounded && rb.velocity.y > 0.1f);
        anim.SetBool("isFalling", !isGrounded && rb.velocity.y < -0.1f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
