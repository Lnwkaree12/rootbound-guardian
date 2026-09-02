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

        // เชื่อมต่อกับ QuestManager เมื่อเก็บกุญแจ
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

        // แยก Logic การใช้งานตามประเภทของไอเทม
        switch (itemToUse.itemType)
        {
            case ItemType.HealthPotion:
                PlayerHealth health = GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(itemToUse.healAmount);
                    Debug.Log($"Used {itemToUse.itemName}, Healed {itemToUse.healAmount} HP");
                    RemoveItem(index);
                }
                break;

            case ItemType.Key:
                Debug.Log($"Used Key: {itemToUse.itemName}");

                // เชื่อมต่อกับ QuestManager เมื่อใช้กุญแจไขประตู
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.UseKeyOnDoor();
                }

                // ลบกุญแจออกจากกระเป๋าหลังจากใช้งานสำเร็จ
                RemoveItem(index);
                break;
        }
    }

    // ลบไอเทมออกจากกระเป๋า
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }

    // ตรวจสอบว่ามีกุญแจอยู่ในกระเป๋าหรือไม่
    public bool HasKey()
    {
        return items.Exists(item => item != null && item.itemType == ItemType.Key);
    }

    public List<ItemData> GetItems()
    {
        return items;
    }

    public void LoadSavedItems(List<ItemData> savedItems)
    {
        items = new List<ItemData>(savedItems);

        // ตรวจสอบว่าในข้อมูลที่โหลดมามีกุญแจหรือไม่ เพื่ออัปเดต QuestManager ให้ถูกต้อง
        if (HasKey() && QuestManager.Instance != null)
        {
            QuestManager.Instance.CollectKey();
        }
    }
}