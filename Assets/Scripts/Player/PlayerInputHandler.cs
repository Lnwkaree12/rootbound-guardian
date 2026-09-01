using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private InputActionAsset inputActions;

    public Vector2 MoveInput { get; private set; }
    public bool DashPressed { get; private set; }
    public bool InteractPressed { get; private set; }

    private InputAction moveAction;
    private InputAction dashAction;

    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        dashAction = playerMap.FindAction("Sprint");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();

        dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();

        dashAction.performed -= OnDashPerformed;
    }

    private void Update()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        DashPressed = true;
    }

    public void ResetDashFlag()
    {
        DashPressed = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            InteractPressed = true;
        }
    }

    public void ResetInteractFlag()
    {
        InteractPressed = false;
    }

    private void LateUpdate()
    {
        // รีเซ็ตค่าเพื่อไม่ให้กดค้างข้ามเฟรม
        InteractPressed = false;
    }
}