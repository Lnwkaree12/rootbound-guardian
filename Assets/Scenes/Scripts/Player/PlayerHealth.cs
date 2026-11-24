using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// จัดการพลังชีวิต (HP) ของผู้เล่น สถานะ Safe Zone การอมตะชั่วคราว (i-Frame) และลำดับการตาย/เกิดใหม่
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Safe Zone Settings")]
    [SerializeField] private bool isInSafeZone;

    [Header("i-Frame Settings")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;

    [Header("Death Settings")]
    [SerializeField] private float respawnDelay = 0.5f; // เวลาหน่วงเพิ่มเติมหลัง Animation ตายจบ
    [SerializeField] private int respawnHealth = 15;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Events")]
    public UnityEvent<int, int> onHealthChanged;
    public UnityEvent onDeath;

    private Coroutine healthRoutine;
    private PlayerOxygen oxygenComponent;
    private PlayerAnimation playerAnim;
    private Coroutine bleedRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        oxygenComponent = GetComponent<PlayerOxygen>();
        playerAnim = GetComponent<PlayerAnimation>();
    }

    private void Start()
    {
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateSafeZoneState(isInSafeZone);
    }

    /// <summary>
    /// รับความเสียหายจากศัตรูหรือกับดัก
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable || currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"[Damage Taken] โดนโจมตีหักเลือด {damageAmount} | เลือดคงเหลือ: {currentHealth}/{maxHealth}");
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlaySound(hurtSound);
            if (playerAnim != null) playerAnim.TriggerInjured();
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    public void ApplyDamageOverTime(int damagePerTick, float tickInterval, int tickCount)
    {
        if (damagePerTick <= 0 || tickInterval <= 0f || tickCount <= 0) return;

        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
        }
        bleedRoutine = StartCoroutine(BleedRoutine(damagePerTick, tickInterval, tickCount));
    }

    public void ApplyTotalDamageOverTime(int totalDamage, int tickCount, float tickInterval)
    {
        if (totalDamage <= 0 || tickCount <= 0) return;
        int perTick = Mathf.CeilToInt((float)totalDamage / tickCount);
        ApplyDamageOverTime(perTick, tickInterval, tickCount);
    }

    private IEnumerator BleedRoutine(int damagePerTick, float tickInterval, int tickCount)
    {
        for (int i = 0; i < tickCount; i++)
        {
            yield return new WaitForSeconds(tickInterval);

            if (currentHealth <= 0) break;

            currentHealth -= damagePerTick;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            Debug.Log($"[Bleed] รับความเสียหายต่อเนื่อง {damagePerTick} เลือด: {currentHealth}/{maxHealth}");
            onHealthChanged?.Invoke(currentHealth, maxHealth);

            PlaySound(hurtSound);

            if (currentHealth <= 0)
            {
                Die();
                yield break;
            }
            else
            {
                if (playerAnim != null) playerAnim.TriggerInjured();
            }
        }

        bleedRoutine = null;
    }

    public void UpdateSafeZoneState(bool safe)
    {
        isInSafeZone = safe;

        if (healthRoutine != null)
        {
            StopCoroutine(healthRoutine);
        }

        if (isInSafeZone)
        {
            Debug.Log("[Safe Zone] เข้าสู่จุดปลอดภัย");
        }
        else
        {
            Debug.Log("[Danger Zone] ออกจากจุดเซฟ");
        }

        oxygenComponent?.UpdateSafeZoneState(safe);
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    private void Die()
    {
        if (healthRoutine != null) StopCoroutine(healthRoutine);

        // หยุด Bleed ค้างหากกำลังเดินเลือดอยู่ตอนตาย
        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
            bleedRoutine = null;
        }

        isInvulnerable = true;
        PlaySound(deathSound);

        if (playerAnim != null) playerAnim.TriggerDeath();

        onDeath?.Invoke();
        Debug.Log("Player Died!");

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetActionState(PlayerActionState.Dead);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // รอให้ Animation ตายเล่นจบ + เวลาหน่วงที่ตั้งไว้
        float animLength = playerAnim != null ? playerAnim.GetDeadAnimationLength() : 0f;
        yield return new WaitForSeconds(animLength + respawnDelay);

        if (CheckpointManager.Instance != null)
        {
            // เรียก RespawnPlayer (หากไม่มีจุดเซฟ CheckpointManager จะเรียก GameOver ให้อัตโนมัติ)
            CheckpointManager.Instance.RespawnPlayer(gameObject);

            // หากยังมีจุดเซฟ ให้คืนค่า Animator กลับสู่สถานะปกติหลังเกิดใหม่
            if (CheckpointManager.Instance.HasValidCheckpoint())
            {
                ResetAnimatorAfterRespawn();
            }
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] ไม่พบ CheckpointManager ในฉาก!");
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] ฟื้นฟูเลือด {amount} | เลือดคงเหลือ: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// ตั้งค่าพลังชีวิตโดยตรง ใช้สำหรับโหลดค่าเลือดที่บันทึกไว้ตอนกดเซฟ
    /// </summary>
    public void SetHealth(int amount)
    {
        currentHealth = Mathf.Clamp(amount, 1, maxHealth);
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] ตั้งค่าเลือดตามเซฟเป็น {currentHealth}/{maxHealth}");
    }

    public void ResetHealth()
    {
        ResetHealth(true);
    }

    public void ResetHealth(bool safeZone)
    {
        currentHealth = Mathf.Clamp(respawnHealth, 1, maxHealth);
        isInvulnerable = false;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateSafeZoneState(safeZone);

        Debug.Log($"[ResetHealth] เกิดใหม่แล้ว! ตั้งต้นเลือดเป็น {currentHealth}/{maxHealth} (ไม่รีเซ็ตออกซิเจน)");
    }

    /// <summary>
    /// คืนค่า Animator และสถานะตัวละครหลังเกิดใหม่โดยไม่ทับค่าเลือดที่โหลดมาจากจุดเซฟ
    /// </summary>
    private void ResetAnimatorAfterRespawn()
    {
        isInvulnerable = false;

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.ResetAfterRespawn();
        }

        if (playerAnim != null)
        {
            Animator anim = playerAnim.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Update(0f);
                anim.Play("Idle");
            }
        }
    }

    public void ResetHealthAfterRespawn()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        ResetAnimatorAfterRespawn();
        Debug.Log($"[ResetHealthAfterRespawn] เกิดใหม่แล้ว รีเซ็ตเลือดเต็มเป็น {currentHealth}/{maxHealth}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}