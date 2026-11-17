using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerInputHandler inputHandler;
    private SpriteRenderer spriteRenderer;
    private PlayerOxygen playerOxygen; // 🫧 อ้างอิงระบบออกซิเจน

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int InjuredHash = Animator.StringToHash("Injured");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int PickupHash = Animator.StringToHash("Pickup");
    private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing"); // 🪜 บันทึก Hash ท่าปีน
    private static readonly int OxygenTierHash = Animator.StringToHash("OxygenTier"); // 🫧 บันทึก Hash Oxygen Tier
    private static readonly int ClimbSpeedHash = Animator.StringToHash("ClimbSpeed"); // 🪜 บันทึก Hash ความเร็วปีน

    [Header("Facing Direction Settings")]
    [SerializeField] private bool is3DModel = false;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        inputHandler = GetComponent<PlayerInputHandler>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerOxygen = GetComponent<PlayerOxygen>();
    }

    private void Update()
    {
        if (animator == null || inputHandler == null) return;

        // 1. อัปเดตความเร็วเดิน/วิ่ง
        float speed = inputHandler.MoveInput.sqrMagnitude;
        animator.SetFloat(SpeedHash, speed);

        // 2. อัปเดตระดับ OxygenTier ส่งเข้า Animator (0 = ปกติ, 1 = เหนื่อย, 2 = วิกฤต) เป็น Float
        UpdateOxygenTier();

        // 3. ระบบหันหน้าตามทิศทางวิ่ง
        HandleFacingDirection(inputHandler.MoveInput.x);
    }

    private void UpdateOxygenTier()
    {
        if (playerOxygen == null || animator == null) return;

        float oxygenPercent = (float)playerOxygen.CurrentOxygen / playerOxygen.MaxOxygen;
        float tier = 0f;

        if (oxygenPercent > 0.5f) tier = 0f;
        else if (oxygenPercent > 0.25f) tier = 1f;
        else tier = 2f;

        // ใช้ SetFloat ให้ตรงกับ Blend Tree ที่ตั้งไว้
        animator.SetFloat(OxygenTierHash, tier);
    }

    // 🪜 สั่งเล่น/หยุด ท่าปีนบันได
    public void SetClimbing(bool isClimbing)
    {
        if (animator != null)
        {
            animator.SetBool(IsClimbingHash, isClimbing);
        }
    }

    // 🪜 ส่งค่าความเร็วไปที่ Parameter "ClimbSpeed" ใน Animator (คุมท่าหยุดนิ่ง / ขยับปีน)
    public void SetClimbSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(ClimbSpeedHash, speed);
        }
    }

    private void HandleFacingDirection(float moveX)
    {
        if (Mathf.Abs(moveX) < 0.01f) return;

        if (is3DModel)
        {
            float targetAngle = moveX < 0 ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.flipX = moveX < 0;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = moveX < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    public void TriggerDash()
    {
        if (animator != null) animator.SetTrigger(DashHash);
    }

    public void TriggerInjured()
    {
        if (animator != null) animator.SetTrigger(InjuredHash);
    }

    public void TriggerDeath()
    {
        if (animator != null) animator.SetTrigger(DeadHash);
    }

    public void PlayPickUpAnimation()
    {
        if (animator != null) animator.SetTrigger(PickupHash);
    }

    public float GetDeadAnimationLength()
    {
        if (animator == null) return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name.ToLower().Contains("dead") || clip.name.ToLower().Contains("die"))
            {
                return clip.length;
            }
        }
        return 1f;
    }
}