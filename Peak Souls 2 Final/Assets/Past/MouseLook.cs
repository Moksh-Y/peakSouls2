using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public Transform playerBody;
    private float xRotation = 0f;
    public float minVerticalAngle = -60f; // Minimum vertical angle in degrees
    public float maxVerticalAngle = 60f;  // Maximum vertical angle in degrees

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
