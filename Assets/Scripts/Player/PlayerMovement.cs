using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private AudioClip moveSfx;
    private float currentSpeed;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 input;
    private Vector3 lastMoveDirection = Vector3.back; // default forward direction
    private bool wasMoving;
    public Animator animator;
    public SpriteRenderer sr;

    [Header("Stats Reference")]
    private PlayerStats playerStats;

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration = 0.3f;
    private bool isAttacking = false;
    private float attackTimer = 0f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    private bool isGrounded = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 0.6f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupDuration = 0.35f;
    private bool isPickingUp = false;
    private float pickupTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Freeze rotation on X, Y, and Z axes to prevent tipping over
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        if (moveSfx != null) audioSource.clip = moveSfx;

        // Auto-assign references if not set in Inspector
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        // Disable root motion to prevent the animator from freezing physics movement
        if (animator != null) animator.applyRootMotion = false;
        
        // Find PlayerStats component
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
    }

    void Update()
    {
        // Handle Dash Cooldown Timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Handle Active Dash State (Ignore input and updates during active dash)
        if (isDashing)
        {
            return;
        }

        // Handle Active Pickup State (Ignore input and updates during picking up)
        if (isPickingUp)
        {
            pickupTimer += Time.deltaTime;
            if (pickupTimer >= pickupDuration)
            {
                isPickingUp = false;
            }
            return;
        }

        // Handle Active Attack State
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                isAttacking = false;
            }
            return;
        }

        // Trigger Dash on Left Shift (Requires 20 Stamina)
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            if (playerStats == null || playerStats.ConsumeStamina(20f))
            {
                StartDash();
                return;
            }
            else
            {
                Debug.Log("[PlayerMovement] Not enough stamina to dash!");
            }
        }

        // Trigger Jump on Space
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            StartJump();
            return;
        }

        // Trigger attack on Left Click (Mouse 0)
        if (Input.GetMouseButtonDown(0))
        {
            StartAttack();
            return;
        }

        // Trigger Pickup on F
        if (Input.GetKeyDown(KeyCode.F) && isGrounded)
        {
            StartPickup();
            return;
        }

        HandleInput();
        Animate();
    }

    private void StartAttack()
    {
        Debug.Log("[PlayerMovement] Attack triggered! (คลิกซ้ายแล้ว)");
        isAttacking = true;
        attackTimer = 0f;
        StopVelocity();

        // ลดเลือดตัวเอง 3 หน่วยทุกครั้งที่โจมตีเพื่อเชื่อม HP bar
        if (playerStats != null)
        {
            playerStats.TakeDamage(3f);
        }

        if (animator != null)
        {
            Debug.Log("[PlayerMovement] Playing animation: AttackDown");
            animator.Play("AttackDown");
        }
        else
        {
            Debug.LogError("[PlayerMovement] Cannot play animation because animator is NULL!");
        }
    }

    private void StartJump()
    {
        Debug.Log("[PlayerMovement] Jump triggered! (กระโดดแล้ว)");
        // Apply vertical velocity for physics jump
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        isGrounded = false;
        
        if (animator != null)
        {
            Debug.Log("[PlayerMovement] Playing animation: Jump");
            animator.Play("Jump");
        }
    }

    private void StartDash()
    {
        Debug.Log("[PlayerMovement] Dash triggered! (พุ่งตัวแล้ว)");
        isDashing = true;
        dashTimer = 0f;
        dashCooldownTimer = dashCooldown;
        
        // Dash in the direction of current input, or the last moved direction if standing still
        dashDirection = input != Vector3.zero ? input.normalized : lastMoveDirection.normalized;
        
        if (animator != null)
        {
            Debug.Log("[PlayerMovement] Playing animation: Dash");
            animator.Play("Dash");
        }
    }

    public void HandleInput()
    {
        // รับค่าแกนนอน (A/D) และแกนตั้ง (W/S)
        input.x = Input.GetAxisRaw("Horizontal");
        input.z = Input.GetAxisRaw("Vertical"); // เก็บค่า W/S ไว้ในแกน Z ของ 3D
        
        if (input.sqrMagnitude > 1) input.Normalize();

        if (input.x > 0) sr.flipX = false;
        else if (input.x < 0) sr.flipX = true;

        if (input != Vector3.zero) lastMoveDirection = input;

        bool isMoving = input != Vector3.zero;
        if (isMoving && !wasMoving)
        {
            if (moveSfx != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else if (!isMoving && wasMoving)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        wasMoving = isMoving;
    }

    void FixedUpdate()
    {
        // Simple ground check based on vertical velocity of Rigidbody
        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;

        if (isDashing)
        {
            // Update physics-based dash timer in FixedUpdate
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= dashDuration)
            {
                isDashing = false;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // Stop dash speed
            }
            else
            {
                // Maintain high horizontal speed during dash, keeping physics Y gravity intact
                rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);
            }
            return;
        }

        if (isPickingUp)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // คำนวณความเร็วรวมเพื่อส่งให้ Animator
        currentSpeed = new Vector2(input.x, input.z).magnitude * moveSpeed;

        // ใช้ Rigidbody ควบคุมการเคลื่อนที่ทั้งแกน X และ Z
        // ส่วนแกน Y ให้ใช้ความเร็วเดิมของ Rigidbody (เผื่อกรณีมีแรงโน้มถ่วงหรือการตกจากที่สูง)
        rb.linearVelocity = new Vector3(input.x * moveSpeed, rb.linearVelocity.y, input.z * moveSpeed);
    }
    
    public void Animate()
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveZ", lastMoveDirection.z);
        animator.SetFloat("Speed", currentSpeed);
    }

    public void StopVelocity()
    {
        input = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    private void StartPickup()
    {
        Debug.Log("[PlayerMovement] StartPickup triggered! (เล่นแอนิเมชั่นและเช็คเก็บกุญแจ)");
        isPickingUp = true;
        pickupTimer = 0f;
        StopVelocity();

        if (animator != null)
        {
            animator.Play("Pickup");
        }

        TryInteract();
    }

    private void TryInteract()
    {
        // 1. Check if any DoorController is within interactive distance (e.g. 7.5 units)
        DoorController[] allDoors = FindObjectsOfType<DoorController>();
        foreach (var d in allDoors)
        {
            float doorScale = Mathf.Max(d.transform.lossyScale.x, d.transform.lossyScale.z, 1f);
            float maxDoorReach = Mathf.Max(d.interactRadius * doorScale, 7.5f);
            if (Vector3.Distance(transform.position, d.transform.position) <= maxDoorReach)
            {
                Debug.Log("[PlayerMovement] Interacting with DoorController on [F] key press!");
                d.TryOpenDoor();
                return;
            }
        }

        // 2. Search for nearby Key or other interactables within 5.5 units (scaled to character)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5.5f);
        foreach (var col in hitColliders)
        {
            // Check for KeyPickup script on self or parent
            KeyPickup key = col.GetComponent<KeyPickup>();
            if (key == null) key = col.GetComponentInParent<KeyPickup>();

            if (key != null)
            {
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.CollectKey();
                }
                else
                {
                    QuestManager qm = FindObjectOfType<QuestManager>();
                    if (qm != null) qm.CollectKey();
                }
                Debug.Log("[PlayerMovement] Successfully interacted and collected the Key!");
                Destroy(key.gameObject);
                break;
            }
        }
    }
}