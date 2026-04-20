using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;

    public float mouseSensitivity = 200f;
    public float smoothTime = 0.05f; // lower = snappier, higher = smoother

    float xRotation = 0f;

    float currentMouseX;
    float currentMouseY;

    float mouseXVelocity;
    float mouseYVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        // Raw input
        float targetMouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float targetMouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Smooth input
        currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref mouseXVelocity, smoothTime);
        currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref mouseYVelocity, smoothTime);

        // Vertical rotation (camera)
        xRotation -= currentMouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation (player body)
        playerBody.Rotate(Vector3.up * currentMouseX);
    }
}