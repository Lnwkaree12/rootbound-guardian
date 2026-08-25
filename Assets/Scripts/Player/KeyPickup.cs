using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [Header("Rotation & Hover Settings")]
    [SerializeField] private float rotateSpeed = 80f;
    [SerializeField] private float floatSpeed = 2.5f;
    [SerializeField] private float floatHeight = 0.18f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Rotate the key around Y axis in world space
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        // Hover up and down smoothly using Sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if player collected the key (collides with Player tags or scripts)
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CollectKey();
            }
            else
            {
                // Fallback search
                QuestManager qm = FindObjectOfType<QuestManager>();
                if (qm != null) qm.CollectKey();
            }

            Debug.Log("[KeyPickup] Key picked up! Destroying key object.");
            
            // Destroy the key GameObject in the scene
            Destroy(gameObject);
        }
    }
}
