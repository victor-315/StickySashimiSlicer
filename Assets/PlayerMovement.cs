using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpForce = 7f;

    private Rigidbody rb;

    [Header("Jumping")]
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Stamina")]
    public float maxStamina = 5f;
    public float stamina;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 1.5f;

    [Header("Exhaustion")]
    public float exhaustLockTime = 1f;
    public float slowRegenMultiplier = 0.3f;

    [Header("Stop Regen Delay")]
    public float stopRegenDelay = 0.3f;

    private float exhaustTimer;
    private bool regenLocked;

    private float stopTimer;
    private bool wasMoving;

    public Image fillImage;

    private bool isSprinting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        stamina = maxStamina;
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            jumpsRemaining = maxJumps;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, z);
        bool isMoving = input.magnitude > 0.1f;
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);

        // 🛑 Detect STOP (only for idle delay)
        if (wasMoving && !isMoving)
        {
            stopTimer = stopRegenDelay;
        }

        wasMoving = isMoving;

        if (stopTimer > 0f)
            stopTimer -= Time.deltaTime;

        // 🔴 Exhaustion trigger
        if (stamina <= 0f && !regenLocked)
        {
            stamina = 0f;
            regenLocked = true;
            exhaustTimer = exhaustLockTime;
        }

        // ⏳ Exhaustion timer
        if (regenLocked)
        {
            exhaustTimer -= Time.deltaTime;

            if (exhaustTimer <= 0f)
                regenLocked = false;
        }

        // 🏃 Sprint logic
        bool canSprint = !regenLocked && stamina > 0.1f;
        isSprinting = canSprint && sprintInput && isMoving;

        // 🔋 STAMINA SYSTEM
        if (isSprinting)
        {
            // 🔴 Drain
            stamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            // 🔋 Regen allowed while moving OR idle (after delay)
            bool canRegen = false;

            if (isMoving)
            {
                // ✅ moving but not sprinting → regen immediately
                canRegen = true;
            }
            else if (stopTimer <= 0f)
            {
                // 🛑 idle → regen after delay
                canRegen = true;
            }

            if (canRegen && !regenLocked && stamina < maxStamina)
            {
                if (stamina <= maxStamina * 0.2f)
                {
                    // 🟡 slow regen
                    stamina += staminaRegenRate * slowRegenMultiplier * Time.deltaTime;
                }
                else
                {
                    // 🟢 normal regen
                    stamina += staminaRegenRate * Time.deltaTime;
                }
            }
        }

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        // UI
        if (fillImage != null)
            fillImage.fillAmount = stamina / maxStamina;

        // Jump
        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
            Jump();
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(x, 0f, z).normalized;

        float speed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 move = transform.TransformDirection(input) * speed;

        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsRemaining--;
    }
}