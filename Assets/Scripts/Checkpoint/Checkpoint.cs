using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            Inventory inventory = other.GetComponentInParent<Inventory>();

            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SaveCheckpoint(transform.position, health, inventory);
            }
        }
    }
}