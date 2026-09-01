using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    // ลากกลุ่ม Interaction_Prompt ทั้งหมดมาใส่ที่นี่ใน Inspector
    [SerializeField] private GameObject interactionPrompt;

    private bool isPlayerInRange = false;
    private Inventory playerInventory;
    private PlayerInputHandler inputHandler;

    private void Awake()
    {
        // ปิด Prompt ไปก่อนในตอนเริ่มเกม
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // ตรวจสอบว่าผู้เล่นอยู่ในระยะ และมีการกดปุ่ม Interact (F) หรือไม่
        if (isPlayerInRange && inputHandler != null && inputHandler.InteractPressed)
        {
            CollectItem();
            inputHandler.ResetInteractFlag();
        }
    }

    private void CollectItem()
    {
        if (playerInventory != null)
        {
            playerInventory.AddItem(itemData);
            Destroy(gameObject); // เก็บแล้วลบออกจากฉาก
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerInventory = other.GetComponentInParent<Inventory>();
            inputHandler = other.GetComponentInParent<PlayerInputHandler>();

            // เปิด Prompt ที่มีมิติขึ้นมา
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerInventory = null;
            inputHandler = null;

            // ปิด Prompt ลงเมื่อผู้เล่นเดินออก
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
}