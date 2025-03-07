using Unity.VisualScripting;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public float moveSpeed = 5;
    public float jumpForce = 10f;
    public float gravity = -9.81f;
    private CharacterController charController;
    private Vector3 velocity;
    public bool isGrounded;
    //private Animator animator;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        //animator = GetComponent<Animator>();
    }

    void Update()
    {
        isGrounded = charController.isGrounded;
        Move();
        Jump();
        ApplyGravity();
        //AnimateCharacter();
    }

    void Move()
    {
        float moveZ = Input.GetAxis("Vertical");
        float moveX = Input.GetAxis("Horizontal");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        charController.Move(move * moveSpeed * Time.deltaTime);

        //animator.SetFloat("Speed", new Vector2(moveX, moveZ).magnitude);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isGrounded = false; // Ensure the character doesn't jump again immediately
            //animator.SetBool("IsJumping", true);
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            // Reset vertical velocity when grounded to prevent floating
            velocity.y = -0.2f;
            //animator.SetBool("IsJumping", false);
        }

        charController.Move(velocity * Time.deltaTime);
    }


    /*void AnimateCharacter()
    {
        if (isGrounded && Input.GetButton("Jump"))
        {
            animator.SetBool("IsJumping", true);
        }
        else
        {
            animator.SetBool("IsJumping", false);
        }
    }*/
}
