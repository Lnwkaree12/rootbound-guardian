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

    [Header("Events")]
    public UnityEvent<int, int> onOxygenChanged; // current, max
    public UnityEvent onOxygenDepleted;

    private Coroutine oxygenRoutine;
    private bool isInSafeZone = true;
    private bool gameOverTriggered;

    public int CurrentOxygen => currentOxygen;
    public int MaxOxygen => maxOxygen;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        currentOxygen = maxOxygen;
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
                TriggerGameOver();
                yield break;
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
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;

        if (oxygenRoutine != null)
        {
            StopCoroutine(oxygenRoutine);
            oxygenRoutine = null;
        }

        onOxygenDepleted?.Invoke();

        GameOverUIManager gameOverUI = FindObjectOfType<GameOverUIManager>();
        if (gameOverUI != null)
        {
            gameOverUI.TriggerGameOver();
        }
        else
        {
            Debug.LogWarning("[PlayerOxygen] GameOverUIManager ไม่พบในฉาก");
        }
    }
}
