using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // เช็กว่าวัตถุที่มาชนคือ Player
        if (!isActivated && other.CompareTag("Player"))
        {
            isActivated = true;
            CheckpointManager.Instance.SetCheckpoint(transform.position);

            // สามารถใส่ Effect หรือเปิดไฟเสาเซฟตรงนี้ได้
        }
    }
}