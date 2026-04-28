using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;

    public float acceleration = 12f;

    [Header("Jumping")]
    public float jumpForce = 7f;
    public int maxJumps = 2;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Rigidbody rb;

    private int jumpsRemaining;
    private bool isGrounded;

    private Vector3 moveDirection; // 🔥 KEY FIX

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        GroundCheck();
        HandleJump();

        CacheInputDirection(); // 🔥 IMPORTANT FIX
    }

    void FixedUpdate()
    {
        Move();
    }

    // ================= INPUT DIRECTION =================

    void CacheInputDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(x, 0f, z).normalized;

        // 🔥 Use CURRENT camera/player rotation instantly
        moveDirection = transform.TransformDirection(input);
    }

    // ================= MOVEMENT =================

    void Move()
    {
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float speed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = moveDirection * speed;

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
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpsRemaining--;
        }
    }

    // ================= GROUND =================

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            jumpsRemaining = maxJumps;
    }
}