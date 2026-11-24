using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // ใช้สำหรับ URP

/// <summary>
/// จัดการค่าออกซิเจนของผู้เล่น การลด/ฟื้นฟูออกซิเจนตาม Safe Zone และเอฟเฟกต์มืด/ขอบดำบน Global Volume
/// </summary>
public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField]
    private int maxOxygen = 100;
    [SerializeField]
    private int currentOxygen = 100;

    [Header("Drain / Regen")]
    [SerializeField]
    private int drainPerTick = 5;
    [SerializeField]
    private float drainTickInterval = 1f;
    [SerializeField]
    private int regenPerTick = 10;
    [SerializeField]
    private float regenTickInterval = 0.5f;

    [Header("Low Oxygen Audio")]
    [SerializeField]
    private AudioClip lowOxygenPantingClip;
    [SerializeField, Min(0)]
    private int lowOxygenPantingThreshold = 50;
    private AudioSource lowOxygenAudioSource;
    private bool hasPlayedLowOxygenPanting;
    private bool lowOxygenAudioEnabled = true;

    [Header("Post Processing / Global Volume Settings")]
    [SerializeField]
    private Volume globalVolume;
    [Range(0f, 1f)]
    [SerializeField]
    private float dangerThreshold = 0.4f; // เริ่มแสดงเอฟเฟกต์เมื่อออกซิเจนต่ำกว่า 40%

    [SerializeField]
    private float slowDrainTickInterval = 2.5f;
    private float originalDrainInterval;

    [Header("Vignette Settings")]
    [SerializeField]
    private Color vignetteColor = Color.black;   // สีขอบมืด Vignette
    [SerializeField]
    private float vignetteSmoothness = 1f;       // ความฟุ้งของขอบ Vignette
    [SerializeField]
    private float minVignetteIntensity = 0.2f;   // ความเข้มขอบมืดตอนออกซิเจนเต็ม
    [SerializeField]
    private float maxVignetteIntensity = 0.4f;  // ความเข้มขอบมืดสูงสุดตอนออกซิเจนหมด

    [Header("Color Adjustments Settings")]
    [SerializeField]
    private float minExposure = -2.5f;           // ความมืดของภาพตอนออกซิเจนหมด

    [Header("Events")]
    public UnityEvent<int, int> onOxygenChanged; // (current, max)
    public UnityEvent onOxygenDepleted;

    private Coroutine oxygenRoutine;
    private bool isInSafeZone;
    private bool gameOverTriggered;
    private bool oxygenRegenSuppressedUntilSafeZoneExit;
    private bool isOxygenFrozen = false;

    // อ้างอิงถึง Override บน Volume Profile
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    public int CurrentOxygen => currentOxygen;
    public int MaxOxygen => maxOxygen;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        if (currentOxygen <= 0 || currentOxygen > maxOxygen)
            currentOxygen = maxOxygen;

        InitPostProcessing();
        SaveOriginalInterval();
        lowOxygenAudioSource = GetComponent<AudioSource>();
    }

    private void OnValidate()
    {
        if (currentOxygen <= 0)
            currentOxygen = maxOxygen;
    }

    private void Start()
    {
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateLowOxygenAudio();
        UpdateSafeZoneState(isInSafeZone);
        UpdatePostProcessing();
    }

    private void InitPostProcessing()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (!globalVolume.profile.TryGet(out vignette))
            {
                vignette = globalVolume.profile.Add<Vignette>();
            }

            if (!globalVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = globalVolume.profile.Add<ColorAdjustments>();
            }

            vignette.color.overrideState = true;
            vignette.color.value = vignetteColor;

            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = vignetteSmoothness;

            vignette.intensity.overrideState = true;
            vignette.intensity.value = minVignetteIntensity;

            colorAdjustments.postExposure.overrideState = true;

            vignette.active = true;
        }
        else
        {
            Debug.LogWarning("[PlayerOxygen] ยังไม่ได้ใส่ Global Volume ใน Inspector");
        }
    }

    private void UpdatePostProcessing()
    {
        if (globalVolume == null) return;

        float oxygenRatio = (float)currentOxygen / maxOxygen;
        float dangerFactor = 1f - Mathf.InverseLerp(0f, dangerThreshold, oxygenRatio);

        if (vignette != null)
        {
            vignette.color.value = vignetteColor;
            vignette.smoothness.value = vignetteSmoothness;

            float targetIntensity = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, dangerFactor);
            vignette.intensity.value = Mathf.Clamp(targetIntensity, minVignetteIntensity, maxVignetteIntensity);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, minExposure, dangerFactor);
        }
    }

    public void UpdateSafeZoneState(bool safe)
    {
        if (!safe)
        {
            oxygenRegenSuppressedUntilSafeZoneExit = false;
        }

        isInSafeZone = safe;

        if (oxygenRoutine != null)
        {
            StopCoroutine(oxygenRoutine);
        }

        if (isOxygenFrozen) return;

        if (isInSafeZone && !oxygenRegenSuppressedUntilSafeZoneExit)
        {
            oxygenRoutine = StartCoroutine(RegenRoutine());
        }
        else
        {
            oxygenRoutine = StartCoroutine(DrainRoutine());
        }
    }

    public void SetOxygenRegenSuppressedUntilSafeZoneExit(bool suppressed)
    {
        oxygenRegenSuppressedUntilSafeZoneExit = suppressed;

        if (oxygenRoutine != null)
        {
            StopCoroutine(oxygenRoutine);
            oxygenRoutine = null;
        }
    }

    private IEnumerator DrainRoutine()
    {
        while (!isInSafeZone || oxygenRegenSuppressedUntilSafeZoneExit)
        {
            yield return new WaitForSeconds(drainTickInterval);

            currentOxygen -= drainPerTick;
            currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
            onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
            UpdateLowOxygenAudio();
            UpdatePostProcessing();

            if (currentOxygen <= 0)
            {
                TriggerGameOver();
                yield break;
            }
        }
    }

    private IEnumerator RegenRoutine()
    {
        while (isInSafeZone)
        {
            if (currentOxygen < maxOxygen)
            {
                currentOxygen += regenPerTick;
                currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
                onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
                UpdateLowOxygenAudio();
                UpdatePostProcessing();
            }
            yield return new WaitForSeconds(regenTickInterval);
        }
    }

    /// <summary>
    /// ใช้ออกซิเจน เช่น ตอนกด Dash
    /// </summary>
    public void ConsumeOxygen(int amount)
    {
        if (amount <= 0 || isOxygenFrozen) return;

        currentOxygen -= amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateLowOxygenAudio();
        UpdatePostProcessing();

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

        // เปลี่ยนสถานะตัวละครเป็น Dead และหยุดแรงเคลื่อนที่ทันที
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetActionState(PlayerActionState.Dead);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayerAnimation anim = GetComponent<PlayerAnimation>();
        if (anim != null)
        {
            anim.TriggerDeath();
        }

        GameOverUIManager uiManager = GameOverUIManager.Instance;
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<GameOverUIManager>(FindObjectsInactive.Include);
        }

        if (uiManager != null)
        {
            if (!uiManager.gameObject.activeSelf)
            {
                uiManager.gameObject.SetActive(true);
            }
            uiManager.TriggerGameOver("ออกซิเจนหมด! คุณขาดอากาศหายใจ");
        }
        else
        {
            Debug.LogWarning("[PlayerOxygen] ไม่พบ GameOverUIManager ในฉากเลย!");
        }
    }

    private void SaveOriginalInterval()
    {
        if (originalDrainInterval <= 0)
        {
            originalDrainInterval = drainTickInterval;
        }
    }

    /// <summary>
    /// ตั้งค่าออกซิเจนโดยตรง และปลดล็อกแฟล็ก Game Over หากค่าออกซิเจนมากกว่า 0
    /// </summary>
    public void SetOxygen(int value)
    {
        currentOxygen = Mathf.Clamp(value, 0, maxOxygen);
        if (currentOxygen > 0)
        {
            gameOverTriggered = false;
        }
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateLowOxygenAudio();
        UpdatePostProcessing();
    }

    private void UpdateLowOxygenAudio()
    {
        if (currentOxygen > lowOxygenPantingThreshold)
        {
            StopLowOxygenPanting();
            hasPlayedLowOxygenPanting = false;
            return;
        }

        if (!lowOxygenAudioEnabled) return;
        if (hasPlayedLowOxygenPanting) return;
        hasPlayedLowOxygenPanting = true;

        if (lowOxygenPantingClip == null) return;

        if (lowOxygenAudioSource == null)
        {
            Debug.LogWarning("[PlayerOxygen] ใส่เสียงหอบแล้ว แต่ไม่พบ AudioSource บนผู้เล่น");
            return;
        }

        lowOxygenAudioSource.clip = lowOxygenPantingClip;
        lowOxygenAudioSource.loop = true;
        lowOxygenAudioSource.Play();
    }

    private void StopLowOxygenPanting()
    {
        if (lowOxygenAudioSource == null || lowOxygenAudioSource.clip != lowOxygenPantingClip) return;

        lowOxygenAudioSource.Stop();
        lowOxygenAudioSource.loop = false;
        lowOxygenAudioSource.clip = null;
    }

    public void SetLowOxygenAudioEnabled(bool enabled)
    {
        lowOxygenAudioEnabled = enabled;
        if (enabled)
        {
            UpdateLowOxygenAudio();
        }
        else
        {
            StopLowOxygenPanting();
            hasPlayedLowOxygenPanting = false;
        }
    }

    public void SetFreezeOxygen(bool frozen)
    {
        isOxygenFrozen = frozen;
        if (frozen)
        {
            if (oxygenRoutine != null)
            {
                StopCoroutine(oxygenRoutine);
                oxygenRoutine = null;
            }
            UpdatePostProcessing();
        }
        else
        {
            UpdateSafeZoneState(isInSafeZone);
        }
    }

    public void SetOxygenSlowDrain(bool isSlowed)
    {
        if (isSlowed)
        {
            drainTickInterval = slowDrainTickInterval;
        }
        else
        {
            drainTickInterval = originalDrainInterval;
        }
    }
}