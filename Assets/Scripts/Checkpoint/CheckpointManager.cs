using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 lastCheckpointPosition;
    private PlayerDataSave currentSaveData;
    private bool hasCheckpoint = false;

    // ��ª�������������㹻Ѩ�غѹ
    private HashSet<string> currentPickedItemIDs = new HashSet<string>();

    // ��ª����������١�ѹ�֡��� � �ش૿����ش
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

    // ���¡����ͼ�����������
    public void MarkItemAsPicked(string itemID)
    {
        if (!currentPickedItemIDs.Contains(itemID))
        {
            currentPickedItemIDs.Add(itemID);
        }
    }

    // ���¡����ͼ����蹡�૿�������
    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp = null, Inventory inventoryComp = null)
    {
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;

        // 1. �ѹ�֡�����ż����� (����� Component ��)
        if (healthComp != null || inventoryComp != null)
        {
            currentSaveData = new PlayerDataSave
            {
                // ૿������ʹ�Ѩ�غѹ᷹ MaxHealth
                savedHealth = healthComp != null ? healthComp.CurrentHealth : 100,
                savedItems = inventoryComp != null ? new List<ItemData>(inventoryComp.GetItems()) : new List<ItemData>()
            };
        }

        // 2. ��͡��ª����������١�� � �Թҷշ��૿
        savedPickedItemIDs = new HashSet<string>(currentPickedItemIDs);

        Debug.Log($"[CheckpointManager] �ѹ�֡�ش૿����稷����˹�: {checkpointPos}");
    }

    // ���¡����ͼ����蹵�� (Respawn)
    public void RespawnPlayer(GameObject player)
    {
        if (!hasCheckpoint) return;

        // 1. ���컵���Фá�Ѻ�ش૿
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

        // 2. �׹������ʹ��С������Թ�ҧ
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth(); // �е�駤�����ʹ��� respawnHealth ��������� PlayerHealth
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null && currentSaveData != null)
        {
            inventory.LoadSavedItems(currentSaveData.savedItems);
        }

        // 3. Rollback ��ª����������١�纡�Ѻ���ҡѺ�͹૿����ش
        currentPickedItemIDs = new HashSet<string>(savedPickedItemIDs);

        // 4. �ѻവ����㹩ҡ������
        ItemObject[] allItems = FindObjectsByType<ItemObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ItemObject item in allItems)
        {
            bool isAlreadyPickedBeforeSave = savedPickedItemIDs.Contains(item.ItemID);
            item.ResetItemState(!isAlreadyPickedBeforeSave);
        }
    }
}