using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Data")]
    public int potionCount = 3;
    public int maxPotionCount = 5;
    public float healAmount = 35f;

    [Header("References")]
    private PlayerStats playerStats;
    public GameObject inventoryPanel;
    public Image[] slotImages = new Image[5];       // 5 slots in backpack
    public Text[] slotQtyTexts = new Text[5];      // Quantity texts for slots
    public Sprite potionSprite;

    [Header("Quick Slot HUD Potion UI")]
    public Text hudPotionQtyText;

    private bool isInventoryOpen = false;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        
        // Auto-locate UI references by name if not manually set
        FindUIReferences();

        // Hide inventory panel at start
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Load potion sprite from Assets
        if (potionSprite == null)
        {
            potionSprite = Resources.Load<Sprite>("HUD_Potion");
            if (potionSprite == null)
            {
                potionSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/HUD_Potion.png");
            }
        }

        UpdateInventoryUI();
    }

    void Update()
    {
        // Toggle Inventory Panel with 'I' or 'Tab'
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        // Use Potion on 'H' or '1'
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            UsePotion();
        }
        
        // Testing inputs
        if (Input.GetKeyDown(KeyCode.P))
        {
            AddPotion();
        }
    }

    private void FindUIReferences()
    {
        // Try to locate BackpackPanel if null
        if (inventoryPanel == null)
        {
            inventoryPanel = GameObject.Find("BackpackPanel");
        }

        // Try to locate Quick Slot Text if null
        if (hudPotionQtyText == null)
        {
            GameObject qtyObj = GameObject.Find("HUD_PotionQuickSlot/QtyText");
            if (qtyObj == null) qtyObj = GameObject.Find("QtyText");
            if (qtyObj != null) hudPotionQtyText = qtyObj.GetComponent<Text>();
        }

        // Try to locate slot images and texts if null
        if (inventoryPanel != null)
        {
            Transform slotsContainer = inventoryPanel.transform.Find("SlotsContainer");
            if (slotsContainer != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Transform slotTrans = slotsContainer.Find($"Slot_{i}");
                    if (slotTrans != null)
                    {
                        if (slotImages[i] == null)
                        {
                            Transform imgTrans = slotTrans.Find("ItemImage");
                            if (imgTrans != null) slotImages[i] = imgTrans.GetComponent<Image>();
                        }
                        if (slotQtyTexts[i] == null)
                        {
                            Transform txtTrans = slotTrans.Find("QtyText");
                            if (txtTrans != null) slotQtyTexts[i] = txtTrans.GetComponent<Text>();
                        }
                    }
                }
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
            UpdateInventoryUI();
            Debug.Log("[InventoryManager] Inventory toggled: " + isInventoryOpen);
        }
    }

    public bool AddPotion()
    {
        if (potionCount < maxPotionCount)
        {
            potionCount++;
            UpdateInventoryUI();
            Debug.Log($"[Inventory] Potion added. Count: {potionCount}/{maxPotionCount}");
            return true;
        }
        Debug.Log("[Inventory] Cannot add potion, inventory is full!");
        return false;
    }

    public void UsePotion()
    {
        if (potionCount > 0)
        {
            if (playerStats != null && playerStats.currentHP < playerStats.maxHP)
            {
                potionCount--;
                playerStats.Heal(healAmount);
                UpdateInventoryUI();
                Debug.Log($"[Inventory] Consumed 1 Potion. Potions remaining: {potionCount}");
            }
            else if (playerStats != null && playerStats.currentHP >= playerStats.maxHP)
            {
                Debug.Log("[Inventory] HP is already full!");
            }
        }
        else
        {
            Debug.Log("[Inventory] No potions remaining!");
        }
    }

    public void UpdateInventoryUI()
    {
        // Update Quick HUD UI
        if (hudPotionQtyText != null)
        {
            hudPotionQtyText.text = $"x{potionCount}";
        }

        // Update Backpack Slots
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            if (i < potionCount)
            {
                // Slot has potion
                slotImages[i].sprite = potionSprite;
                slotImages[i].enabled = true;
                slotImages[i].color = Color.white;
                
                if (i < slotQtyTexts.Length && slotQtyTexts[i] != null)
                {
                    slotQtyTexts[i].text = "1";
                    slotQtyTexts[i].enabled = true;
                }
            }
            else
            {
                // Slot is empty
                slotImages[i].sprite = null;
                slotImages[i].enabled = false;
                
                if (i < slotQtyTexts.Length && slotQtyTexts[i] != null)
                {
                    slotQtyTexts[i].text = "";
                    slotQtyTexts[i].enabled = false;
                }
            }
        }
    }
}
