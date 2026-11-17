using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 lastCheckpointPosition;
    private PlayerDataSave currentSaveData;
    private bool hasCheckpoint = false;
    private bool lastCheckpointHealsOxygen = true;
    private TreeSavePoint lastCheckpointTree;
    private bool lastCheckpointUsesTree;

    private HashSet<string> currentPickedItemIDs = new HashSet<string>();
    private HashSet<string> savedPickedItemIDs = new HashSet<string>();

    private HashSet<string> currentOpenedChestIDs = new HashSet<string>();
    private HashSet<string> savedOpenedChestIDs = new HashSet<string>();

    private HashSet<string> currentFallenStalactites = new HashSet<string>();
    private HashSet<string> savedFallenStalactites = new HashSet<string>();

    private bool savedHasKey = false;
    private bool savedDoorUnlocked = false;

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
        SaveCheckpoint(checkpointPos, healthComp, inventoryComp, healsOxygen, null);
    }

    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp, Inventory inventoryComp, bool healsOxygen, TreeSavePoint sourceTree)
    {
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;
        lastCheckpointHealsOxygen = healsOxygen;
        lastCheckpointTree = sourceTree;
        lastCheckpointUsesTree = sourceTree != null;

        // 🌟 บันทึกสถานะ Quest ปัจจุบัน
        if (QuestManager.Instance != null)
        {
            savedHasKey = QuestManager.Instance.hasKey;
            // หากต้องการเซฟตัวแปร doorUnlocked ด้วย สามารถเพิ่มเงื่อนไขดึงค่าได้ที่นี่
        }

        if (healthComp != null || inventoryComp != null)
        {
            currentSaveData = new PlayerDataSave
            {
                savedHealth = healthComp != null ? healthComp.CurrentHealth : 100,
                savedItems = inventoryComp != null ? new List<ItemData>(inventoryComp.GetItems()) : new List<ItemData>()
            };
        }

        savedOpenedChestIDs = new HashSet<string>(currentOpenedChestIDs);
        savedFallenStalactites = new HashSet<string>(currentFallenStalactites);
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
        if (!hasCheckpoint)
        {
            Debug.LogWarning("[CheckpointManager] ❌ ไม่มีจุดเซฟเหลืออยู่! ทำการเรียกหน้า Game Over");
            TriggerGameOverDirectly();
            return;
        }

        // 🌟 โหลดสถานะ Quest กลับคืนมาเมื่อเกิดใหม่
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadQuestState(savedHasKey, savedDoorUnlocked);
        }

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

        bool shouldHealOxygen = lastCheckpointHealsOxygen &&
            (!lastCheckpointUsesTree || (lastCheckpointTree != null && lastCheckpointTree.IsRestored));

        PlayerOxygen oxygen = player.GetComponent<PlayerOxygen>();
        oxygen?.SetOxygenRegenSuppressedUntilSafeZoneExit(!shouldHealOxygen);

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth(shouldHealOxygen);
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

        currentOpenedChestIDs = new HashSet<string>(savedOpenedChestIDs);

        // ค้นหาหีบทั้งหมดในฉากแล้วสั่งรีเซ็ตสถานะเปิด/ปิด
        ChestController[] allChests = FindObjectsByType<ChestController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ChestController chest in allChests)
        {
            bool isOpenedBeforeSave = savedOpenedChestIDs.Contains(chest.ChestID);
            chest.ResetChestState(isOpenedBeforeSave);
        }

        currentFallenStalactites = new HashSet<string>(savedFallenStalactites);

        StalactiteTrap[] allStalactites = FindObjectsByType<StalactiteTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StalactiteTrap trap in allStalactites)
        {
            bool fallenBeforeSave = savedFallenStalactites.Contains(trap.StalactiteID);
            trap.ResetStalactiteState(fallenBeforeSave);
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

    public void MarkChestAsOpened(string chestID)
    {
        if (!currentOpenedChestIDs.Contains(chestID))
        {
            currentOpenedChestIDs.Add(chestID);
        }
    }

    public bool IsChestOpened(string chestID)
    {
        return savedOpenedChestIDs.Contains(chestID) || currentOpenedChestIDs.Contains(chestID);
    }

    public void MarkStalactiteAsFallen(string stalactiteID)
    {
        if (!currentFallenStalactites.Contains(stalactiteID))
        {
            currentFallenStalactites.Add(stalactiteID);
        }
    }
}