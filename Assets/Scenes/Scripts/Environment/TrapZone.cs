using UnityEngine;

public class TrapZone : MonoBehaviour
{
    [Header("Center Push Settings")]
    [Tooltip("ความแรงในการผลักตัวละครเข้าหากึ่งกลางแกนซ้าย-ขวา")]
    [SerializeField] private float pushForce = 5f;
    [Tooltip("ระยะสูงสุดที่จะทำการผลัก")]
    [SerializeField] private float maxPushDistance = 2f;

    [Header("Fall & Unstick Settings")]
    [Tooltip("ความแรงในการกระชากตัวละครให้ร่วงลงด้านล่างทันที")]
    [SerializeField] private float dropForce = 15f;
    [Tooltip("ระยะหรือแรงผลักออกด้านหน้าเล็กน้อย เพื่อไม่ให้ติดขอบหรือพื้นผิวที่ยื่นออกมา")]
    [SerializeField] private float unstickPushForward = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ดึง Rigidbody จากตัวแม่ (Player Variant)
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                // 1. 🌟 ปลดล็อคความฝืดและผลักให้ออกจากขอบที่ค้ำอยู่เล็กน้อย (เช่น ผลักไปตามแกน Z หรือทิศทางของ TrapZone)
                // ปรับทิศทาง unstick ตามแนวที่ต้องการให้ตัวละครหลุดออกจากขอบที่ยื่น
                Vector3 unstickDir = transform.forward * unstickPushForward;

                // 2. กระชากความเร็วแกน Y ให้พุ่งลงด้านล่างทันที + ใส่แรงผลักออกเพื่อไม่ให้ติดขอบ
                rb.linearVelocity = new Vector3(unstickDir.x, -dropForce, unstickDir.z);

                // 3. คำนวณตำแหน่งแกน X (ซ้าย-ขวา) เพื่อผลักเข้าหากึ่งกลาง
                float playerX = rb.position.x;
                float centerX = transform.position.x;
                float distanceX = playerX - centerX;

                if (Mathf.Abs(distanceX) <= maxPushDistance)
                {
                    float direction = (distanceX > 0f) ? -1f : 1f;
                    Vector3 pushDirection = new Vector3(direction, 0f, 0f);

                    // ออกแรงผลักเข้ากลาง
                    rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                }
            }

            // 4. ปิดการรับ Input เพื่อไม่ให้กดเดินหนี
            PlayerInputHandler inputHandler = other.GetComponentInParent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.ResetInputState();
                inputHandler.enabled = false;
            }

            Debug.Log("<color=red>[TrapZone] ปลดจากขอบ ย้ายตัวลงหลุมและผลักเข้ากลางสำเร็จ!</color>");
        }
    }
}