using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class TreeCooldownUI : MonoBehaviour
{
    [Header("Slider & Visual Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image bgImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image glossImage;
    [SerializeField] private Image badgeIcon;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Color States")]
    [SerializeField] private Color normalColor = new Color(0.35f, 0.88f, 0.45f, 1f);      // Emerald Forest Green
    [SerializeField] private Color warningColor = new Color(1.00f, 0.78f, 0.28f, 1f);     // Warm Honey Gold
    [SerializeField] private Color criticalColor = new Color(0.96f, 0.34f, 0.28f, 1f);    // Soft Coral Red

    [Header("Floating & Animation")]
    [SerializeField] private float floatSpeed = 2.6f;
    [SerializeField] private float floatHeight = 0.08f;
    [SerializeField] private float wobbleAngle = 0f;

    private Camera targetCamera;
    private TreeSavePoint treeSavePoint;
    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private Vector3 badgeInitialLocalPos;

    private void Awake()
    {
        treeSavePoint = GetComponentInParent<TreeSavePoint>();
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;

        if (badgeIcon != null) badgeInitialLocalPos = badgeIcon.transform.localPosition;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (healthSlider != null && fillImage == null)
        {
            if (healthSlider.fillRect != null)
            {
                fillImage = healthSlider.fillRect.GetComponent<Image>();
            }
        }
    }

    private void Start()
    {
        targetCamera = GetBestCamera();
    }

    private void OnEnable()
    {
        // Smooth fade-in pop when tree is restored
        StartCoroutine(PopInRoutine());
    }

    private IEnumerator PopInRoutine()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        transform.localScale = initialScale * 0.7f;

        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Elastic overshoot bounce
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            transform.localScale = initialScale * (t * overshoot);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        transform.localScale = initialScale;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = GetBestCamera();
            if (targetCamera == null) return;
        }

        // 1. Billboard facing active camera perfectly level with screen
        Quaternion camRot = targetCamera.transform.rotation;
        if (wobbleAngle > 0.001f)
        {
            float tilt = Mathf.Sin(Time.time * (floatSpeed * 0.8f)) * wobbleAngle;
            transform.rotation = camRot * Quaternion.Euler(0f, 0f, tilt);
        }
        else
        {
            transform.rotation = camRot;
        }

        // 2. Gentle cozy floating hover
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = initialLocalPos + new Vector3(0f, yOffset, 0f);

        // 3. Update fill color and percent text
        UpdateProgressVisuals();
    }

    private void UpdateProgressVisuals()
    {
        if (treeSavePoint == null)
        {
            treeSavePoint = GetComponentInParent<TreeSavePoint>();
        }

        if (treeSavePoint != null && !treeSavePoint.IsRestored)
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            return;
        }

        float currentVal = 100f;
        float maxVal = 100f;

        if (treeSavePoint != null)
        {
            currentVal = treeSavePoint.CurrentTreeHealth;
            maxVal = treeSavePoint.MaxTreeHealth > 0 ? treeSavePoint.MaxTreeHealth : 100f;

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxVal;
                healthSlider.value = currentVal;
            }
        }
        else if (healthSlider != null)
        {
            maxVal = healthSlider.maxValue > 0 ? healthSlider.maxValue : 100f;
            currentVal = Mathf.Clamp(healthSlider.value, 0f, maxVal);
        }
        else
        {
            return;
        }

        float ratio = Mathf.Clamp01(currentVal / maxVal);

        // Dynamic Color State
        if (fillImage != null)
        {
            if (ratio > 0.45f)
            {
                float t = (ratio - 0.45f) / 0.55f;
                fillImage.color = Color.Lerp(warningColor, normalColor, t);
            }
            else
            {
                float t = ratio / 0.45f;
                fillImage.color = Color.Lerp(criticalColor, warningColor, t);
            }
        }

        // Percentage Text
        if (percentText != null)
        {
            int p = Mathf.RoundToInt(ratio * 100f);
            percentText.text = $"{p}%";

            // Warning pulse when critical (< 25%)
            if (ratio < 0.25f)
            {
                float textPulse = 1f + Mathf.Sin(Time.time * 8f) * 0.12f;
                percentText.transform.localScale = Vector3.one * textPulse;
                percentText.color = Color.Lerp(Color.white, criticalColor, Mathf.PingPong(Time.time * 4f, 1f));
            }
            else
            {
                percentText.transform.localScale = Vector3.one;
                percentText.color = new Color(0.98f, 0.98f, 0.92f, 1f); // Cozy Ivory White
            }
        }

        // Sprout Badge cute waggle / hop
        if (badgeIcon != null)
        {
            float badgeWaggle = Mathf.Sin(Time.time * 3.5f) * 4f;
            badgeIcon.transform.localRotation = Quaternion.Euler(0f, 0f, badgeWaggle);

            if (ratio < 0.25f)
            {
                // Nervous shivering hop when low!
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 4f;
                badgeIcon.transform.localPosition = badgeInitialLocalPos + new Vector3(0f, hop, 0f);
            }
            else
            {
                badgeIcon.transform.localPosition = badgeInitialLocalPos;
            }
        }
    }

    private Camera GetBestCamera()
    {
        // 1. Check Player's attached camera first (gameplay camera)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("Player Variant");
        if (player != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>(false);
            if (playerCam != null && playerCam.isActiveAndEnabled)
            {
                return playerCam;
            }
        }

        // 2. Check Camera.main
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.isActiveAndEnabled)
        {
            return mainCam;
        }

        // 3. Fallback to any active camera
        Camera[] cams = Camera.allCameras;
        foreach (var cam in cams)
        {
            if (cam != null && cam.isActiveAndEnabled)
            {
                return cam;
            }
        }

        return null;
    }

    public void SetupReferences(Slider sl, Image fill, Image bg, Image frame, Image gloss, Image badge, TextMeshProUGUI txt)
    {
        healthSlider = sl;
        fillImage = fill;
        bgImage = bg;
        frameImage = frame;
        glossImage = gloss;
        badgeIcon = badge;
        percentText = txt;
        if (badgeIcon != null) badgeInitialLocalPos = badgeIcon.transform.localPosition;
    }
}

