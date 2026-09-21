using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 lastCheckpointPosition;
    private PlayerDataSave currentSaveData;
    private bool hasCheckpoint = false;

    // รายชื่อไอเทมที่เก็บไปในปัจจุบัน
    private HashSet<string> currentPickedItemIDs = new HashSet<string>();

    // รายชื่อไอเทมที่ถูกบันทึกไว้ ณ จุดเซฟล่าสุด
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

    // เรียกเมื่อผู้เล่นเก็บไอเทม
    public void MarkItemAsPicked(string itemID)
    {
        if (!currentPickedItemIDs.Contains(itemID))
        {
            currentPickedItemIDs.Add(itemID);
        }
    }

    // เรียกเมื่อผู้เล่นกดเซฟที่ต้นไม้
    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp = null, Inventory inventoryComp = null)
    {
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;

        // 1. บันทึกข้อมูลผู้เล่น (ถ้าส่ง Component มา)
        if (healthComp != null || inventoryComp != null)
        {
            currentSaveData = new PlayerDataSave
            {
                // เซฟค่าเลือดปัจจุบันแทน MaxHealth
                savedHealth = healthComp != null ? healthComp.CurrentHealth : 100,
                savedItems = inventoryComp != null ? new List<ItemData>(inventoryComp.GetItems()) : new List<ItemData>()
            };
        }

        // 2. ล็อกรายชื่อไอเทมที่ถูกเก็บ ณ วินาทีที่เซฟ
        savedPickedItemIDs = new HashSet<string>(currentPickedItemIDs);

        Debug.Log($"[CheckpointManager] บันทึกจุดเซฟสำเร็จที่ตำแหน่ง: {checkpointPos}");
    }

    // เรียกเมื่อผู้เล่นตาย (Respawn)
    public void RespawnPlayer(GameObject player)
    {
        if (!hasCheckpoint) return;

        // 1. วาร์ปตัวละครกลับจุดเซฟ
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = lastCheckpointPosition;
        if (cc != null) cc.enabled = true;

        // 2. คืนค่าเลือดและกระเป๋าเดินทาง
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth(); // จะตั้งค่าเลือดตาม respawnHealth ที่ตั้งไว้ใน PlayerHealth
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null && currentSaveData != null)
        {
            inventory.LoadSavedItems(currentSaveData.savedItems);
        }

        // 3. Rollback รายชื่อไอเทมที่ถูกเก็บกลับไปเท่ากับตอนเซฟล่าสุด
        currentPickedItemIDs = new HashSet<string>(savedPickedItemIDs);

        // 4. อัปเดตไอเทมในฉากทั้งหมด
        ItemObject[] allItems = FindObjectsByType<ItemObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ItemObject item in allItems)
        {
            bool isAlreadyPickedBeforeSave = savedPickedItemIDs.Contains(item.ItemID);
            item.ResetItemState(!isAlreadyPickedBeforeSave);
        }
    }
}