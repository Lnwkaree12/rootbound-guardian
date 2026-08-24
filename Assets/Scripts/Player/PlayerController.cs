using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement Logic")]
    [SerializeField] private PlayerMovement movement = new PlayerMovement();

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

    private Vector2 rawInput;
    private bool jumpRequested;
    private bool dashRequested;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // ดึง Action จาก Input Action Asset
        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        jumpAction = playerMap.FindAction("Jump");
        dashAction = playerMap.FindAction("Sprint");
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // ผูก Event เข้ากับ Button
        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        inputActions.Disable();
    }

    private void OnJump(InputAction.CallbackContext context) => jumpRequested = true;
    private void OnDash(InputAction.CallbackContext context) => dashRequested = true;

    private void Update()
    {
        // อ่านค่า Movement Vector2 (X, Y)
        rawInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(rawInput.x, 0f, rawInput.y);

        // คำนวณเวกเตอร์รวม
        Vector3 finalVelocity = movement.CalculateVelocity(
            moveDirection,
            controller.isGrounded,
            jumpRequested,
            dashRequested,
            transform
        );

        // สั่งเคลื่อนที่
        if (controller != null && controller.enabled)
        {
            controller.Move(finalVelocity * Time.deltaTime);
        }

        // รีเซ็ตการกดปุ่มแบบ Frame-by-Frame
        jumpRequested = false;
        dashRequested = false;
    }
}