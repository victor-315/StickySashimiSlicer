using UnityEngine;

public class three2 : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPoint;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    void Update()
    {
        if (targetPoint == null) return;

        // Direction to target
        Vector3 direction = targetPoint.position - transform.position;

        // Ignore movement if close enough
        if (direction.magnitude <= stoppingDistance)
            return;

        // Normalize direction
        Vector3 moveDirection = direction.normalized;

        // Move forward
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Rotate to face movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void OnDrawGizmosSelected()
    {
        if (targetPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, targetPoint.position);
    }
}