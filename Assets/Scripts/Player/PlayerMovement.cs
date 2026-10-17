using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform camera;
    public PlayerInputs inputs;

    [Header("Movement")]
    public float movementSpeed;

    [Header("Movement/jump")]
    public float jumpHeight = 20f;
    public Transform groundCheck;
    public LayerMask jumpMask;

    [Header("Movement/Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Debug Inputs")]
    public Vector2 moveDirection;
    public bool dashPressed;
    public bool jumpPressed;

    bool isJumping;
    bool dashQueued;
    bool isDashing;
    float dashTimeLeft;
    float cooldownTimeLeft;
    Vector3 dashDir;

    void Start()
    {
        inputs = new();
        inputs.Enable();
    }

    void Update()
    {
        moveDirection = inputs.Movement.WASD.ReadValue<Vector2>();

        RaycastHit hit;

        if (inputs.Movement.Jump.WasPressedThisFrame())
            jumpPressed = true;

        if (inputs.Movement.Dash.WasPressedThisFrame())
            dashQueued = true;
    }

    void FixedUpdate()
    {
        Vector3 forward = camera.forward;
        Vector3 right = camera.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 inputDir = forward * moveDirection.y + right * moveDirection.x;

        // cooldown
        if (cooldownTimeLeft > 0f)
            cooldownTimeLeft -= Time.fixedDeltaTime;

        if (IsGrounded() && jumpPressed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpHeight, rb.linearVelocity.z);
            isJumping = true;
        }
        else
        {
            isJumping = false;
        }
        jumpPressed = false;

        //dash
        if (dashQueued && !isDashing && cooldownTimeLeft <= 0f)
        {
            dashDir = inputDir.sqrMagnitude > 0.01f ? inputDir.normalized : forward;
            isDashing = true;
            dashTimeLeft = dashDuration;
            cooldownTimeLeft = dashCooldown;
        }
        dashQueued = false;

        // Dash override
        if (isDashing)
        {
            if (isJumping)
            {
                rb.linearVelocity = new Vector3(
                    dashDir.x * dashSpeed * 2.5f,
                    rb.linearVelocity.y,
                    dashDir.z * dashSpeed * 2.5f
                );
            }
            else
            {
                rb.linearVelocity = new Vector3(
                    dashDir.x * dashSpeed,
                    rb.linearVelocity.y,
                    dashDir.z * dashSpeed
                );
            }

            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0f)
                isDashing = false;

            return;
        }

        // movement
        if (IsGrounded())
        {
            Vector3 move = inputDir * movementSpeed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(
            groundCheck.position,
            0.25f,
            jumpMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, 0.25f);
    }
}
