using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerInputHandler inputHandler;
    private SpriteRenderer spriteRenderer; // ใช้กรณีเกม 2D Sprite

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int InjuredHash = Animator.StringToHash("Injured");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int PickupHash = Animator.StringToHash("Pickup");

    [Header("Facing Direction Settings")]
    [SerializeField] private bool is3DModel = false; // เลือก True หากใช้ 3D Model, False หากใช้ 2D Sprite

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        inputHandler = GetComponent<PlayerInputHandler>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (animator == null || inputHandler == null) return;

        // อ่านค่าการเคลื่อนที่เฉพาะความเร็ว (Speed) เพื่อสลับระหว่าง Idle กับ Run
        float speed = inputHandler.MoveInput.sqrMagnitude;
        animator.SetFloat(SpeedHash, speed);

        // 🔄 ระบบหันหน้าตามทิศทางวิ่ง
        HandleFacingDirection(inputHandler.MoveInput.x);
    }

    private void HandleFacingDirection(float moveX)
    {
        if (Mathf.Abs(moveX) < 0.01f) return; // ไม่กดเดิน ให้ทิศเดิมไว้

        if (is3DModel)
        {
            // สำหรับ 3D Model: การหมุน Y-Axis (180 องศา หรือ 0 องศา)
            float targetAngle = moveX < 0 ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
        else if (spriteRenderer != null)
        {
            // สำหรับ 2D Sprite: ใช้ flipX ของ SpriteRenderer
            spriteRenderer.flipX = moveX < 0;
        }
        else
        {
            // หรือพลิก Scale X ในระดับ Transform
            Vector3 scale = transform.localScale;
            scale.x = moveX < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // 💨 แดช
    public void TriggerDash()
    {
        if (animator != null)
        {
            animator.SetTrigger(DashHash);
        }
    }

    // 💥 โดนดาเมจ
    public void TriggerInjured()
    {
        if (animator != null)
        {
            animator.SetTrigger(InjuredHash);
        }
    }

    // 💀 ตาย
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.SetTrigger(DeadHash);
        }
    }

    // 📦 เก็บของ
    public void PlayPickUpAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(PickupHash);
        }
    }

    // ⏱️ คำนวณความยาวคลิป Animation ตายเพื่อส่งกลับไปให้ PlayerHealth
    public float GetDeadAnimationLength()
    {
        if (animator == null) return 0f;

        // ดึงคลิป animation ที่เปิดใช้งานทั้งหมดใน AnimatorController
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            // ตรวจสอบชื่อ Clip ให้ตรงกับชื่อใน Animator (เช่น "Dead", "Die", หรือ "Player_Dead")
            if (clip.name.ToLower().Contains("dead") || clip.name.ToLower().Contains("die"))
            {
                return clip.length;
            }
        }

        return 1f; // ค่า Default สำรองกรณีหาชื่อคลิปไม่เจอ
    }
}