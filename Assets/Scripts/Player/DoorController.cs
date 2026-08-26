using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Panel Reference")]
    public Transform doorPanel;

    [Header("Door Settings")]
    public float openAngle = -90f; // Swing inwards
    public float openSpeed = 3.5f;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool playerInRange = false;

    void Start()
    {
        if (doorPanel == null)
        {
            doorPanel = transform.Find("DoorPanel");
        }

        if (doorPanel != null)
        {
            closedRotation = doorPanel.localRotation;
            openRotation = Quaternion.Euler(0f, openAngle, 0f) * closedRotation;
        }
    }

    void Update()
    {
        // Smoothly rotate the door panel to open/closed rotation
        if (doorPanel != null)
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            doorPanel.localRotation = Quaternion.Slerp(doorPanel.localRotation, targetRot, Time.deltaTime * openSpeed);
        }

        // If player is in range and presses F, interact!
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            TryOpenDoor();
        }
    }

    public void TryOpenDoor()
    {
        if (isOpen) return;

        // Check if player has the key
        bool hasKey = false;
        if (QuestManager.Instance != null)
        {
            hasKey = QuestManager.Instance.hasKey;
        }
        else
        {
            QuestManager qm = FindObjectOfType<QuestManager>();
            if (qm != null) hasKey = qm.hasKey;
        }

        if (hasKey)
        {
            isOpen = true;
            Debug.Log("[Door] Door unlocked and opened!");
            
            // Set collider on the door panel to trigger so the player can walk through
            if (doorPanel != null)
            {
                Collider col = doorPanel.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
        }
        else
        {
            Debug.Log("[Door] The door is locked! You need to find the key first.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            playerInRange = true;
            Debug.Log("[Door] Player in range. Press F to unlock/open.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            playerInRange = false;
        }
    }
}
