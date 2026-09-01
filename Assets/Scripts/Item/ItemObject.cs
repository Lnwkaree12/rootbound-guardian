using System.Collections;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private string itemID; // ตั้ง ID ไม่ให้ซ้ำกันใน Inspector (เช่น Key_Room1, Potion_01)
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject interactionPrompt;

    private bool isCollected = false;
    private bool isPlayerInRange = false;
    private Inventory playerInventory;
    private PlayerInputHandler inputHandler;
    private PlayerAnimation playerAnimation;

    public string ItemID => itemID;

    private void OnValidate()
    {
        // สุ่ม ID อัตโนมัติถ้ายังไม่ได้ตั้งค่าใน Inspector
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = System.Guid.NewGuid().ToString();
        }
    }

    private void Update()
    {
        if (!isCollected && isPlayerInRange && inputHandler != null && inputHandler.InteractPressed)
        {
            StartCoroutine(CollectSequence());
        }
    }

    private IEnumerator CollectSequence()
    {
        isCollected = true;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (playerAnimation != null) playerAnimation.PlayPickUpAnimation();
        if (playerInventory != null) playerInventory.AddItem(itemData);

        // ส่งสัญญาณบอก CheckpointManager ว่าไอเทมชิ้นนี้ถูกเก็บไปแล้ว
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkItemAsPicked(itemID);
        }

        yield return new WaitForSeconds(0.5f);

        // ซ่อนไอเทมแทนการ Destroy
        gameObject.SetActive(false);
    }

    public void ResetItemState(bool shouldBeActive)
    {
        isCollected = !shouldBeActive;
        gameObject.SetActive(shouldBeActive);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerInventory = other.GetComponentInParent<Inventory>();
            inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            playerAnimation = other.GetComponentInParent<PlayerAnimation>();

            if (interactionPrompt != null && !isCollected)
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
            playerAnimation = null;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
}