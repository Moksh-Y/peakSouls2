using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 7f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Vector2 rotationSpeed = new Vector2(120f, 120f);
    [SerializeField] private Vector2 pitchMinMax = new Vector2(-40f, 85f);
    [SerializeField] private LayerMask collisionLayers;
    
    [Header("Lock-on Settings")]
    [SerializeField] private float lockOnDistance = 30f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float sphereCastRadius = 0.5f;

    [Header("Controller Settings")]
    [SerializeField] private float rightStickSensitivity = 3f;
    [SerializeField] private bool invertYAxis = false;
    [SerializeField] private float deadzone = 0.1f;
    
    private float currentYaw;
    private float currentPitch;
    private Transform currentTarget;
    private bool isLockedOn;
    private Vector3 smoothVelocity;
    private Camera cam;
    private float currentDistance;
    private float smoothDistanceVelocity;
    

    private void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize camera behind player
        Vector3 playerRotation = playerTransform.rotation.eulerAngles;
        currentYaw = playerRotation.y;
        currentPitch = 20f; // Slightly tilted down initial view
        currentDistance = distance;
    }

    private void LateUpdate()
    {
        if (isLockedOn && currentTarget != null)
        {
            HandleLockedCamera();
        }
        else
        {
            HandleFreeCamera();
        }

        // Handle lock-on input
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetButtonDown("R_Stick_Click"))
        {
            ToggleLockOn();
        }
    }

    private void HandleCameraInput()
    {
        // Get both mouse and controller input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");
        
        float rightStickX = Input.GetAxis("R_Stick_Y");
        float rightStickY = Input.GetAxis("R_Stick_X");
        
        // Apply deadzone to controller input
        if (Mathf.Abs(rightStickX) < deadzone) rightStickX = 0;
        if (Mathf.Abs(rightStickY) < deadzone) rightStickY = 0;
        
        // Combine inputs (use whichever was moved last)
        float inputX = (Mathf.Abs(mouseX) > 0) ? mouseX : rightStickX * rightStickSensitivity;
        float inputY = (Mathf.Abs(mouseY) > 0) ? mouseY : rightStickY * rightStickSensitivity;
        
        if (invertYAxis) inputY *= -1;
        
        // Update camera angles
        currentYaw += inputX * rotationSpeed.x * Time.deltaTime;
        currentPitch -= inputY * rotationSpeed.y * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, pitchMinMax.x, pitchMinMax.y);
    }

    private void HandleFreeCamera()
    {
        HandleCameraInput();

        // Calculate camera position
        Vector3 targetPosition = CalculateCameraPosition();
        
        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref smoothVelocity,
            1f / smoothSpeed
        );

        // Look at player
        transform.LookAt(playerTransform.position + Vector3.up * height);
    }

    private void HandleLockedCamera()
    {
        // Calculate middle point between player and target
        Vector3 middlePoint = Vector3.Lerp(
            playerTransform.position,
            currentTarget.position,
            0.5f
        );

        // Calculate optimal camera position
        Vector3 directionToTarget = (currentTarget.position - playerTransform.position).normalized;
        Vector3 targetPosition = playerTransform.position - directionToTarget * distance;
        targetPosition.y = playerTransform.position.y + height;

        // Handle collision for locked camera
        targetPosition = HandleCameraCollision(targetPosition);

        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref smoothVelocity,
            1f / smoothSpeed
        );

        // Look at middle point
        transform.LookAt(middlePoint);
    }

    private Vector3 CalculateCameraPosition()
    {
        // Convert angles to direction
        float yawRad = currentYaw * Mathf.Deg2Rad;
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        
        Vector3 direction = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );

        // Get the desired distance based on collision
        float targetDistance = distance;
        Vector3 targetPosition = playerTransform.position - direction * targetDistance + Vector3.up * height;
        
        // Check for collision and adjust position
        targetPosition = HandleCameraCollision(targetPosition);
        
        // Calculate actual distance after collision
        float currentDistance = Vector3.Distance(targetPosition, playerTransform.position);
        
        // If we're too close, push camera out to minimum distance
        if (currentDistance < minDistance)
        {
            targetPosition = playerTransform.position - direction * minDistance + Vector3.up * height;
        }

        return targetPosition;
    }

    private Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        // Ensure minimum ground height
        float minHeight = playerTransform.position.y + 0.5f;
        if (desiredPosition.y < minHeight)
        {
            desiredPosition.y = minHeight;
        }

        // Cast a ray from player to desired camera position
        RaycastHit hit;
        Vector3 direction = (desiredPosition - playerTransform.position).normalized;
        float targetDistance = Vector3.Distance(playerTransform.position, desiredPosition);
        
        // Start the spherecast from slightly above player to avoid ground detection
        Vector3 castStart = playerTransform.position + Vector3.up * 1.5f;
        
        if (Physics.SphereCast(
            castStart,
            0.2f,
            direction,
            out hit,
            targetDistance,
            collisionLayers))
        {
            return castStart + direction * (hit.distance - 0.1f);
        }
        
        return desiredPosition;
    }

    private void ToggleLockOn()
    {
        if (isLockedOn)
        {
            isLockedOn = false;
            currentTarget = null;
            return;
        }

        // Find nearest enemy
        Collider[] enemies = Physics.OverlapSphere(
            playerTransform.position,
            lockOnDistance,
            enemyLayer
        );

        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (Collider enemy in enemies)
        {
            // Check if enemy is in line of sight
            Vector3 directionToEnemy = (enemy.transform.position - playerTransform.position).normalized;
            if (Physics.SphereCast(
                playerTransform.position + Vector3.up,
                sphereCastRadius,
                directionToEnemy,
                out RaycastHit hit,
                lockOnDistance
            ))
            {
                if (hit.collider == enemy)
                {
                    float distance = Vector3.Distance(playerTransform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }
            }
        }

        if (closestEnemy != null)
        {
            currentTarget = closestEnemy;
            isLockedOn = true;
        }
    }
}