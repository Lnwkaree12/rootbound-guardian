using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Safe Zone Settings")]
    [SerializeField] private bool isInSafeZone = true;

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

        isInvulnerable = true;
        PlaySound(deathSound);

        if (playerAnim != null) playerAnim.TriggerDeath();

        onDeath?.Invoke();
        Debug.Log("Player Died!");

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        // 1. ดึงระยะเวลาของ Animation ตายจาก PlayerAnimation
        float animLength = playerAnim != null ? playerAnim.GetDeadAnimationLength() : 0f;

        // 2. รอจนกว่า Animation ตายจะเล่นเสร็จ + Delay ที่ตั้งค่าไว้
        yield return new WaitForSeconds(animLength + respawnDelay);

        GameOverUIManager gameOverUI = FindObjectOfType<GameOverUIManager>();
        if (gameOverUI != null)
        {
            gameOverUI.TriggerGameOver("พลังชีวิตหมดลงแล้ว!");
        }
        else if (GameOverUIManager.Instance != null)
        {
            GameOverUIManager.Instance.TriggerGameOver("พลังชีวิตหมดลงแล้ว!");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] GameOverUIManager ไม่พบในฉาก");
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // 1. ดึงระยะเวลาของ Animation ตายจาก PlayerAnimation
        float animLength = playerAnim != null ? playerAnim.GetDeadAnimationLength() : 0f;

        // 2. รอจนกว่า Animation ตายจะเล่นเสร็จ + Delay ที่ตั้งค่าไว้
        yield return new WaitForSeconds(animLength + respawnDelay);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer(gameObject);
        }

        ResetHealthAfterRespawn();
    }

    public void ResetHealth()
    {
        currentHealth = Mathf.Clamp(respawnHealth, 1, maxHealth);
        isInvulnerable = false;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateSafeZoneState(true);
        Debug.Log($"[ResetHealth] เกิดใหม่แล้ว! รีเซ็ตเลือดเป็น {currentHealth}/{maxHealth}");
    }

    public void ResetHealthAfterRespawn()
    {
        currentHealth = maxHealth;
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (playerAnim != null)
        {
            Animator anim = playerAnim.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // สั่ง Rebind เพื่อล้างค่า Trigger ทั้งหมดที่ค้างอยู่ (เช่น Dead / Injured)
                anim.Rebind();
                anim.Update(0f);

                // สั่งให้บังคับเข้า State "Idle" ทันที
                anim.Play("Idle");
            }
        }

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