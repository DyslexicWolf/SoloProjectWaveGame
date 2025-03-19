using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    //camera needs to be a child of the playerobj, this script needs to be on the playerobj, assign the camera to the variable
    private PlayerInputActions playerInputActions;
    private Vector2 mouseInput;

    private float xRotation = 0f;
    private float yRotation = 0f;

    public float mouseSensitivity = 2f;
    public Transform playerCamera;
    public float lookUpLimit = -80f;
    public float lookDownLimit = 80f;

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

    void Update()
    {
        yRotation += mouseInput.x * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        xRotation -= mouseInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, lookUpLimit, lookDownLimit);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
