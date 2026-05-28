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

    [Header("Animation")]
    public Animator animator;

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

        animator = GetComponentInChildren<Animator>();

        stamina = maxStamina;
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            jumpsRemaining = maxJumps;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, z);

        bool isMoving = input.magnitude > 0.1f;
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);

        if (wasMoving && !isMoving)
            stopTimer = stopRegenDelay;

        wasMoving = isMoving;

        if (stopTimer > 0f)
            stopTimer -= Time.deltaTime;

        if (stamina <= 0f && !regenLocked)
        {
            stamina = 0f;
            regenLocked = true;
            exhaustTimer = exhaustLockTime;
        }

        if (regenLocked)
        {
            exhaustTimer -= Time.deltaTime;

            if (exhaustTimer <= 0f)
                regenLocked = false;
        }

        bool canSprint = !regenLocked && stamina > 0.1f;

        isSprinting = canSprint && sprintInput && isMoving;

        // ✅ FIXED SPEED (this is the important part)
        if (animator != null)
        {
            float speedValue = input.magnitude; // 0–1 real movement value
            animator.SetFloat("speed", speedValue);
            animator.SetBool("sprint", isSprinting);
        }

        if (isSprinting)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            bool canRegen = false;

            if (isMoving)
                canRegen = true;
            else if (stopTimer <= 0f)
                canRegen = true;

            if (canRegen && !regenLocked && stamina < maxStamina)
            {
                if (stamina <= maxStamina * 0.2f)
                {
                    stamina += staminaRegenRate * slowRegenMultiplier * Time.deltaTime;
                }
                else
                {
                    stamina += staminaRegenRate * Time.deltaTime;
                }
            }
        }

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);

        if (fillImage != null)
            fillImage.fillAmount = stamina / maxStamina;

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