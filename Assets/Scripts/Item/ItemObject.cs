using System.Collections;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private string itemID;
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip pickupSFX; // ใส่ไฟล์เสียงตอนเก็บไอเทมที่นี่
    [SerializeField][Range(0f, 1f)] private float soundVolume = 1.0f; // ปรับความดัง

    private bool isCollected = false;
    private bool isPlayerInRange = false;
    private Inventory playerInventory;
    private PlayerInputHandler inputHandler;
    private PlayerAnimation playerAnimation;

    public string ItemID => itemID;

    private void OnValidate()
    {
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

        // 🔊 เล่นเสียงเก็บไอเทม ณ ตำแหน่งของไอเทม
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, soundVolume);
        }

        if (playerInventory != null) playerInventory.AddItem(itemData);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkItemAsPicked(itemID);
        }

        yield return new WaitForSeconds(0.5f);

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