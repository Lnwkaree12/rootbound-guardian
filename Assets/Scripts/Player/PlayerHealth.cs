using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Gas & Safe Zone Settings")]
    [SerializeField] private bool isInSafeZone = true; // เริ่มเกมให้อยู่ในจุดเซฟก่อน
    [SerializeField] private int gasDamagePerTick = 5;  // ดาเมจก๊าซพิษต่อครั้ง
    [SerializeField] private float gasTickInterval = 1f; // หักเลือดทุกๆ กี่วินาที
    [SerializeField] private int healPerTick = 10;      // เลือดเพิ่มขึ้นทีละเท่าไหร่ในจุดเซฟ
    [SerializeField] private float healTickInterval = 0.5f; // เพิ่มเลือดทุกๆ กี่วินาที

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

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // เริ่มเช็กลูปเลือด (สูดอากาศพิษ / ฟื้นฟู) ทันทีที่เริ่มเกม
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
            Debug.Log("[Safe Zone] เข้าสู่จุดปลอดภัย: หยุดลดเลือด และเริ่มเพิ่มเลือด");
            healthRoutine = StartCoroutine(HealingRoutine());
        }
        else
        {
            Debug.Log("[Gas Zone] ออกจากจุดเซฟ: เริ่มสูดก๊าซพิษ เลือดค่อยๆ ลดลง!");
            healthRoutine = StartCoroutine(PoisonGasRoutine());
        }
    }

    private IEnumerator PoisonGasRoutine()
    {
        while (!isInSafeZone && currentHealth > 0)
        {
            yield return new WaitForSeconds(gasTickInterval);

            currentHealth -= gasDamagePerTick;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            Debug.Log($"[Gas Harm] สูดก๊าซพิษ! เลือดเหลือ: {currentHealth}/{maxHealth}");
            onHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
                yield break;
            }
        }
    }

    private IEnumerator HealingRoutine()
    {
        while (isInSafeZone && currentHealth < maxHealth && currentHealth > 0)
        {
            yield return new WaitForSeconds(healTickInterval);

            currentHealth += healPerTick;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            Debug.Log($"[Safe Heal] พักในจุดเซฟ เลือดเพิ่มเป็น: {currentHealth}/{maxHealth}");
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }
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

        ResetHealth();
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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}