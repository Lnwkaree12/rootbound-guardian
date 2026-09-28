using UnityEngine;

public class TreeSavePoint : MonoBehaviour
{
    [Header("Tree Status")]
    [SerializeField] private bool isRestored = false;
    [SerializeField] private bool healsOxygen = true;

    [Header("UI Prompts (Optional)")]
    [SerializeField] private GameObject interactPromptUI;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject deadTreeVisual;
    [SerializeField] private GameObject restoredTreeVisual;

    private bool isPlayerInRange = false;
    private GameObject playerObject;
    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;

    private void Start()
    {
        UpdateTreeVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerTriggerUtility.IsPlayer(other))
        {
            isPlayerInRange = true;
            playerObject = other.gameObject;

            // Cache Reference ให้เหมือนกับ Checkpoint.cs
            inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            playerHealth = other.GetComponentInParent<PlayerHealth>();
            playerOxygen = other.GetComponentInParent<PlayerOxygen>();
            playerInventory = other.GetComponentInParent<Inventory>();

            if (!isRestored)
            {
                if (interactPromptUI != null) interactPromptUI.SetActive(true);
            }
            else
            {
                // ถ้าต้นไม้เคยฟื้นฟูแล้ว เดินกลับเข้ามาจะเซฟจุดเกิดให้อัตโนมัติทันทีเหมือน Checkpoint
                if (healsOxygen && playerOxygen != null)
                {
                    playerOxygen.UpdateSafeZoneState(true);
                }

                SaveGame();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (PlayerTriggerUtility.IsPlayer(other))
        {
            if (isRestored && healsOxygen && playerOxygen != null)
            {
                playerOxygen.UpdateSafeZoneState(false);
            }

            isPlayerInRange = false;
            playerObject = null;
            inputHandler = null;
            playerHealth = null;
            playerOxygen = null;
            playerInventory = null;

            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isPlayerInRange) return;

        // กด Interact (E) เพื่อฟื้นฟูต้นไม้และบันทึกจุดเซฟ
        if (!isRestored && inputHandler != null && inputHandler.InteractPressed)
        {
            RestoreTree();
        }
    }

    private void RestoreTree()
    {
        isRestored = true;

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        UpdateTreeVisual();

        if (healsOxygen && playerOxygen != null)
        {
            playerOxygen.UpdateSafeZoneState(true);
        }

        SaveGame();
    }

    private void SaveGame()
    {
        if (CheckpointManager.Instance != null)
        {
            // ใช้ Reference ที่ Cache ไว้ตั้งแต่ OnTriggerEnter
            CheckpointManager.Instance.SaveCheckpoint(transform.position, playerHealth, playerInventory);
            Debug.Log("[Tree Save Point] บันทึกจุด Checkpoint เรียบร้อย!");
        }
        else
        {
            Debug.LogError("❌ หา CheckpointManager.Instance ไม่เจอใน Scene!");
        }
    }

    private void UpdateTreeVisual()
    {
        if (deadTreeVisual != null) deadTreeVisual.SetActive(!isRestored);
        if (restoredTreeVisual != null) restoredTreeVisual.SetActive(isRestored);
    }
}