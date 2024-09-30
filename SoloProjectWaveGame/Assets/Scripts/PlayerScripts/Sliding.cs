using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement playerMovement;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideScaleY;
    private float startYScale;
    private bool isSliding;

    private PlayerInputActions playerInputActions;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInputActions = new PlayerInputActions();

        playerInputActions.Player.Move.performed += OnInputPerformed;
        playerInputActions.Player.Move.canceled += OnInputCancelled;
        playerInputActions.Player.Slide.performed += OnSlidePerformed;
        playerInputActions.Player.Slide.canceled += OnSlideCanceled;

        startYScale = transform.localScale.y;
    }

    

    private void OnInputPerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    private void OnInputCancelled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnSlidePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("in onSlidePerformed");
        if(moveInput.magnitude > Vector2.zero.magnitude && !isSliding)
        {
            Debug.Log("in if");
            isSliding = true;
        }
    }

    private void OnSlideCanceled(InputAction.CallbackContext context)
    {
        if (isSliding)
        {
            StopSlide();
        }
    }

    private void FixedUpdate()
    {
        if (isSliding)
        {
            SlidingMovement();
        }
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

        slideTimer -= Time.deltaTime;

        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }

    private void StartSlide()
    {
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideScaleY, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void StopSlide()
    {
        isSliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}
