using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private int dashHealthCost = 10;
    [SerializeField] private int dashOxygenCost = 15;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;         // สำหรับเสียงเอฟเฟกต์ทั่วไป (Fall)
    [SerializeField] private AudioSource footstepAudioSource; // สำหรับเสียงเดิน
    [SerializeField] private AudioSource dashAudioSource;     // 👈 แยก AudioSource สำหรับเสียง Dash โดยเฉพาะ
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
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;

    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        playerAnim = GetComponent<PlayerAnimation>();
        playerHealth = GetComponent<PlayerHealth>();
        playerOxygen = GetComponent<PlayerOxygen>();

        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // ตรวจสอบและสร้าง AudioSource สำหรับ Footstep หากยังไม่มี
        if (footstepAudioSource == null || footstepAudioSource == audioSource)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
        }

        // 👈 ตรวจสอบและสร้าง AudioSource สำหรับ Dash โดยเฉพาะ
        if (dashAudioSource == null || dashAudioSource == audioSource)
        {
            dashAudioSource = gameObject.AddComponent<AudioSource>();
            dashAudioSource.loop = false;
            dashAudioSource.playOnAwake = false;
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

        if (isGrounded && isMoving)
        {
            if (footstepSound != null && footstepAudioSource != null)
            {
                if (!footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.clip = footstepSound;
                    footstepAudioSource.Play();
                }
            }
        }
        else
        {
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
        // 🔊 เล่นเสียง Dash ผ่าน AudioSource ของ Dash โดยตรง
        if (dashSound != null && dashAudioSource != null)
        {
            Debug.Log("Dash Sound Played!"); // 👈 ดูใน Console ว่าข้อความนี้ขึ้นไหมเมื่อกด Dash
            dashAudioSource.PlayOneShot(dashSound);
        }
        else
        {
            Debug.LogWarning("Dash Sound หรือ Dash AudioSource ยังไม่ได้ตั้งค่า!");
        }

        // 🫧 ลด Oxygen เมื่อกด Dash (fallback เป็นหักเลือดถ้าไม่มี PlayerOxygen)
        if (playerOxygen != null)
        {
            playerOxygen.ConsumeOxygen(dashOxygenCost);
        }
        else if (playerHealth != null)
        {
            playerHealth.TakeDamage(dashHealthCost);
        }

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