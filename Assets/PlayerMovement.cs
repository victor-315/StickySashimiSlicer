using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float acceleration = 10f;

    [Header("Jumping")]
    public float jumpForce = 7f;
    public int maxJumps = 2;

    private int jumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private bool isGrounded;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 🔒 Prevent physics rotation jitter
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        GroundCheck();
        HandleJump();
    }

    void FixedUpdate()
    {
        Move();
    }

    // ================= MOVEMENT =================

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(x, 0f, z).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = transform.TransformDirection(input) * targetSpeed;

        Vector3 velocity = Vector3.Lerp(
            new Vector3(rb.velocity.x, 0f, rb.velocity.z),
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
    }

    // ================= JUMP =================

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            Jump();
        }
    }

    void Jump()
    {
        // Reset Y velocity so jumps are consistent
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        jumpsRemaining--;
    }

    // ================= GROUND CHECK =================

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
    }
}