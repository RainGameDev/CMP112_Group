using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [BoxGroup("References")]
    public Rigidbody rb;

    [BoxGroup("References")]
    public Transform camera;

    [BoxGroup("References")]
    public PlayerInputs inputs;

    [BoxGroup("Movement")]
    public float movementSpeed;

    [BoxGroup("Movement/jump")]
    public float jumpHeight = 20f;

    [BoxGroup("Movement/jump")]
    public Transform groundCheck;

    [BoxGroup("Movement/jump")]
    public LayerMask jumpMask;

    [BoxGroup("Movement/Dash")]
    public float dashSpeed = 20f;

    [BoxGroup("Movement/Dash")]
    public float dashDuration = 0.2f;

    [BoxGroup("Movement/Dash")]
    public float dashCooldown = 1f;

    [BoxGroup("Rotation")]
    public Transform rotationObject;

    [BoxGroup("Rotation")]
    public float rotationSpeed = 720f;

    [BoxGroup("Debug Inputs")]
    public Vector2 moveDirection;

    [BoxGroup("Debug Inputs")]
    public bool dashPressed;

    [BoxGroup("Debug Inputs")]
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

        Rotate();
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
        Vector3 move = inputDir * movementSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
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

    void Rotate()
    {
        Vector3 forward = camera.forward;
        Vector3 right = camera.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 inputDir = forward * moveDirection.y + right * moveDirection.x;

        Vector3 lookDir = isDashing ? dashDir : inputDir;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            rotationObject.rotation = Quaternion.RotateTowards(
                rotationObject.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
