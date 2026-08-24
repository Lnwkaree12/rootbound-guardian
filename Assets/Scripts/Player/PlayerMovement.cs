using UnityEngine;

[System.Serializable]
public class PlayerMovement
{
    [Header("Move Settings")]
    public float moveSpeed = 7f;
    public float gravity = -20f;

    [Header("Jump Settings")]
    public float jumpHeight = 2.5f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Vector3 verticalVelocity;
    private Vector3 dashDirection;
    private float dashTimer;
    private float cooldownTimer;

    public bool IsDashing { get; private set; }

    public Vector3 CalculateVelocity(Vector3 moveInput, bool isGrounded, bool jumpPressed, bool dashPressed, Transform playerTransform)
    {
        // 1. จัดการ Cooldown แดช
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        // 2. สถานะ Dashing
        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) IsDashing = false;
            return dashDirection * dashSpeed;
        }

        // เริ่มกด Dash
        if (dashPressed && cooldownTimer <= 0)
        {
            IsDashing = true;
            dashTimer = dashDuration;
            cooldownTimer = dashCooldown;

            // ถ้าไม่มี Input ให้แดชไปข้างหน้าตัวละคร
            dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : playerTransform.forward;
            return dashDirection * dashSpeed;
        }

        // 3. คำนวณความเร็วแนวราบ (X, Z)
        Vector3 horizontalVelocity = moveInput.normalized * moveSpeed;

        // 4. คำนวณแรงโน้มถ่วงและการกระโดด (Y)
        if (isGrounded)
        {
            if (verticalVelocity.y < 0) verticalVelocity.y = -2f;

            if (jumpPressed)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        return horizontalVelocity + verticalVelocity;
    }
}