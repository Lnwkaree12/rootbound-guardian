using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 10;
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public List<ItemData> Items => items;

    // เพิ่มไอเทมเข้ากระเป๋า
    public bool AddItem(ItemData newItem)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory Full!");
            return false;
        }

        items.Add(newItem);
        Debug.Log($"Picked up: {newItem.itemName}");

        // แจ้งเตือนกับ QuestManager ถ้าเก็บกุญแจ
        if (newItem.itemType == ItemType.Key)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CollectKey();
            }
        }

        return true;
    }

    // ใช้งานไอเทมตาม Index ในกระเป๋า
    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        ItemData itemToUse = items[index];

        switch (itemToUse.itemType)
        {
            case ItemType.Key:
                Debug.Log($"Used Key: {itemToUse.itemName}");

                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.UseKeyOnDoor();
                }

                RemoveItem(index);
                break;
        }
    }

    // ลบไอเทมออกจากกระเป๋าด้วย Index
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }

    // ลบไอเทมออกจากกระเป๋าโดยตรงด้วยข้อมูล ItemData
    public void RemoveItem(ItemData itemToRemove)
    {
        if (items.Contains(itemToRemove))
        {
            items.Remove(itemToRemove);
            Debug.Log($"Removed item: {itemToRemove.itemName} from inventory.");
        }
    }

    // 🌟 ฟังก์ชันพิเศษ: ค้นหาและดึง Potion ออกจากกระเป๋าโดยตรงอย่างปลอดภัย (ไม่แตะต้อง Key)
    public bool ConsumePotionDirectly()
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            // เช็คเงื่อนไขต้องเป็น Potion เท่านั้น และต้องไม่ใช่ Key
            if (item != null && item.itemType == ItemType.Potion && item.itemType != ItemType.Key && item.itemName == "Potion")
            {
                items.RemoveAt(i);
                Debug.Log("[Inventory] Potion consumed successfully for Tree Restoration.");
                return true;
            }
        }
        return false;
    }

    // ค้นหาไอเทมในกระเป๋าด้วยชื่อ
    public ItemData GetItemByName(string searchName)
    {
        return items.Find(item => item != null && item.itemName == searchName);
    }

    // ตรวจสอบว่ามีไอเทมกุญแจอยู่ในกระเป๋าหรือไม่
    public bool HasKey()
    {
        return items.Exists(item => item != null && item.itemType == ItemType.Key);
    }

    // 🌟 เช็คว่ามี Potion อยู่ในกระเป๋าจริงๆ หรือไม่ (ไม่สน QuestManager หรือ Key ใดๆ ทั้งสิ้น)
    public bool HasPotion()
    {
        foreach (var item in items)
        {
            if (item != null && item.itemType == ItemType.Potion && item.itemName == "Potion")
            {
                return true;
            }
        }
        return false;
    }

    public List<ItemData> GetItems()
    {
        return items;
    }

    public void LoadSavedItems(List<ItemData> savedItems)
    {
        items = new List<ItemData>(savedItems);

        if (HasKey() && QuestManager.Instance != null)
        {
            QuestManager.Instance.CollectKey();
        }
    }
}