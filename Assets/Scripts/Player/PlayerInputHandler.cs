using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private InputActionAsset inputActions;

    public Vector2 MoveInput { get; private set; }
    public bool DashPressed { get; private set; }

    // เปลี่ยนมาอ่านค่า WasPressedThisFrame ตรงๆ จาก Action
    public bool InteractPressed => interactAction != null && interactAction.WasPressedThisFrame();

    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction interactAction; // เพิ่ม InputAction สำหรับ Interact

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        dashAction = playerMap.FindAction("Sprint");
        interactAction = playerMap.FindAction("Interact"); // ดึง Action Interact
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        dashAction?.Enable();
        interactAction?.Enable(); // เปิดใช้งาน interactAction

        if (dashAction != null)
            dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        dashAction?.Disable();
        interactAction?.Disable(); // ปิดใช้งาน interactAction

        if (dashAction != null)
            dashAction.performed -= OnDashPerformed;
    }

    private void Update()
    {
        if (moveAction != null)
        {
            MoveInput = moveAction.ReadValue<Vector2>();
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        DashPressed = true;
    }

    public void ResetDashFlag()
    {
        DashPressed = false;
    }
}