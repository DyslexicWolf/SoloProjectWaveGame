using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;

    private PlayerInputActions playerInputActions;
    private Vector2 mouseInput;
    public float rotationSpeed;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Look.performed += ctx => mouseInput = ctx.ReadValue<Vector2>().normalized;
        playerInputActions.Player.Look.canceled += ctx => mouseInput = Vector2.zero;
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
        Vector3 cameraForward = this.transform.forward;

        // Remove any vertical component
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            playerObj.rotation = Quaternion.Slerp(playerObj.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
