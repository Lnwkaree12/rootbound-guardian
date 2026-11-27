using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private bool canDash = true;

    [Header("Gravity Settings")]
    [SerializeField] private float customGravity = -30f;

    [Header("Ground Check (Raycast)")]
    [Tooltip("Layer ของพื้นหรือ Terrain ที่อนุญาตให้ยืนและเดินได้")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("จุดเริ่มต้นของ Ray ยิงจาก transform.position ขึ้นด้านบนเล็กน้อย")]
    [SerializeField] private float groundRayOriginOffset = 0.2f;
    [Tooltip("ความยาว Ray ที่ยิงลงด้านล่างจากจุด Origin")]
    [SerializeField] private float groundRayLength = 0.35f;
    [Tooltip("โควต้าเวลาสะสมสูงสุด (วินาที) ที่อนุญาตให้กดเดินข้ามช่องว่างตอน Raycast หลุดพื้น")]
    [SerializeField] private float maxGapWalkTime = 0.2f;

    private float gapWalkTimer;

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

    [Header("Edge Bumper State")]
    [SerializeField] private bool canTriggerEdgeBumper = false;

    public bool CanTriggerEdgeBumper => canTriggerEdgeBumper;
    public float MoveSpeed => moveSpeed;

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
    private PlayerInputHandler inputHandler;

    public bool IsGrounded { get; private set; }
    public float VerticalInput => inputHandler != null ? inputHandler.MoveInput.y : currentInput.y;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        playerAnim = GetComponent<PlayerAnimation>();
        inputHandler = GetComponent<PlayerInputHandler>();
        gapWalkTimer = maxGapWalkTime;
    }

    private void UpdateGroundedState()
    {
        Vector3 origin = transform.position + Vector3.up * groundRayOriginOffset;
        float rayDistance = groundRayOriginOffset + groundRayLength;

        bool hitGround = Physics.Raycast(
            origin,
            Vector3.down,
            rayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        // อยู่บนพื้น -> รีเซ็ตโควต้าเวลาเดินบนช่องว่าง
        if (hitGround)
        {
            gapWalkTimer = maxGapWalkTime;
            IsGrounded = true;
            return;
        }

        // ไม่อยู่บนพื้น -> เช็คว่ากำลังกดปุ่มเดินอยู่ไหม
        bool isMovingInput = currentInput.sqrMagnitude > 0.01f;

        if (isMovingInput)
        {
            if (gapWalkTimer > 0f)
            {
                gapWalkTimer -= Time.fixedDeltaTime;
                IsGrounded = gapWalkTimer > 0f;
            }
            else
            {
                IsGrounded = false;
            }
        }
        else
        {
            // ปล่อยปุ่ม -> หยุดนับเวลาไว้ที่เดิม แต่ถ้าโควต้าหมดแล้วให้ถือว่าหลุดพื้น
            if (gapWalkTimer <= 0f)
            {
                IsGrounded = false;
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();

        if (isClimbing)
        {
            float verticalInput = inputHandler != null
                ? Mathf.Clamp(inputHandler.MoveInput.y, -1f, 1f)
                : (InputSystemInput.GetKey(KeyCode.W) ? 1f : (InputSystemInput.GetKey(KeyCode.S) ? -1f : 0f));

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
            // ถ้าไม่อยู่บนพื้น จะไม่ให้เดินปกติ (คงความเร็วแกน Y ไว้สำหรับแรงโน้มถ่วง/การร่วง)
            if (!IsGrounded)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            else if (currentInput.sqrMagnitude > 0.01f)
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

        currentInput = Vector2.zero;
        rb.linearVelocity = Vector3.zero;
        rb.linearVelocity = force;
    }

    public void Dash(Vector3 direction)
    {
        if (!canDash) return;

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
        gapWalkTimer = maxGapWalkTime;
        IsGrounded = true;
        currentInput = Vector2.zero;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        canTriggerEdgeBumper = false;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * -.9f;
        Gizmos.DrawWireSphere(origin, castRadius);

        Vector3 groundOrigin = transform.position + Vector3.up * groundRayOriginOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * (groundRayOriginOffset + groundRayLength));
    }

    public void SetCanTriggerEdgeBumper(bool state)
    {
        canTriggerEdgeBumper = state;
    }

   

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetDashEnabled(bool enabled)
    {
        canDash = enabled;
        if (!canDash && isDashing)
        {
            StopDash();
        }
    }
}