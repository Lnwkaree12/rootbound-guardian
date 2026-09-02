using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float gravity = -20f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float originalStepOffset;

    public bool IsGrounded => controller != null && controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            originalStepOffset = controller.stepOffset;
        }
    }

    private void LateUpdate()
    {
        // ล็อกไม่ให้หมุนเบี้ยวสำหรับมุมมอง 2D / Top-Down
        transform.rotation = Quaternion.identity;
    }

    // สั่งเดินปกติ
    public void Move(Vector2 inputDirection)
    {
        if (controller == null || !controller.enabled) return;

        controller.stepOffset = originalStepOffset;
        ApplyGravity();

        Vector3 move = new Vector3(inputDirection.x, 0, inputDirection.y);
        Vector3 finalVelocity = (move * moveSpeed) + verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    // สั่งพุ่งแดช
    public void Dash(Vector3 direction)
    {
        if (controller == null || !controller.enabled) return;

        verticalVelocity.y = 0f;
        controller.stepOffset = 0.8f;

        Vector3 finalVelocity = direction * dashSpeed;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    // คำนวณแรงโน้มถ่วง
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;
    }

    // หยุดแรงเคลื่อนที่ทั้งหมดทันที (ใช้ตอนเปิด Win UI หรือ Pause)
    public void StopVelocity()
    {
        verticalVelocity = Vector3.zero;
    }

    // ระบบวาร์ปตำแหน่งปลอดภัย
    public void Teleport(Vector3 newPosition)
    {
        if (controller == null) return;

        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}