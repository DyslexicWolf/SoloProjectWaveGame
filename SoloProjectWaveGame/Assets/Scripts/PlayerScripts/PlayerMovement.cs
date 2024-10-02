using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float groundDrag;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    public Transform orientation;

    [Header("Jump")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public bool readyToJump;
    public bool isJumping;

    [Header("GroundCheck")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool isGrounded;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit sloapHit;
    private bool exitingSlope;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    public float slideSpeed;
    public float slideScaleY;
    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;
    private float slideTimer;

    private bool isSliding;

    private PlayerInputActions playerInputActions;

    private Vector3 moveDirection;
    private Vector2 moveInput;

    private Rigidbody rb;


    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
        playerInputActions.Player.Jump.canceled += OnJumpCanceled;
        playerInputActions.Player.Sprint.performed += ctx => desiredMoveSpeed = sprintSpeed;
        playerInputActions.Player.Sprint.canceled += ctx => desiredMoveSpeed = walkSpeed;
        playerInputActions.Player.Crouch.performed += OnCrouchPerformed;
        playerInputActions.Player.Crouch.canceled += OnCrouchCanceled;
        playerInputActions.Player.Slide.performed += OnSlidePerformed;
        playerInputActions.Player.Slide.canceled += OnSlideCanceled;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        desiredMoveSpeed = walkSpeed;
        startYScale = transform.localScale.y;
    }
    

    private void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Crouching");
        desiredMoveSpeed = crouchSpeed;
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        desiredMoveSpeed = walkSpeed;
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        isJumping = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        isJumping = false;
    }

    private void OnSlidePerformed(InputAction.CallbackContext context)
    {
        if (moveInput.magnitude > Vector2.zero.magnitude && !isSliding)
        {
            isSliding = true;
            slideTimer = maxSlideTime;
            StartSlide();
        }
    }

    private void OnSlideCanceled(InputAction.CallbackContext context)
    {
        if (isSliding)
        {
            StopSlide();
        }
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }


    private void Update()
    {
        //ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        //handle drag
        if (isGrounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }

        if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMovement());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }
    
    private void FixedUpdate()
    {

        if (isSliding && OnSlope() && rb.linearVelocity.y <= 0.1f)
        {
            desiredMoveSpeed = slideSpeed;
            SlidingMovement();
        }
        else
        {
            desiredMoveSpeed = sprintSpeed;
            MovePlayer();

        }

        if (isJumping && readyToJump && isGrounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        //calculate movement direction
        moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 10f, ForceMode.Force);
        }
        else if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        //limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if(rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private IEnumerator SmoothlyLerpMovement()
    {
        //smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, sloapHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            {
                time += Time.deltaTime * speedIncreaseMultiplier;
            }
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void Jump()
    {
        exitingSlope = true;
        //reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;
        Debug.Log(slideTimer);
        //sliding normal
        if(!OnSlope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce * 10f, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }

        //sliding down slope
        else 
        {
            rb.AddForce(GetSlopeMoveDirection(inputDirection) * slideForce * 10f, ForceMode.Force);
        }
        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }

    private void StartSlide()
    {
        transform.localScale = new Vector3(transform.localScale.x, slideScaleY, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void StopSlide()
    {
        isSliding = false;
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out sloapHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, sloapHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 moveDirection)
    {
        return Vector3.ProjectOnPlane(moveDirection, sloapHit.normal).normalized;
    }
}
