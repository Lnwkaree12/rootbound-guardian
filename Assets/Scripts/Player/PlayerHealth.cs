using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Safe Zone Settings")]
    [SerializeField] private bool isInSafeZone = true; // เริ่มเกมให้อยู่ในจุดเซฟก่อน

    [Header("i-Frame Settings")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;

    [Header("Death Settings")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private int respawnHealth = 15; // 👈 กำหนดเลือดที่จะได้เมื่อเกิดใหม่ (ปรับเปลี่ยนได้ตามต้องการใน Inspector)

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Events")]
    public UnityEvent<int, int> onHealthChanged;
    public UnityEvent onDeath;

    private Coroutine healthRoutine;
    private PlayerOxygen oxygenComponent;
    private Coroutine bleedRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        oxygenComponent = GetComponent<PlayerOxygen>();
    }

    private void Start()
    {
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // เริ่มเช็กลูปเลือด (สูดอากาศพิษ / ฟื้นฟู) ทันทีที่เริ่มเกม
        // PlayerHealth no longer heals in safe zones; oxygen handles regeneration.
        UpdateSafeZoneState(isInSafeZone);
    }

    // ==========================================
    // ⚔️ ฟังก์ชัน TakeDamage (สำหรับศัตรู/กับดัก)
    // ==========================================
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
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    // Apply damage over time (used for traps/bleed).
    // Example: damagePerTick=2, tickInterval=0.5f, tickCount=5 will deal 10 damage over 2.5s.
    public void ApplyDamageOverTime(int damagePerTick, float tickInterval, int tickCount)
    {
        if (damagePerTick <= 0 || tickInterval <= 0f || tickCount <= 0) return;

        if (bleedRoutine != null)
        {
            StopCoroutine(bleedRoutine);
        }
        bleedRoutine = StartCoroutine(BleedRoutine(damagePerTick, tickInterval, tickCount));
    }

    // Convenience: split totalDamage into tickCount chunks (ceil division)
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
        }

        bleedRoutine = null;
    }

    // ==========================================
    // 🌬️ ระบบ Safe Zone และ อากาศพิษ
    // ==========================================
    public void UpdateSafeZoneState(bool safe)
    {
        isInSafeZone = safe;

        if (healthRoutine != null)
        {
            StopCoroutine(healthRoutine);
        }

        if (isInSafeZone)
        {
            Debug.Log("[Safe Zone] เข้าสู่จุดปลอดภัย: ระบบจะไม่เพิ่มเลือดโดยตรง แต่จะเพิ่ม Oxygen เท่านั้น");
        }
        else
        {
            Debug.Log("[Danger Zone] ออกจากจุดเซฟ: ระบบ Oxygen จะลดค่าออกซิเจนแทนเลือด");
        }

        // แจ้งให้ระบบ Oxygen ด้วย (ถ้ามี)
        oxygenComponent?.UpdateSafeZoneState(safe);
    }
    private IEnumerator PoisonGasRoutine()
    {
        // ปรากฎว่าโค้ดการหักเลือดจากก๊าซย้ายไปไว้ที่ PlayerOxygen
        yield break;
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

        isInvulnerable = true;
        PlaySound(deathSound);
        onDeath?.Invoke();
        Debug.Log("Player Died!");

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer(gameObject);
        }

        // หลังจาก Respawn แล้ว รีเซ็ตเลือดเป็นเต็มแต่ไม่กระทบค่า Oxygen
        ResetHealthAfterRespawn();
    }

    public void ResetHealth()
    {
        // กำหนดให้เลือดเกิดใหม่เท่ากับ respawnHealth (ไม่ให้เกิน maxHealth และไม่ต่ำกว่า 1)
        currentHealth = Mathf.Clamp(respawnHealth, 1, maxHealth);
        isInvulnerable = false;

        // ส่ง Event อัปเดต UI หลอดเลือด
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // เมื่อเกิดใหม่ให้ถือว่าอยู่ในจุดเซฟ (จะเริ่มเด้งเลือดเพิ่มจาก respawnHealth ไปตามเวลา)
        UpdateSafeZoneState(true);
        Debug.Log($"[ResetHealth] เกิดใหม่แล้ว! รีเซ็ตเลือดเป็น {currentHealth}/{maxHealth}");
    }

    // เรียกเมื่อ Respawn เพื่อรีเซ็ตเลือดเต็มโดยไม่เพิ่ม Oxygen
    public void ResetHealthAfterRespawn()
    {
        currentHealth = maxHealth;
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        // อย่าเรียก UpdateSafeZoneState เพื่อป้องกันการเพิ่ม Oxygen ทันที
        Debug.Log($"[ResetHealthAfterRespawn] เกิดใหม่แล้ว รีเซ็ตเลือดเต็มเป็น {currentHealth}/{maxHealth} (ไม่เพิ่ม Oxygen)");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}