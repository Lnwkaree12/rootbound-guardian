using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    [Header("UI Elements")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Image dashIcon;
    [SerializeField] private Image ringFrame;
    [SerializeField] private Image readyGlow;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text keybindText;
    [SerializeField, Min(1f)] private float controllerKeybindIconScale = 1.45f;
    private Image controllerKeybindIcon;
    private RectTransform controllerKeybindIconRect;
    private bool controllerKeybindIconCreated;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);

    private bool wasOnCooldown = false;
    private float glowPulseTimer = 0f;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = Object.FindAnyObjectByType<PlayerController>();
        }

        EnsureControllerKeybindIcon();
        RefreshKeybindPrompt();

        // Configure radial fill on overlay if not set
        if (cooldownOverlay != null)
        {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
        }
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = Object.FindAnyObjectByType<PlayerController>();
        }

        RefreshKeybindPrompt();
    }

    private void OnEnable()
    {
        InputSystemInput.OnLastInputDeviceChanged += OnInputDeviceChanged;
        EnsureControllerKeybindIcon();
        RefreshKeybindPrompt();
    }

    private void OnDisable()
    {
        InputSystemInput.OnLastInputDeviceChanged -= OnInputDeviceChanged;
    }

    private void OnInputDeviceChanged(bool isController)
    {
        RefreshKeybindPrompt();
    }

    private void EnsureControllerKeybindIcon()
    {
        if (controllerKeybindIconCreated) return;

        GameObject iconObject = new GameObject("ControllerKeybindIcon");
        iconObject.transform.SetParent(transform, false);

        controllerKeybindIconRect = iconObject.AddComponent<RectTransform>();
        controllerKeybindIconRect.anchorMin = new Vector2(0.5f, 0f);
        controllerKeybindIconRect.anchorMax = new Vector2(0.5f, 0f);
        controllerKeybindIconRect.pivot = new Vector2(0.5f, 0.5f);
        controllerKeybindIconRect.anchoredPosition = new Vector2(0f, 12f);
        controllerKeybindIconRect.sizeDelta = new Vector2(72f, 72f);
        controllerKeybindIconRect.localScale = Vector3.one * controllerKeybindIconScale;

        controllerKeybindIcon = iconObject.AddComponent<Image>();
        controllerKeybindIcon.preserveAspect = true;
        controllerKeybindIcon.raycastTarget = false;
        controllerKeybindIconCreated = true;
    }

    private void RefreshKeybindPrompt()
    {
        EnsureControllerKeybindIcon();

        bool isController = InputSystemInput.IsControllerInputActive;
        Sprite sprintSprite = isController
            ? InputSystemInput.GetControllerPromptSprite(InputSystemInput.PromptAction.Sprint)
            : null;

        Transform keybindBadge = keybindText != null
            ? keybindText.transform.parent
            : transform.Find("KeybindBadge");

        if (keybindBadge != null)
        {
            keybindBadge.gameObject.SetActive(!isController);
        }

        if (controllerKeybindIcon != null)
        {
            controllerKeybindIcon.sprite = sprintSprite;
            controllerKeybindIcon.enabled = isController && sprintSprite != null;
        }
    }

    private void Update()
    {
        if (InputSystemInput.IsControllerInputActive &&
            (controllerKeybindIcon == null || controllerKeybindIcon.sprite == null))
        {
            RefreshKeybindPrompt();
        }

        if (playerController == null)
        {
            playerController = Object.FindAnyObjectByType<PlayerController>();
            if (playerController == null) return;
        }

        float timer = playerController.DashCooldownTimer;
        float total = playerController.DashCooldown;
        bool onCooldown = timer > 0.01f || playerController.IsDashing;

        // Cooldown radial fill
        if (cooldownOverlay != null)
        {
            float fill = total > 0f ? Mathf.Clamp01(timer / total) : 0f;
            cooldownOverlay.fillAmount = fill;
            cooldownOverlay.gameObject.SetActive(fill > 0.001f);
        }

        // Cooldown text countdown
        if (cooldownText != null)
        {
            if (timer > 0.05f)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = timer.ToString("0.0") + "s";
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }

        // Dim dash icon when cooling down
        if (dashIcon != null)
        {
            dashIcon.color = onCooldown ? cooldownColor : normalColor;
        }

        // Ready flash effect when cooldown finishes
        if (wasOnCooldown && !onCooldown)
        {
            glowPulseTimer = 0.4f; // Flash for 0.4s
        }
        wasOnCooldown = onCooldown;

        if (readyGlow != null)
        {
            if (glowPulseTimer > 0f)
            {
                glowPulseTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(glowPulseTimer / 0.4f);
                readyGlow.color = new Color(0.3f, 1f, 0.85f, alpha * 0.9f);
                readyGlow.gameObject.SetActive(true);
            }
            else
            {
                readyGlow.gameObject.SetActive(false);
            }
        }
    }

    // Public setter for programmatic setup
    public void Setup(PlayerController pc, Image overlay, Image icon, Image ring, Image glow, Text cdText, Text keyText)
    {
        playerController = pc;
        cooldownOverlay = overlay;
        dashIcon = icon;
        ringFrame = ring;
        readyGlow = glow;
        cooldownText = cdText;
        keybindText = keyText;
        EnsureControllerKeybindIcon();
        RefreshKeybindPrompt();
    }
}
