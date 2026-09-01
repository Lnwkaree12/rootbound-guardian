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
                    // เปลี่ยนเป็น healAmount ให้ตรงกับ ItemData.cs
                    health.Heal(itemToUse.healAmount);
                    Debug.Log($"Used {itemToUse.itemName}, Healed {itemToUse.healAmount} HP");
                    RemoveItem(index);
                }
                break;

            case ItemType.Key:
                Debug.Log($"Used Key: {itemToUse.itemName}");
                // สามารถสั่งเปิดประตู หรือส่ง Event ไปที่ระบบประตูได้ตรงนี้
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

    public List<ItemData> GetItems()
    {
        return items; // สมมติว่าใน Inventory คุณใช้ List<ItemData> items
    }

    public void LoadSavedItems(List<ItemData> savedItems)
    {
        items = new List<ItemData>(savedItems);
        //UpdateUI(); // เรียกฟังก์ชันอัปเดตหน้าจอ UI กระเป๋าของคุณ (ถ้ามี)
    }
}