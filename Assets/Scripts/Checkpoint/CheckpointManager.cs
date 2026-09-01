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

    // เรียกเมื่อผู้เล่นแตะจุดเซฟ
    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp, Inventory inventoryComp)
    {
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;

        // 1. บันทึกข้อมูลผู้เล่น
        currentSaveData = new PlayerDataSave
        {
            savedHealth = healthComp.MaxHealth,
            savedItems = new List<ItemData>(inventoryComp.GetItems())
        };

        // 2. ล็อกรายชื่อไอเทมที่ถูกเก็บ ณ วินาทีที่เซฟ
        savedPickedItemIDs = new HashSet<string>(currentPickedItemIDs);

        Debug.Log("บันทึกจุดเซฟเรียบร้อย!");
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
        if (health != null) health.ResetHealth();

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
            // ถ้า ID ของไอเทมชิ้นนี้อยู่ในรายการที่เซฟไปแล้ว ให้ซ่อนไว้ (false)
            // ถ้าไม่อยู่ในรายการ ให้แสดงกลับมาวางบนพื้น (true)
            bool isAlreadyPickedBeforeSave = savedPickedItemIDs.Contains(item.ItemID);
            item.ResetItemState(!isAlreadyPickedBeforeSave);
        }
    }
}