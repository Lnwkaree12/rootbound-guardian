using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(CapsuleCollider))]
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

    [Header("Wall Slide / Predictive Cast Settings")]
    [Tooltip("รัศมีของ SphereCast ควรใกล้เคียงกับความกว้างของตัวละคร")]
    [SerializeField] private float castRadius = 0.35f;
    [Tooltip("ระยะทางล่วงหน้าที่ต้องการเช็คสิ่งกีดขวาง")]
    [SerializeField] private float castDistance = 0.4f;
    [Tooltip("Layer ของกำแพงหรือ Props แต่งฉากที่ต้องการให้ไถ")]
    [SerializeField] private LayerMask obstacleLayer;

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
            rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);
        }
        else
        {
            if (currentInput.sqrMagnitude > 0.01f)
            {
                // แปลง Input (Vector2) เป็น Vector3 ในโลก 3D
                Vector3 inputDir = new Vector3(currentInput.x, 0f, currentInput.y).normalized;
                Vector3 targetVelocity = inputDir * moveSpeed;

                // 🌟 ระบบ Predictive Wall Slide ตรวจจับสิ่งกีดขวางล่วงหน้า
                Vector3 castOrigin = transform.position + Vector3.up * -.9f;
                RaycastHit hit;

                if (Physics.SphereCast(castOrigin, castRadius, inputDir, out hit, castDistance, obstacleLayer))
                {
                    Vector3 wallNormal = hit.normal;
                    wallNormal.y = 0f;
                    wallNormal.Normalize();

                    Vector3 slideDir = Vector3.ProjectOnPlane(inputDir, wallNormal);

                    if (slideDir.sqrMagnitude < 0.001f)
                    {
                        Vector3 wallTangent = Vector3.Cross(wallNormal, Vector3.up);
                        float dot = Vector3.Dot(inputDir, wallTangent);
                        slideDir = wallTangent * Mathf.Sign(dot);
                    }

                    targetVelocity = slideDir * moveSpeed;

                    Debug.Log($"<color=green>SphereCast ชน:</color> {hit.collider.name} | WallNormal: {wallNormal}");
                }
                else
                {
                    // Debug.Log("ทางโล่ง ไม่ชนอะไร");
                }

                // =========================================================================
                // 🌟 [จุดที่แก้ไข]: ใช้ Vector3.Lerp ช่วยหน่วงความเร็วไม่ให้พุ่งพรวดพราดเกินไป
                // เลข 15f ยิ่งน้อยยิ่งไหลลื่น/หน่วงมาก ยิ่งมากยิ่งเปลี่ยนทิศทางไว
                // =========================================================================
                Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                Vector3 smoothedVelocity = Vector3.Lerp(currentVelocityXZ, targetVelocity, Time.fixedDeltaTime * 10f);

                // กำหนดความเร็วแกน X, Z พร้อมรักษาสภาพความเร็วแกน Y (แรงโน้มถ่วง) ไว้
                rb.linearVelocity = new Vector3(smoothedVelocity.x, rb.linearVelocity.y, smoothedVelocity.z);

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

        currentInput = Vector2.zero;
        rb.linearVelocity = Vector3.zero;
        rb.linearVelocity = force;
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

    public void ResetAfterRespawn()
    {
        isClimbing = false;
        isDashing = false;
        forcedExitInputLockTimer = 0f;
        currentInput = Vector2.zero;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (playerAnim != null)
        {
            playerAnim.SetClimbing(false);
            playerAnim.SetClimbSpeed(0f);
        }
    }

    public void Move(Vector2 inputDirection)
    {
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * -.9f;
        Gizmos.DrawWireSphere(origin, castRadius);
    }
}