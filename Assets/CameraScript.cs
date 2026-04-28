using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform playerBody;

    [Header("Settings")]
    public float sensitivity = 150f;
    public float smoothTime = 0.03f;

    private float xRotation = 0f;

    private float mouseXVelocity;
    private float mouseYVelocity;

    private float currentMouseX;
    private float currentMouseY;

    private bool isLocked = false;

    void Update()
    {
        HandleCursorLock();

        if (!isLocked) return;

        HandleMouseLook();
    }

    void HandleMouseLook()
    {
        float targetMouseX = Input.GetAxis("Mouse X") * sensitivity;
        float targetMouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // ✅ Smooth mouse (no jitter)
        currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref mouseXVelocity, smoothTime);
        currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref mouseYVelocity, smoothTime);

        // Vertical rotation
        xRotation -= currentMouseY * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation
        playerBody.Rotate(Vector3.up * currentMouseX * Time.deltaTime);
    }

    void HandleCursorLock()
    {
        // 🔒 REQUIRED for WebGL
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isLocked = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isLocked = false;
        }
    }
}