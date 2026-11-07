using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;

    [Header("Gravity Settings")]
    [SerializeField] private float customGravity = -30f;

    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float forcedExitInputLockDuration = 0.3f;

    private Rigidbody rb;
    private Vector2 currentInput;
    private bool isDashing;
    private Vector3 dashDirection;
    private bool isClimbing = false;
    public bool IsClimbing => isClimbing;
    private float forcedExitInputLockTimer;
    private float targetLadderPos;
    private bool useXAlignment = true;
    private PlayerAnimation playerAnim;

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        playerAnim = GetComponent<PlayerAnimation>();
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            float verticalInput = 0f;

            // 🌟 เช็คปุ่ม W เพื่อขึ้น และ S เพื่อลงอย่างเดียวเท่านั้น (ไม่ให้ปุ่ม A/D มาเกี่ยวตอนปีน)
            if (Input.GetKey(KeyCode.W))
            {
                verticalInput = 1f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                verticalInput = -1f;
            }

            float moveX = 0f;
            float moveZ = 0f;

            if (useXAlignment)
            {
                float smoothX = Mathf.Lerp(rb.position.x, targetLadderPos, Time.fixedDeltaTime * 15f);
                moveX = (smoothX - rb.position.x) / Time.fixedDeltaTime;
                moveZ = 0f;
            }
            else
            {
                float smoothZ = Mathf.Lerp(rb.position.z, targetLadderPos, Time.fixedDeltaTime * 15f);
                moveZ = (smoothZ - rb.position.z) / Time.fixedDeltaTime;
                moveX = 0f;
            }

            // ล็อกความเร็วแนวนอนให้อยู่กับที่ของบันได บังคับให้ขยับได้แค่ขึ้น-ลง (Y) เท่านั้น
            rb.linearVelocity = new Vector3(moveX, verticalInput * climbSpeed, moveZ);

            if (playerAnim != null)
            {
                playerAnim.SetClimbSpeed(Mathf.Abs(verticalInput));
            }

            return;
        }

        if (forcedExitInputLockTimer > 0f)
        {
            forcedExitInputLockTimer -= Time.fixedDeltaTime;

            if (!IsGrounded)
            {
                rb.AddForce(Vector3.up * customGravity, ForceMode.Acceleration);
            }

            return;
        }

        // 1. แรงโน้มถ่วงปกติ
        if (!IsGrounded)
        {
            rb.AddForce(Vector3.up * customGravity, ForceMode.Acceleration);
        }

        // 2. ระบบ Dash
        if (isDashing)
        {
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, 0f, dashDirection.z * dashSpeed);
        }
        else
        {
            if (currentInput.sqrMagnitude > 0.01f)
            {
                Vector3 targetVelocity = new Vector3(currentInput.x * moveSpeed, rb.linearVelocity.y, currentInput.y * moveSpeed);
                rb.linearVelocity = targetVelocity;
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }

    public void EnableClimbing(bool climbState, Vector3 ladderPosition, bool alignX)
    {
        isClimbing = climbState;
        useXAlignment = alignX;

        if (playerAnim != null)
        {
            playerAnim.SetClimbing(climbState);
            if (!climbState) playerAnim.SetClimbSpeed(0f);
        }

        if (isClimbing)
        {
            forcedExitInputLockTimer = 0f;
            rb.useGravity = false;
            Vector3 pos = rb.position;

            if (useXAlignment)
            {
                targetLadderPos = ladderPosition.x;
                pos.x = targetLadderPos;
            }
            else
            {
                targetLadderPos = ladderPosition.z;
                pos.z = targetLadderPos;
            }

            rb.MovePosition(pos);
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            ExitClimbingNormal();
        }
    }

    public void ExitClimbingNormal()
    {
        if (!isClimbing) return;

        isClimbing = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;

        if (playerAnim != null)
        {
            playerAnim.SetClimbing(false);
            playerAnim.SetClimbSpeed(0f);
        }
    }

    public void ExitClimbingWithForce(Vector3 force)
    {
        if (!isClimbing) return;

        isClimbing = false;
        rb.useGravity = true;
        forcedExitInputLockTimer = Mathf.Max(0f, forcedExitInputLockDuration);

        if (playerAnim != null)
        {
            playerAnim.SetClimbing(false);
            playerAnim.SetClimbSpeed(0f);
        }

        // 🌟 ล้างค่าอินพุตทั้งหมดและตัดความเร็วเก่าทิ้ง ป้องกันแรงกดค้างส่งผลต่อทิศทางกระเด็น
        currentInput = Vector2.zero;
        rb.linearVelocity = Vector3.zero; // เคลียร์ความเร็วเก่าให้เป็นศูนย์ก่อน
        rb.linearVelocity = force;       // ใส่แรงพุ่งตามที่ตั้งค่าไว้ใน Inspector เป๊ะๆ
    }

    public void Dash(Vector3 direction)
    {
        dashDirection = direction.normalized;
        isDashing = true;
    }

    public void StopDash()
    {
        isDashing = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void Move(Vector2 inputDirection)
    {
        // ถือกำหนดว่าถ้ากำลังปีนอยู่ ห้ามให้อินพุตภายนอก (A/D/W/S จากคีย์บอร์ดปกติ) มายุ่ง
        if (isClimbing || forcedExitInputLockTimer > 0f) return;
        currentInput = inputDirection;
    }

    private void OnCollisionStay(Collision collision)
    {
        IsGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsGrounded = false;
    }
}