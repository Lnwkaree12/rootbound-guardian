using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;       // สำหรับเสียงเอฟเฟกต์ชั่วคราว (Dash, Fall)
    [SerializeField] private AudioSource footstepAudioSource; // สำหรับเสียงเดินโดยเฉพาะ (ป้องกันเสียงทับกัน)
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip fallSound;

    [Header("Fall Detection")]
    [SerializeField] private float fallThresholdSpeed = -2f;
    private CharacterController characterController;
    private Rigidbody rb;
    private bool isFallingSoundPlayed = false;

    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private PlayerAnimation playerAnim;

    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        playerAnim = GetComponent<PlayerAnimation>();

        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // ปรับแก้ตรงนี้: หาก FootstepAudioSource ไม่ถูกตั้งค่า หรือเผลอใช้ตัวเดียวกับ audioSource
        // ให้สร้าง AudioSource ตัวใหม่แยกต่างหากทันที
        if (footstepAudioSource == null || footstepAudioSource == audioSource)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        HandleDashState();
        CheckFallingState();

        if (isDashing)
        {
            movement.Dash(dashDirection);
            StopFootstepSound(); // หยุดเสียงเดินทันทีเมื่อกำลังแดช
        }
        else
        {
            movement.Move(inputHandler.MoveInput);
            HandleFootstepSounds();
        }
    }

    private void HandleFootstepSounds()
    {
        bool isGrounded = (characterController != null) ? characterController.isGrounded : true;
        bool isMoving = inputHandler.MoveInput.sqrMagnitude > 0.01f;

        // เช็คว่าอยู่บนพื้น และกำลังเคลื่อนที่
        if (isGrounded && isMoving)
        {
            if (footstepSound != null && footstepAudioSource != null)
            {
                // ถ้าเสียงเดินไม่ได้กำลังเล่นอยู่ ให้เริ่มเล่น
                if (!footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.clip = footstepSound;
                    footstepAudioSource.Play();
                }
            }
        }
        else
        {
            // ถ้าหยุดเดิน หรือ ลอยอยู่บนอากาศ ให้หยุดเล่นเสียงเดินทันที
            StopFootstepSound();
        }
    }

    private void StopFootstepSound()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }
    }

    private void CheckFallingState()
    {
        float verticalVelocity = 0f;

        if (characterController != null)
        {
            verticalVelocity = characterController.velocity.y;
        }
        else if (rb != null)
        {
            verticalVelocity = rb.linearVelocity.y;
        }

        bool isGrounded = (characterController != null) ? characterController.isGrounded : true;

        if (!isGrounded && verticalVelocity < fallThresholdSpeed)
        {
            if (!isFallingSoundPlayed)
            {
                StopFootstepSound();
                PlaySound(fallSound);
                isFallingSoundPlayed = true;
            }
        }
        else if (isGrounded)
        {
            isFallingSoundPlayed = false;
        }
    }

    private void HandleDashState()
    {
        if (inputHandler.DashPressed && !isDashing)
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
    }

    private void StartDash()
    {
        Vector2 input = inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        if (moveDir.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }
        else
        {
            dashDirection = moveDir.normalized;
        }

        if (playerAnim != null) playerAnim.TriggerDash();

        PlaySound(dashSound);

        isDashing = true;
        dashTimer = dashDuration;

        inputHandler.ResetDashFlag();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}