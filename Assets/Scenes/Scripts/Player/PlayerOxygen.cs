using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // ใช้สำหรับ URP

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

    [Header("Post Processing / Global Volume Settings")]
    [SerializeField]
    private Volume globalVolume;
    [Range(0f, 1f)]
    [SerializeField]
    private float dangerThreshold = 0.4f; // จะเริ่มแสดงผลเมื่อออกซิเจนต่ำกว่า 40%

    [SerializeField]
    private float slowDrainTickInterval = 2.5f;
    private float originalDrainInterval;

    [Header("Vignette Settings")]
    [SerializeField]
    private Color vignetteColor = Color.black;   // บังคับเปลี่ยนสี Vignette เป็นสีดำสนิท
    [SerializeField]
    private float vignetteSmoothness = 1f;       // ตั้งค่า Smoothness เป็น 1
    [SerializeField]
    private float minVignetteIntensity = 0.2f;   // ค่าขอบมืดตอนออกซิเจนเต็ม
    [SerializeField]
    private float maxVignetteIntensity = 0.4f;  // ค่าขอบมืดตอนออกซิเจนหมด (จำกัดไม่เกิน 0.4f)

    [Header("Color Adjustments Settings")]
    [SerializeField]
    private float minExposure = -2.5f;           // ความมืดมิดของภาพตอนออกซิเจนหมด

    [Header("Events")]
    public UnityEvent<int, int> onOxygenChanged; // current, max
    public UnityEvent onOxygenDepleted;

    private Coroutine oxygenRoutine;
    private bool isInSafeZone;
    private bool gameOverTriggered;

    // References สำหรับเก็บ Overrides จาก Volume
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
    }

    private void OnValidate()
    {
        if (currentOxygen <= 0)
            currentOxygen = maxOxygen;
    }

    private void Start()
    {
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdateSafeZoneState(isInSafeZone);
        UpdatePostProcessing(); // อัปเดตเอฟเฟกต์เริ่มต้น
    }

    private void InitPostProcessing()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            // ดึง Override Vignette มาใช้งาน
            if (!globalVolume.profile.TryGet(out vignette))
            {
                vignette = globalVolume.profile.Add<Vignette>();
            }

            // ดึง Override ColorAdjustments มาใช้งาน
            if (!globalVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = globalVolume.profile.Add<ColorAdjustments>();
            }

            // เปิดใช้งาน overrideState (ติ๊กถูก)
            vignette.color.overrideState = true;
            vignette.color.value = vignetteColor;

            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = vignetteSmoothness;

            vignette.intensity.overrideState = true;
            vignette.intensity.value = minVignetteIntensity; // บังคับคืนค่าเริ่มต้นทันทีที่เริ่มเกม

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

        // คำนวณอัตราส่วนออกซิเจน (1 = เต็ม, 0 = หมด)
        float oxygenRatio = (float)currentOxygen / maxOxygen;

        // คำนวณค่าความรุนแรงของเอฟเฟกต์
        float dangerFactor = 1f - Mathf.InverseLerp(0f, dangerThreshold, oxygenRatio);

        // ปรับขอบดำ (Vignette)
        if (vignette != null)
        {
            vignette.color.value = vignetteColor;
            vignette.smoothness.value = vignetteSmoothness;

            // ล็อกไม่ให้ค่าทะลุ maxVignetteIntensity (0.4f)
            float targetIntensity = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, dangerFactor);
            vignette.intensity.value = Mathf.Clamp(targetIntensity, minVignetteIntensity, maxVignetteIntensity);
        }

        // ปรับความมืด (Exposure)
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, minExposure, dangerFactor);
        }
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
                UpdatePostProcessing();
            }
            yield return new WaitForSeconds(regenTickInterval);
        }
    }

    public void ResetOxygen()
    {
        currentOxygen = maxOxygen;
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        UpdatePostProcessing();
        UpdateSafeZoneState(true);
    }

    public void ConsumeOxygen(int amount)
    {
        if (amount <= 0) return;

        currentOxygen -= amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        onOxygenChanged?.Invoke(currentOxygen, maxOxygen);
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

        GameOverUIManager gameOverUI = FindObjectOfType<GameOverUIManager>();
        if (gameOverUI != null)
        {
            gameOverUI.TriggerGameOver("ออกซิเจนหมด! คุณขาดอากาศหายใจ");
        }
        else if (GameOverUIManager.Instance != null)
        {
            GameOverUIManager.Instance.TriggerGameOver("ออกซิเจนหมด! คุณขาดอากาศหายใจ");
        }
        else
        {
            Debug.LogWarning("[PlayerOxygen] GameOverUIManager ไม่พบในฉาก");
        }
    }

    private void SaveOriginalInterval()
    {
        if (originalDrainInterval <= 0)
        {
            originalDrainInterval = drainTickInterval;
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