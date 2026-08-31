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

    public bool IsGrounded => controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        // ล็อกไม่ให้หมุนเบี้ยวสำหรับมุมมอง 2D / Top-Down
        transform.rotation = Quaternion.identity;
    }

    // สั่งเดินปกติ
    public void Move(Vector2 inputDirection)
    {
        ApplyGravity();

        Vector3 move = new Vector3(inputDirection.x, 0, inputDirection.y);
        Vector3 finalVelocity = (move * moveSpeed) + verticalVelocity;

        if (controller.enabled)
        {
            controller.Move(finalVelocity * Time.deltaTime);
        }
    }

    // สั่งพุ่งแดช
    public void Dash(Vector3 direction)
    {
        ApplyGravity();

        Vector3 finalVelocity = (direction * dashSpeed) + verticalVelocity;

        if (controller.enabled)
        {
            controller.Move(finalVelocity * Time.deltaTime);
        }
    }

    // คำนวณแรงโน้มถ่วง (ตกเหว/แตะพื้น)
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravity * Time.deltaTime;
    }

    // ระบบวาร์ปตำแหน่งปลอดภัย (ใช้ตอนตกเหว)
    public void Teleport(Vector3 newPosition)
    {
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}