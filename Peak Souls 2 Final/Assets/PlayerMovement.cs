using System;
using RPGCharacterAnims.Actions;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 5f;
    public float jumpForce = 5f;
    public float rollSpeed = 250f;
    public int combo_count = 0;
    public bool isDead = false;
    public bool isHit = false;
    public bool isArmed = true;
    public int vials_left = 5;
    public bool isHealing = false;
    public bool isJumping = false;
    public bool isRolling = false;
    public bool isBlocking = false;
    public Rigidbody player;
    public Animator animator;
    public GameObject weapon;
    public bool isAttacking = false;
    public bool isInterruptable = true;
    public bool isGrounded = true;
    public bool isMoving = true;
    public float health = 100;
    public float stamina = 100;
    public float heal_amount = 30;
    public Transform cameraTransform;
    public float movementForceMultiplier = 10f;
    public float rotationSpeed = 4f;
    //public Vector2 moveInput;
    public LayerMask groundLayer;


    //Animation Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int BlockStateHash = Animator.StringToHash("BlockState");
    private static readonly int BlockingHash = Animator.StringToHash("Blocking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int ComboCountHash = Animator.StringToHash("Combo Count");
    private static readonly int RollHash = Animator.StringToHash("Roll");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int ArmedHash = Animator.StringToHash("Armed");
    private static readonly int MovingHash = Animator.StringToHash("isMoving");


    void Start()
    {
        player = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        cameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        groundCheck();
        if(isInterruptable==false) return;   
        checkInput();
        
        //Debug.Log("working update");
        
    }

    void groundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.3f, groundLayer);
        animator.SetBool(GroundedHash, isGrounded);
    }

    void movement(){
        //Debug.Log("working movement");
        float xAxis = Input.GetAxis("Horizontal");
        float yAxis = Input.GetAxis("Vertical");
        //Debug.Log(xAxis);
        //Debug.Log(yAxis);
        Vector2 moveInput = new Vector2(xAxis, yAxis);
        if(moveInput == Vector2.zero) {
            animator.SetBool("isMoving", false);
            return;}
        animator.SetBool("isMoving", true);
        //Debug.Log(player.linearVelocity.magnitude*2);
        animator.SetFloat("Speed", player.linearVelocity.magnitude*2);
        //Debug.Log("working ");
        //Debug.Log(moveInput);
        Vector3 inputDirection = cameraTransform.forward * moveInput.y + cameraTransform.right * moveInput.x;
        inputDirection.y = 0;
        inputDirection = inputDirection.normalized;
        Vector3 moveDirection = inputDirection;
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ?  sprintSpeed: walkSpeed;
        
        // Calculate target linearVelocity
        Vector3 targetVelocity = moveDirection * targetSpeed;
        
        // Apply movement force
        Vector3 velocityDiff = targetVelocity - player.linearVelocity;
        velocityDiff.y = 0; // Don't affect vertical movement
        
        
        player.AddForce(velocityDiff * movementForceMultiplier, ForceMode.Force);
        

        // Handle rotation
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Limit horizontal linearVelocity
        Vector3 horizontalVelocity = new Vector3(player.linearVelocity.x, 0, player.linearVelocity.z);
        if (horizontalVelocity.magnitude > targetSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * targetSpeed;
            player.linearVelocity = new Vector3(limitedVelocity.x, player.linearVelocity.y, limitedVelocity.z);
        }
    }
    void sheathe(){
        animator.SetBool("Armed", false);
        weapon.SetActive(false);
        isArmed = false;
    }
    void unsheathe(){
        weapon.SetActive(true);
        animator.SetBool("Armed", true);
        isArmed = true;
    }
    void jump(){
        if(!isGrounded) return;
        player.AddForce(jumpForce*Vector3.up);
        animator.SetTrigger("Jump");
    }
    void attack(int combo_count){

    }
    void checkInput(){
        
        if(Input.GetKeyDown(KeyCode.E)|| Input.GetButtonDown("Circle_Button")){
            roll();
        }
        if(Input.GetKeyDown(KeyCode.Space)|| Input.GetButtonDown("Cross_Button")){
            jump();
        }
        if(Input.GetMouseButtonDown(1)){
            blocking();
        }
        if(Input.GetKeyDown(KeyCode.Y)){
            if(isArmed == false){
                unsheathe();
            }else{
                sheathe();
            };
        }
        if(Input.GetMouseButton(0)){
            attack(combo_count);
        }
        if(Input.GetKeyDown(KeyCode.R)){
            if(vials_left > 0){
                heal();
            }
        }
        movement();
    }
    void roll(){
        animator.SetTrigger("Roll");

        //player.AddForce(player.transform.forward * rollSpeed);
    }
    void takeDamage(int damage){

    }
    void blocking(){

    }
    void death(){

    }
    void heal(){

    }
}
