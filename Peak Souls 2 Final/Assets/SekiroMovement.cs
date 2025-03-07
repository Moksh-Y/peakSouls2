using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SekiroMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 1f;
    [SerializeField] private float movementForceMultiplier = 10f;

    [Header("Ground Detection")]
    [SerializeField] private float groundRayDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Combat Settings")]
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float rollDuration = 0.5f;
    [SerializeField] private float attackDuration = 1.0f;
    [SerializeField] private int maxCombo = 3;

    private Rigidbody rb;
    public Animator animator;
    private Transform cameraTransform;
    private Vector2 moveInput;
    private bool isSprinting;
    private bool isGrounded;
    private bool isRolling;
    private bool isAttacking;
    private float rollTimeRemaining;
    private float attackTimeRemaining;
    private int currentCombo;
    private Vector3 rollDirection;
    private Vector3 currentVelocity;

    // Animator Hash IDs
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int IsRollingHash = Animator.StringToHash("IsRolling");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int ComboCountHash = Animator.StringToHash("ComboCount");

    private void Start()
    {
        InitializeComponents();
        SubscribeToEvents();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        cameraTransform = Camera.main.transform;
    }

    private void SubscribeToEvents()
    {
        PlayerInputs.OnMoveInput += HandleMoveInput;
        PlayerInputs.OnSprintInput += HandleSprintInput;
        PlayerInputs.OnJumpInput += HandleJumpInput;
        PlayerInputs.OnRollInput += HandleRollInput;
        PlayerInputs.OnAttackInput += HandleAttackInput;
    }

    private void OnDestroy()
    {
        PlayerInputs.OnMoveInput -= HandleMoveInput;
        PlayerInputs.OnSprintInput -= HandleSprintInput;
        PlayerInputs.OnJumpInput -= HandleJumpInput;
        PlayerInputs.OnRollInput -= HandleRollInput;
        PlayerInputs.OnAttackInput -= HandleAttackInput;
    }

    private void HandleMoveInput(Vector2 input) => moveInput = input;
    private void HandleSprintInput(bool sprinting) => isSprinting = sprinting;

    private void HandleJumpInput()
    {
        if (isGrounded && !isRolling && !isAttacking)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger(JumpHash);
            animator.SetBool(FreeFallHash, true);
            isGrounded = false;
        }
    }

    private void HandleRollInput()
    {
        if (!isRolling && !isAttacking && isGrounded)
        {
            StartRoll();
        }
    }

    private void HandleAttackInput()
    {
        if (!isRolling && isGrounded)
        {
            StartAttack();
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        rollTimeRemaining = rollDuration;

        rollDirection = moveInput != Vector2.zero
            ? GetInputDirection()
            : transform.forward;

        animator.SetTrigger(RollHash);
        animator.SetBool(IsRollingHash, true);
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimeRemaining = attackDuration;

        if (attackTimeRemaining > 0)
        {
            currentCombo = (currentCombo % maxCombo) + 1;
        }
        else
        {
            currentCombo = 1;
        }

        animator.SetTrigger(AttackHash);
        animator.SetInteger(ComboCountHash, currentCombo);
    }

    private void Update()
    {
        CheckGroundState();
        UpdateStateTimers();
        UpdateAnimator();
    }

    private void CheckGroundState()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundRayDistance, groundLayer);
        
        if (isGrounded && !wasGrounded)
        {
            animator.SetBool(FreeFallHash, false);
            animator.SetBool(JumpHash, false);
        }
        
        animator.SetBool(GroundedHash, isGrounded);
    }

    private void UpdateStateTimers()
    {
        if (isRolling)
        {
            rollTimeRemaining -= Time.deltaTime;
            if (rollTimeRemaining <= 0)
            {
                isRolling = false;
                animator.SetBool(IsRollingHash, false);
            }
        }

        if (isAttacking)
        {
            attackTimeRemaining -= Time.deltaTime;
            if (attackTimeRemaining <= 0)
            {
                isAttacking = false;
                currentCombo = 0;
                animator.SetInteger(ComboCountHash, 0);
            }
        }
    }

    private void UpdateAnimator()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentMaxSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float speedFactor = Mathf.Clamp01(horizontalVelocity.magnitude / currentMaxSpeed) * 15f;

        animator.SetFloat(SpeedHash, speedFactor);
        animator.SetFloat(MotionSpeedHash, moveInput.magnitude);

        if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            animator.SetBool(FreeFallHash, true);
        }
    }

    private void FixedUpdate()
    {
        rb.linearDamping = isGrounded ? groundDrag : airDrag;

        if (isRolling)
        {
            ApplyRollMovement();
        }
        else if (!isAttacking)
        {
            ApplyMovement();
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector3 inputDirection = cameraTransform.forward * moveInput.y + cameraTransform.right * moveInput.x;
        inputDirection.y = 0;
        return inputDirection.normalized;
    }

    private void ApplyRollMovement()
    {
        Vector3 targetVelocity = rollDirection * rollSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void ApplyMovement()
    {
        if (moveInput == Vector2.zero) return;

        Vector3 moveDirection = GetInputDirection();
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Calculate target linearVelocity
        Vector3 targetVelocity = moveDirection * targetSpeed;
        
        // Apply movement force
        Vector3 velocityDiff = targetVelocity - rb.linearVelocity;
        velocityDiff.y = 0; // Don't affect vertical movement
        
        rb.AddForce(velocityDiff * movementForceMultiplier, ForceMode.Force);

        // Handle rotation
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Limit horizontal linearVelocity
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > targetSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * targetSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }
}