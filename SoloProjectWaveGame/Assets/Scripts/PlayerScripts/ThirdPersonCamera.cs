using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    private PlayerInputActions playerInputActions;
    private Vector2 mouseInput;
    public float rotationSpeed;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Look.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
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
        //calculate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        //rotate player object
        Vector3 inputDir = orientation.forward * mouseInput.y + orientation.right * mouseInput.x;
        if(inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, rotationSpeed * Time.deltaTime);
        }
    }
}
