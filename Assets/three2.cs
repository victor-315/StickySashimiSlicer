using UnityEngine;

public class OrbitAndFaceDirection : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform centerPoint;
    public float orbitRadius = 5f;
    public float baseSpeed = 50f;

    [Header("Random Slowdown")]
    public float minSpeedMultiplier = 0.3f;
    public float maxSpeedMultiplier = 1f;
    public float slowdownChangeInterval = 2f;

    [Header("Facing")]
    public float rotationSmoothness = 8f;

    private float currentAngle;
    private float currentSpeedMultiplier = 1f;

    private Vector3 lastPosition;

    void Start()
    {
        if (centerPoint == null)
        {
            Debug.LogError("Center Point is missing!");
            enabled = false;
            return;
        }

        // Start at current position
        Vector3 offset = transform.position - centerPoint.position;
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

        lastPosition = transform.position;

        InvokeRepeating(nameof(RandomizeSpeed), 0f, slowdownChangeInterval);
    }

    void Update()
    {
        // Rotate around center
        currentAngle += baseSpeed * currentSpeedMultiplier * Time.deltaTime;

        float radians = currentAngle * Mathf.Deg2Rad;

        Vector3 orbitPos = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            transform.position.y - centerPoint.position.y,
            Mathf.Sin(radians) * orbitRadius
        );

        transform.position = centerPoint.position + orbitPos;

        // Movement direction
        Vector3 movementDirection = (transform.position - lastPosition).normalized;

        // Face movement direction
        if (movementDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothness * Time.deltaTime
            );
        }

        lastPosition = transform.position;
    }

    void RandomizeSpeed()
    {
        currentSpeedMultiplier = Random.Range(
            minSpeedMultiplier,
            maxSpeedMultiplier
        );
    }

    void OnDrawGizmosSelected()
    {
        if (centerPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPoint.position, orbitRadius);
    }
}