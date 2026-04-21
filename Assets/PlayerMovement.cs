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
    public float exhaustLockTime = 1f;          // 🔴 hard 0-lock duration
    public float slowRegenMultiplier = 0.3f;     // 🟡 slow regen after lock

    private float exhaustTimer;
    private bool regenLocked;

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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            jumpsRemaining = maxJumps;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, z);
        bool isMoving = input.magnitude > 0.1f;
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);

        // 🔴 Trigger exhaustion lock when stamina hits 0
        if (stamina <= 0f && !regenLocked)
        {
            regenLocked = true;
            exhaustTimer = exhaustLockTime;
        }

        // countdown lock timer
        if (exhaustTimer > 0f)
        {
            exhaustTimer -= Time.deltaTime;
        }
        else
        {
            // unlock regen after delay
            regenLocked = false;
        }

        // sprint blocked during lock
        isSprinting = !regenLocked && sprintInput && isMoving;

        // 🔋 STAMINA LOGIC
        if (isSprinting)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            if (stamina < maxStamina)
            {
                if (regenLocked)
                {
                    // 🔴 HARD LOCK (no regen)
                }
                else if (exhaustTimer <= 0f && stamina <= maxStamina * 0.2f)
                {
                    // 🟡 slow regen phase after lock
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