using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    public Transform neckPivot;

    [Header("Settings")]
    public float sensitivity = 180f;

    [Header("Sprint Lean (STRONG)")]
    public float leanForward = 15f;   // 🔥 much stronger forward push
    public float leanDown = -0.08f;     // stronger dip
    public float leanSmooth = 12f;

    private float xRotation;
    private float yRotation;

    private bool isLocked;

    private Vector3 defaultNeckPos;

    void Start()
    {
        defaultNeckPos = neckPivot.localPosition;
    }

    void Update()
    {
        HandleCursorLock();

        if (!isLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -85f, 65f);

        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
        neckPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        bool sprinting = Input.GetKey(KeyCode.LeftShift);

        Vector3 targetPos = defaultNeckPos;

        if (sprinting)
        {
            // 🔥 STRONG forward + slight downward lean
            targetPos += neckPivot.forward * leanForward;
            targetPos += new Vector3(0f, leanDown, 0f);
        }

        neckPivot.localPosition = Vector3.Lerp(
            neckPivot.localPosition,
            targetPos,
            Time.deltaTime * leanSmooth
        );
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