using UnityEngine;

public class TreeSavePoint : MonoBehaviour
{
    [Header("Tree Status")]
    [SerializeField] private bool isRestored = false; // สถานะว่าต้นไม้ถูกฟื้นฟูหรือยัง
    private bool isPlayerInRange = false;

    [Header("UI Prompts (Optional)")]
    [SerializeField] private GameObject interactPromptUI;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject deadTreeVisual;
    [SerializeField] private GameObject restoredTreeVisual;

    private Transform playerTransform;
    private PlayerInputHandler inputHandler;

    private void Start()
    {
        UpdateTreeVisual();
    }

    private void Update()
    {
        if (!isPlayerInRange || inputHandler == null) return;

        if (inputHandler.InteractPressed)
        {
            if (!isRestored)
            {
                RestoreTree();
            }
            else
            {
                SaveGame();
            }
        }
    }

    private void RestoreTree()
    {
        isRestored = true;
        Debug.Log("[Tree Save Point] ฟื้นฟูต้นไม้สำเร็จ! สามารถกด E เพื่อบันทึกเกมได้แล้ว");

        UpdateTreeVisual();

        if (playerTransform != null)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.UpdateSafeZoneState(true);
            }
        }
    }

    private void SaveGame()
    {
        if (CheckpointManager.Instance != null && playerTransform != null)
        {
            PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
            Inventory inventory = playerTransform.GetComponent<Inventory>();

            // เรียก SaveCheckpoint พร้อมส่งตำแหน่ง, ค่าเลือด และของในกระเป๋า
            CheckpointManager.Instance.SaveCheckpoint(transform.position, health, inventory);
            Debug.Log("[Save System] บันทึกข้อมูล Checkpoint เรียบร้อยแล้ว!");
        }
    }

    private void UpdateTreeVisual()
    {
        if (deadTreeVisual != null) deadTreeVisual.SetActive(!isRestored);
        if (restoredTreeVisual != null) restoredTreeVisual.SetActive(isRestored);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
            inputHandler = other.GetComponent<PlayerInputHandler>();

            if (interactPromptUI != null) interactPromptUI.SetActive(true);

            if (!isRestored)
            {
                Debug.Log("กด [Interact/E] เพื่อฟื้นฟูต้นไม้");
            }
            else
            {
                Debug.Log("กด [Interact/E] เพื่อบันทึกเกม");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;
            inputHandler = null;

            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }
    }
}