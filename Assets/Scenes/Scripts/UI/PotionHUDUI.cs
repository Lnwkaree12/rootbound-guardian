using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionHUDUI : MonoBehaviour
{
    public static PotionHUDUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Image potionIcon;
    [SerializeField] private Image ringFrame;
    [SerializeField] private Image auraGlow;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual Feedback Settings")]
    [SerializeField] private float glowPulseSpeed = 2.5f;
    [SerializeField] private float punchScale = 1.28f;
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.4f);

    private Inventory playerInventory;
    private InventoryManager inventoryManager;
    private PlayerStats playerStats;
    private int cachedPotionCount = -1;
    private Coroutine punchRoutine;
    private Coroutine glowFlashRoutine;
    private Vector3 initialIconScale = Vector3.one;
    private Vector3 initialGlowScale = Vector3.one;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (potionIcon != null) initialIconScale = potionIcon.transform.localScale;
        if (auraGlow != null)
        {
            initialGlowScale = auraGlow.transform.localScale;
            auraGlow.gameObject.SetActive(false);
        }

        FindPlayerReferences();
    }

    private void Start()
    {
        FindPlayerReferences();
        RefreshDisplay(true);
    }

    private void Update()
    {
        if (playerInventory == null)
        {
            FindPlayerReferences();
        }

        int currentCount = GetCurrentPotionCount();

        if (currentCount != cachedPotionCount)
        {
            bool countIncreased = (cachedPotionCount != -1 && currentCount > cachedPotionCount);
            cachedPotionCount = currentCount;
            RefreshDisplay(countIncreased);
        }

        // Quick shortcut to drink potion: H key or 1 key (or Alpha1)
        if (InputSystemInput.GetKeyDown(KeyCode.H) || InputSystemInput.GetKeyDown(KeyCode.Alpha1))
        {
            TryUsePotionFromHUD();
        }
    }

    private void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("Player Variant");

        if (player != null)
        {
            playerInventory = player.GetComponentInParent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
            playerStats = player.GetComponentInParent<PlayerStats>() ?? player.GetComponentInChildren<PlayerStats>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
        }
    }

    public int GetCurrentPotionCount()
    {
        if (playerInventory == null)
        {
            FindPlayerReferences();
        }

        if (playerInventory != null)
        {
            int invCount = playerInventory.GetPotionCount();
            // Sync with inventoryManager if present
            if (inventoryManager != null && inventoryManager.potionCount != invCount)
            {
                inventoryManager.potionCount = invCount;
            }
            return invCount;
        }

        if (inventoryManager != null)
        {
            return inventoryManager.potionCount;
        }

        return 0;
    }

    public void RefreshDisplay(bool triggerPunch = false)
    {
        int count = GetCurrentPotionCount();

        if (quantityText != null)
        {
            quantityText.text = $"x{count}";
        }

        if (potionIcon != null)
        {
            potionIcon.color = count > 0 ? fullColor : emptyColor;
        }

        if (auraGlow != null && !triggerPunch)
        {
            if (glowFlashRoutine == null)
            {
                auraGlow.gameObject.SetActive(false);
            }
        }

        if (triggerPunch && gameObject.activeInHierarchy)
        {
            if (potionIcon != null)
            {
                if (punchRoutine != null) StopCoroutine(punchRoutine);
                punchRoutine = StartCoroutine(PunchScaleRoutine());
            }
            if (auraGlow != null)
            {
                if (glowFlashRoutine != null) StopCoroutine(glowFlashRoutine);
                glowFlashRoutine = StartCoroutine(PickupCircularFlashRoutine());
            }
        }
    }

    public void TryUsePotionFromHUD()
    {
        if (playerInventory != null && playerInventory.HasPotion())
        {
            if (playerStats != null)
            {
                if (playerStats.currentHP < playerStats.maxHP)
                {
                    bool success = playerInventory.ConsumePotionDirectly();
                    if (success)
                    {
                        float heal = inventoryManager != null ? inventoryManager.healAmount : 35f;
                        playerStats.Heal(heal);
                        RefreshDisplay(false);
                        Debug.Log($"[PotionHUDUI] Player drank 1 potion (+{heal} HP). Remaining: {GetCurrentPotionCount()}");
                    }
                }
                else
                {
                    Debug.Log("[PotionHUDUI] HP is already full!");
                }
            }
        }
        else if (inventoryManager != null && inventoryManager.potionCount > 0)
        {
            inventoryManager.UsePotion();
            RefreshDisplay(false);
        }
        else
        {
            Debug.Log("[PotionHUDUI] No potions available!");
        }
    }

    private IEnumerator PunchScaleRoutine()
    {
        if (potionIcon == null) yield break;

        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Overshoot bounce
            float scaleMultiplier = Mathf.Lerp(punchScale, 1f, t);
            potionIcon.transform.localScale = initialIconScale * scaleMultiplier;
            yield return null;
        }

        potionIcon.transform.localScale = initialIconScale;
    }

    private IEnumerator PickupCircularFlashRoutine()
    {
        if (auraGlow == null) yield break;

        auraGlow.gameObject.SetActive(true);
        Color baseColor = new Color(0.28f, 0.82f, 1f, 1f);
        float totalDuration = 0.85f;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float norm = Mathf.Clamp01(elapsed / totalDuration);
            // 2 brief circular blue flashes fading out
            float pulse = Mathf.Abs(Mathf.Sin(norm * Mathf.PI * 2f));
            float fade = 1f - norm;
            Color gc = baseColor;
            gc.a = pulse * fade * 0.9f;
            auraGlow.color = gc;
            auraGlow.transform.localScale = initialGlowScale * Mathf.Lerp(0.95f, 1.24f, norm);
            yield return null;
        }

        auraGlow.transform.localScale = initialGlowScale;
        auraGlow.gameObject.SetActive(false);
        glowFlashRoutine = null;
    }

    // Public setter for Editor configuration
    public void Configure(Image icon, Image ring, Image glow, TextMeshProUGUI qtyTxt, CanvasGroup cg)
    {
        potionIcon = icon;
        ringFrame = ring;
        auraGlow = glow;
        quantityText = qtyTxt;
        canvasGroup = cg;
        if (potionIcon != null) initialIconScale = potionIcon.transform.localScale;
    }
}
