using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 10;
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public List<ItemData> Items => items;

    // ����������ҡ�����
    public bool AddItem(ItemData newItem)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory Full!");
            return false;
        }

        items.Add(newItem);
        Debug.Log($"Picked up: {newItem.itemName}");

        // �������͡Ѻ QuestManager ������纡ح�
        if (newItem.itemType == ItemType.Key)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CollectKey();
            }
        }

        return true;
    }

    // ��ҹ������� Index 㹡�����
    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        ItemData itemToUse = items[index];

        // �¡ Logic �����ҹ����������ͧ����
        switch (itemToUse.itemType)
        {
            case ItemType.HealthPotion:
                PlayerHealth health = GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal((int)itemToUse.healAmount);
                    Debug.Log($"Used {itemToUse.itemName}, Healed {itemToUse.healAmount} HP");
                    RemoveItem(index);
                }
                break;

            case ItemType.Key:
                Debug.Log($"Used Key: {itemToUse.itemName}");

                // �������͡Ѻ QuestManager �������ح�䢻�е�
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.UseKeyOnDoor();
                }

                // ź�ح��͡�ҡ��������ѧ�ҡ��ҹ�����
                RemoveItem(index);
                break;
        }
    }

    // ź�����͡�ҡ������
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }

    // ��Ǩ�ͺ����աح�����㹡������������
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

        // ��Ǩ�ͺ���㹢����ŷ����Ŵ���աح�������� �����ѻവ QuestManager ���١��ͧ
        if (HasKey() && QuestManager.Instance != null)
        {
            QuestManager.Instance.CollectKey();
        }
    }
}