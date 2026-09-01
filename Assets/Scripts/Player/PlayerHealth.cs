using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("i-Frame Settings")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;

    [Header("Events")]
    public UnityEvent<int, int> onHealthChanged;
    public UnityEvent onDeath;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable || currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
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
        onDeath?.Invoke();
        Debug.Log("Player Died!");

        // เรียกใช้งานระบบ Respawn
        Respawn();
    }

    private void Respawn()
    {
        // 1. วาร์ปตัวละครไปจุด Save
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer(gameObject);
        }

        // 2. เติมเลือดกลับมาเต็ม
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isInvulnerable = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return; // ถ้าตายแล้วไม่ต้องฮีล

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // ไม่ให้เลือดเกิน MaxHealth

        onHealthChanged?.Invoke(currentHealth, maxHealth); // อัปเดต หลอดเลือด UI
    }
}