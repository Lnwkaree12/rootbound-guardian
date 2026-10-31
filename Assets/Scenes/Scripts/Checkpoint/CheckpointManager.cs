using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 lastCheckpointPosition;
    private PlayerDataSave currentSaveData;
    private bool hasCheckpoint = false;
    private bool lastCheckpointHealsOxygen = true;

    private HashSet<string> currentPickedItemIDs = new HashSet<string>();
    private HashSet<string> savedPickedItemIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MarkItemAsPicked(string itemID)
    {
        if (!currentPickedItemIDs.Contains(itemID))
        {
            currentPickedItemIDs.Add(itemID);
        }
    }

    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp = null, Inventory inventoryComp = null)
    {
        SaveCheckpoint(checkpointPos, healthComp, inventoryComp, true);
    }

    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp, Inventory inventoryComp, bool healsOxygen)
    {
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;
        lastCheckpointHealsOxygen = healsOxygen;

        if (healthComp != null || inventoryComp != null)
        {
            currentSaveData = new PlayerDataSave
            {
                savedHealth = healthComp != null ? healthComp.CurrentHealth : 100,
                savedItems = inventoryComp != null ? new List<ItemData>(inventoryComp.GetItems()) : new List<ItemData>()
            };
        }

        savedPickedItemIDs = new HashSet<string>(currentPickedItemIDs);
        Debug.Log($"[CheckpointManager] บันทึกจุดเซฟใหม่ที่ตำแหน่ง: {checkpointPos}");
    }

    // ลบจุดเซฟทิ้งถ้าตำแหน่งตรงกับต้นไม้ที่เลือดหมด
    public void InvalidateCheckpointIfMatch(Vector3 treePos)
    {
        if (hasCheckpoint && Vector3.Distance(lastCheckpointPosition, treePos) < 3f)
        {
            hasCheckpoint = false;
            currentSaveData = null;
            Debug.Log("[CheckpointManager] ⚠️ จุดเซฟปัจจุบันถูกทำลายเพราะต้นไม้หมดอายุ! ลบเซฟทิ้งแล้ว");
        }
    }

    public bool HasValidCheckpoint()
    {
        return hasCheckpoint;
    }

    public void RespawnPlayer(GameObject player)
    {
        // ตรวจสอบว่ามีจุดเซฟที่ใช้งานได้อยู่หรือไม่ หากไม่มี -> สั่ง Game Over ทันที
        if (!hasCheckpoint)
        {
            Debug.LogWarning("[CheckpointManager] ❌ ไม่มีจุดเซฟเหลืออยู่! ทำการเรียกหน้า Game Over");
            TriggerGameOverDirectly();
            return;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = lastCheckpointPosition;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            player.transform.position = lastCheckpointPosition;
        }
        Physics.SyncTransforms();

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth(lastCheckpointHealsOxygen);
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null && currentSaveData != null)
        {
            inventory.LoadSavedItems(currentSaveData.savedItems);
        }

        currentPickedItemIDs = new HashSet<string>(savedPickedItemIDs);

        ItemObject[] allItems = FindObjectsByType<ItemObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ItemObject item in allItems)
        {
            bool isAlreadyPickedBeforeSave = savedPickedItemIDs.Contains(item.ItemID);
            item.ResetItemState(!isAlreadyPickedBeforeSave);
        }
    }

    private void TriggerGameOverDirectly()
    {
        GameOverUIManager gameOverUI = FindObjectOfType<GameOverUIManager>();
        if (gameOverUI != null)
        {
            gameOverUI.TriggerGameOver("ไม่มีจุดเซฟเหลือรอด! เกมโอเวอร์");
        }
        else if (GameOverUIManager.Instance != null)
        {
            GameOverUIManager.Instance.TriggerGameOver("ไม่มีจุดเซฟเหลือรอด! เกมโอเวอร์");
        }
        else
        {
            Debug.LogError("[CheckpointManager] ไม่พบ GameOverUIManager ในฉากเพื่อแสดงหน้าจอ Game Over");
        }
    }
}