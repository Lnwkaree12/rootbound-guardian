using UnityEngine;

public class LadderTopEntry : MonoBehaviour
{
    [Header("Top Entry Settings")]
    [Tooltip("ลากตัววัตถุ Ladder หลักมาใส่ช่องนี้")]
    [SerializeField] private Transform ladderTransform;
    [SerializeField] private bool alignToX = true;

    [Header("Offset (ปรับตำแหน่งตอนเริ่มเกาะ)")]
    [Tooltip("ปรับระยะห่างไม่ให้ตัวละครจมเข้าไปในบันได เช่น ขยับออกมาด้านหน้าเล็กน้อย")]
    [SerializeField] private Vector3 entryOffset = new Vector3(0f, 0f, 0.5f);

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player == null) return;

            // ถ้าตัวละครไม่ได้กำลังปีนอยู่ และผู้เล่นกดปุ่ม S เพื่อจะปีนลง
            if (!player.IsClimbing && Input.GetKey(KeyCode.S))
            {
                // คำนวณตำแหน่งที่จะให้ตัวละครไปเกาะ โดยอ้างอิงจากตำแหน่งบันได + ระยะ Offset ที่ตั้งไว้
                Vector3 targetPos = (ladderTransform != null) ? ladderTransform.position : transform.position;
                targetPos += entryOffset; // ป้องกันตัวละครติดฝั่งผิดฝั่งหรือจมในบันได

                player.EnableClimbing(true, targetPos, alignToX);
            }
        }
    }
}