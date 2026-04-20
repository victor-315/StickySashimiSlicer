using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpForce = 7f;

    public int maxJumps = 2;

    private int jumpsRemaining;
    private Rigidbody rb;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private bool isGrounded;

    // 🔋 Sprint Meter
    public float maxStamina = 5f;
    public float stamina;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 1.5f;

    private bool isSprinting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        jumpsRemaining = maxJumps;
        stamina = maxStamina;
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }

        // Sprint logic
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);
        isSprinting = sprintInput && stamina > 0f && isGrounded;

        if (isSprinting)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            stamina += staminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // Jump
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        float speed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        Vector3 velocity = move * speed;

        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsRemaining--;
    }
}