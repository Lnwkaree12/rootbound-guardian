using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f; // 🌟 ปรับเพิ่มตรงนี้ ยิ่งเยอะ ยิ่งพุ่งไปได้ไกลขึ้นในเวลาเท่าเดิม!

    [Header("Gravity Settings")]
    [SerializeField] private float customGravity = -30f; // ช่วยดึงแรงโน้มถ่วงให้ตกไว ไม่ลอย

    private Rigidbody rb;
    private Vector2 currentInput;
    private bool isDashing;
    private Vector3 dashDirection;

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // 1. เพิ่ม Custom Gravity เพื่อไม่ให้ตัวละครลอยตอนตก
        if (!IsGrounded)
        {
            rb.AddForce(Vector3.up * customGravity, ForceMode.Acceleration);
        }

        // 2. ระบบพุ่ง Dash
        if (isDashing)
        {
            // 🌟 ตั้งความเร็วพุ่งแกน X/Z ทันที (ตัดความเร็วร่วงแกน Y ชั่วคราวเพื่อให้แดชเป็นแนวตรง)
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, 0f, dashDirection.z * dashSpeed);
        }
        else
        {
            // ระบบเดินปกติ
            if (currentInput.sqrMagnitude > 0.01f)
            {
                Vector3 targetVelocity = new Vector3(currentInput.x * moveSpeed, rb.linearVelocity.y, currentInput.y * moveSpeed);
                rb.linearVelocity = targetVelocity;
            }
            else
            {
                // หยุดแกน X/Z ทันทีเมื่อไม่ได้กดเดิน (ป้องกันการสไลด์)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }

    // สั่งพุ่ง Dash
    public void Dash(Vector3 direction)
    {
        dashDirection = direction.normalized;
        isDashing = true;
    }

    // 🛑 เมื่อหมดเวลา Dash สั่งหยุดความเร็วพุ่งทันที (ตัดอาการไถล)
    public void StopDash()
    {
        isDashing = false;
        // ล้างความเร็วแกน X/Z ให้กลับมาเท่ากับ 0 ทันที เพื่อไม่ให้ไถลต่อ
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void Move(Vector2 inputDirection)
    {
        currentInput = inputDirection;
    }

    private void OnCollisionStay(Collision collision)
    {
        // เช็กพื้นแบบง่าย
        IsGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsGrounded = false;
    }
}