using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Status")]
    [SerializeField] private bool isActivated = false; // เช็กว่าจุดนี้เคยถูกกดเปิดใช้งานหรือยัง

    private bool isPlayerInside = false;
    private PlayerInputHandler inputHandler;
    private GameObject playerObject;
    private PlayerHealth playerHealth;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerObject = other.gameObject;

            inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            playerHealth = other.GetComponentInParent<PlayerHealth>();

            // ถ้าจุดนี้เคยถูกกดเปิดใช้งานไว้แล้ว (isActivated == true)
            // พอเดินเข้ามาอีกครั้ง เลือดจะหยุดลดและเริ่มฟื้นฟูทันทีโดยไม่ต้องกดปุ่มซ้ำ
            if (isActivated && playerHealth != null)
            {
                playerHealth.UpdateSafeZoneState(true);
                Debug.Log("เข้าสู่ Checkpoint ที่เปิดใช้งานไว้แล้ว: เริ่มฟื้นฟูเลือดอัตโนมัติ");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ออกจากเขตจุดเซฟ กลับสู่โซนก๊าซพิษ
            if (playerHealth != null)
            {
                playerHealth.UpdateSafeZoneState(false);
            }

            isPlayerInside = false;
            playerObject = null;
            inputHandler = null;
            playerHealth = null;
        }
    }

    private void Update()
    {
        if (!isPlayerInside) return;

        // ถ้ายังไม่เคยเปิดใช้งานจุดนี้ และผู้เล่นกด Interact
        if (!isActivated && inputHandler != null && inputHandler.InteractPressed)
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        if (playerObject == null) return;

        isActivated = true; // ล็อคสถานะว่าจุดนี้ถูกเปิดใช้งานแล้ว

        // 1. เปิด Safe Zone ให้ผู้เล่น (หยุดลดเลือด + เริ่มฟื้นฟู)
        if (playerHealth != null)
        {
            playerHealth.UpdateSafeZoneState(true);
            Debug.Log("กด Interact: เปิดใช้งาน Safe Zone ครั้งแรกสำเร็จ!");
        }

        // 2. บันทึกจุด Checkpoint
        Inventory inventory = playerObject.GetComponentInParent<Inventory>();
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SaveCheckpoint(transform.position, playerHealth, inventory);
            Debug.Log("กด Interact: บันทึกจุดเซฟเรียบร้อย!");
        }
        else
        {
            Debug.LogError("❌ หา CheckpointManager.Instance ไม่เจอใน Scene!");
        }
    }
}