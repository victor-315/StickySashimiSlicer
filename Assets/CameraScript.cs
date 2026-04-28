using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform playerBody;

    [Header("Settings")]
    public float sensitivity = 180f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private bool isLocked = false;

    void Update()
    {
        HandleCursorLock();

        if (!isLocked) return;

        // ✅ Instant input
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // Update rotation values immediately
        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply instantly
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleCursorLock()
    {
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