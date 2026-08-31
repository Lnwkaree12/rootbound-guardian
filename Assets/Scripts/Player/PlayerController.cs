using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.2f;

    private PlayerInputHandler inputHandler;
    private PlayerMovement movement; // เปลี่ยนชื่อตัวแปรให้อ่านง่ายขึ้น
    private PlayerAnimation playerAnim;

    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        playerAnim = GetComponent<PlayerAnimation>();
    }

    private void Update()
    {
        HandleDashState();

        if (isDashing)
        {
            movement.Dash(dashDirection);
        }
        else
        {
            movement.Move(inputHandler.MoveInput);
        }
    }

    private void HandleDashState()
    {
        if (inputHandler.DashPressed && !isDashing)
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
    }

    private void StartDash()
    {
        Vector2 input = inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        if (moveDir.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }
        else
        {
            dashDirection = moveDir.normalized;
        }

        if (playerAnim != null) playerAnim.TriggerDash();

        isDashing = true;
        dashTimer = dashDuration;

        // สั่ง Trigger แดชครั้งเดียวทันที
       

        inputHandler.ResetDashFlag();
    }
}