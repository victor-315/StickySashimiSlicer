using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;

    public float sensitivity = 2.5f;   // raw sensitivity
    public float smoothing = 8f;       // higher = smoother

    float xRotation = 0f;

    Vector2 currentMouseDelta;
    Vector2 currentMouseDeltaVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Raw mouse input (NO deltaTime here)
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        // Smooth it
        currentMouseDelta = Vector2.Lerp(
            currentMouseDelta,
            targetMouseDelta,
            1f / smoothing
        );

        // Apply sensitivity AFTER smoothing
        Vector2 finalDelta = currentMouseDelta * sensitivity * 100f;

        // Vertical rotation
        xRotation -= finalDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation
        playerBody.Rotate(Vector3.up * finalDelta.x);
    }
}