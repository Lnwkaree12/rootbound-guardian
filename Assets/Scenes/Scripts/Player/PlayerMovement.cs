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

    [Header("Step/Ledge Assist Settings")]
    [SerializeField] private float stepSmoothSpeed = 15f;

    private Rigidbody rb;
    private Vector2 currentInput;
    private bool isDashing;
    private Vector3 dashDirection;

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        // 1. เพิ่มแรงโน้มถ่วงเมื่อไม่อยู่บนพื้น
        if (!IsGrounded)
        {
            rb.AddForce(Vector3.up * customGravity, ForceMode.Acceleration);
        }

        // 2. ระบบพุ่ง Dash
        if (isDashing)
        {
            CheckAndSmoothStepUp(dashDirection);
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);
        }
        else
        {
            // ระบบเดินปกติ
            if (currentInput.sqrMagnitude > 0.01f)
            {
                Vector3 moveDir = new Vector3(currentInput.x, 0f, currentInput.y).normalized;

                // ช่วยไต่ขอบตื้นๆ เวลาเดินปกติ
                CheckAndSmoothStepUp(moveDir);

                Vector3 targetVelocity = new Vector3(
                    currentInput.x * moveSpeed,
                    rb.linearVelocity.y,
                    currentInput.y * moveSpeed
                );
                rb.linearVelocity = targetVelocity;
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }

    private void CheckAndSmoothStepUp(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        // ใช้ Raycast เช็คขอบตื้นๆ ด้านหน้าเพื่อช่วยก้าวขึ้นแท่นอย่างนุ่มนวล
        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, 0.4f))
        {
            float heightDifference = hit.point.y - transform.position.y;

            if (heightDifference > 0.02f && heightDifference < 0.5f)
            {
                Vector3 targetPos = rb.position;
                targetPos.y = Mathf.Lerp(rb.position.y, rb.position.y + heightDifference + 0.1f, Time.fixedDeltaTime * stepSmoothSpeed);
                rb.MovePosition(targetPos);
            }
        }
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
        currentInput = inputDirection;
    }

    private void OnCollisionStay(Collision collision)
    {
        // 🌟 ปรับปรุงการเช็คพื้นให้กว้างและแม่นยำขึ้น ป้องกันอาการติดขัดกลางอากาศ
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.25f)
            {
                IsGrounded = true;
                return;
            }
        }
        IsGrounded = false;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsGrounded = false;
    }
}