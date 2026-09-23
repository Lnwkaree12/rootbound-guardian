using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private int maxOxygen = 100;
    [SerializeField] private int currentOxygen;

    [Header("Drain / Regen")]
    [SerializeField] private int drainPerTick = 5;
    [SerializeField] private float drainTickInterval = 1f;
    [SerializeField] private int regenPerTick = 10;
    [SerializeField] private float regenTickInterval = 0.5f;

    [Header("When Oxygen Depleted")]
    [SerializeField] private int healthDamagePerTickWhenDepleted = 5;
    [SerializeField] private float damageTickIntervalWhenDepleted = 1f;

    [Header("Events")]
    public UnityEvent<int, int> onOxygenChanged; // current, max
    public UnityEvent onOxygenDepleted;

    private Coroutine oxygenRoutine;
    private PlayerHealth playerHealth;
    private bool isInSafeZone = true;

    public int CurrentOxygen => currentOxygen;
    public int MaxOxygen => maxOxygen;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        currentOxygen = maxOxygen;
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateSafeZoneState(isInSafeZone);
    }

    public void UpdateSafeZoneState(bool safe)
    {
        isInSafeZone = safe;

        if (oxygenRoutine != null)
        {
            StopCoroutine(oxygenRoutine);
        }

        if (isInSafeZone)
        {
            oxygenRoutine = StartCoroutine(RegenRoutine());
        }
        else
        {
            oxygenRoutine = StartCoroutine(DrainRoutine());
        }
    }

    private IEnumerator DrainRoutine()
    {
        while (!isInSafeZone)
        {
            yield return new WaitForSeconds(drainTickInterval);

            currentOxygen -= drainPerTick;
            currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
            onOxygenChanged?.Invoke(currentOxygen, maxOxygen);

            if (currentOxygen <= 0)
            {
                onOxygenDepleted?.Invoke();

                // เมื่อออกซิเจนหมด ให้เริ่มหักเลือดตาม interval จนกว่าออกซิเจนจะขึ้นหรือตาย
                while (currentOxygen <= 0 && !isInSafeZone)
                {
                    yield return new WaitForSeconds(damageTickIntervalWhenDepleted);

                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(healthDamagePerTickWhenDepleted);
                        if (playerHealth.CurrentHealth <= 0) yield break;
                    }
                }

                // ถ้าออกซิเจนยังคง 0 แต่เราอยู่ใน safe zone หรือ player ตาย, 루프จะออก
            }
        }
    }

    private IEnumerator RegenRoutine()
    {
        while (isInSafeZone && currentOxygen < maxOxygen)
        {
            yield return new WaitForSeconds(regenTickInterval);

            currentOxygen += regenPerTick;
            currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
            onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }
    }

    // Public helpers
    public void ResetOxygen()
    {
        currentOxygen = maxOxygen;
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateSafeZoneState(true);
    }

    // Reduce oxygen immediately (e.g., when player dashes)
    public void ConsumeOxygen(int amount)
    {
        if (amount <= 0) return;

        currentOxygen -= amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);

        if (currentOxygen <= 0)
        {
            onOxygenDepleted?.Invoke();
        }
    }
}
