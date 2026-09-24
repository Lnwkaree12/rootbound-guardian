using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Status")]
    [SerializeField] private bool isActivated = false; // เช็กว่าจุดนี้เคยถูกกดเปิดใช้งานหรือยัง
    [SerializeField] private bool healsOxygen = true; // ถ้าติ๊กจะเปิดการฟื้น Oxygen เมื่อเข้า safe zone
    [Header("Visuals (Optional)")]
    [SerializeField] private GameObject activatedVisual;

    private bool isPlayerInside = false;
    private PlayerInputHandler inputHandler;
    private GameObject playerObject;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;

    public bool IsActivated => isActivated;
    public bool HealsOxygen => healsOxygen;

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerTriggerUtility.IsPlayer(other))
        {
            isPlayerInside = true;
            playerObject = other.gameObject;

            inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            playerHealth = other.GetComponentInParent<PlayerHealth>();
            playerOxygen = other.GetComponentInParent<PlayerOxygen>();

            // ถ้าจุดนี้เคยถูกกดเปิดใช้งานไว้แล้ว (isActivated == true)
            // พอเดินเข้ามาอีกครั้ง เลือดจะหยุดลดและเริ่มฟื้นฟูทันทีโดยไม่ต้องกดปุ่มซ้ำ
            if (isActivated)
            {
                if (healsOxygen)
                {
                    if (playerOxygen != null)
                    {
                        playerOxygen.UpdateSafeZoneState(true);
                        Debug.Log("เข้าสู่ Checkpoint ที่เปิดใช้งานไว้แล้ว: เริ่มฟื้นฟู Oxygen อัตโนมัติ");
                    }
                }
                else
                {
                    Debug.Log("This checkpoint is activated but configured to NOT heal Oxygen.");
                }
            }

            // ถ้ายังไม่เคยเปิดใช้งานจุดนี้ ให้เปิดใช้งานอัตโนมัติเมื่อผู้เล่นเข้ามา
            if (!isActivated)
            {
                ActivateCheckpoint();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (PlayerTriggerUtility.IsPlayer(other))
        {
            // ออกจากเขตจุดเซฟ กลับสู่โซนก๊าซพิษ
            if (healsOxygen)
            {
                if (playerOxygen != null)
                {
                    playerOxygen.UpdateSafeZoneState(false);
                }
            }
            else
            {
                if (playerHealth != null)
                {
                    // No oxygen to turn off; keep behavior minimal
                }
            }

            isPlayerInside = false;
            playerObject = null;
            inputHandler = null;
            playerHealth = null;
            playerOxygen = null;
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

        if (activatedVisual != null)
        {
            activatedVisual.SetActive(true);
        }

        // 1. เปิด Safe Zone ให้ผู้เล่น (หยุดลดเลือด + เริ่มฟื้นฟู)
        if (healsOxygen)
        {
            if (playerOxygen != null)
            {
                playerOxygen.UpdateSafeZoneState(true);
                Debug.Log("ActivateCheckpoint: เปิดใช้งาน Safe Zone และเริ่มฟื้นฟู Oxygen");
            }
            else if (playerHealth != null)
            {
                // Fallback: nothing to do for oxygen
                Debug.Log("ActivateCheckpoint: configured to heal Oxygen but PlayerOxygen missing (fallback)");
            }
        }
        else
        {
            Debug.Log("ActivateCheckpoint: เปิดใช้งาน checkpoint แต่ไม่ได้ตั้งค่าให้ฟื้น Oxygen");
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