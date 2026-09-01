using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveZ");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int PickupHash = Animator.StringToHash("Pickup");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (animator == null) return;

        // 1. อ่านค่าการเคลื่อนที่
        Vector2 input = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
        float speed = input.sqrMagnitude;

        animator.SetFloat(SpeedHash, speed);

        if (speed > 0.01f)
        {
            animator.SetFloat(MoveXHash, input.x);
            animator.SetFloat(MoveYHash, input.y);
        }
    }

    // ฟังก์ชันสั่งเปิด/ปิด แอนิเมชัน แดช
    public void TriggerDash()
    {
        if (animator != null)
        {
            animator.SetTrigger(DashHash);
        }
    }

    // ฟังก์ชันสั่งแอนิเมชันตาย
    //public void TriggerDeath()
    //{
    //    if (animator != null)
    //    {
    //        animator.SetTrigger("IsDead");
    //    }
    //}

    public void PlayPickUpAnimation()
    {
        animator.SetTrigger("Pickup");
    }
}