using UnityEngine;

public class LadderBottomExit : MonoBehaviour
{
    [Header("Bottom Exit Force Settings")]
    [Tooltip("กำหนดทิศทางและแรงพุ่งตอนลงมาถึงพื้นล่าง เช่น X: ซ้าย/ขวา, Y: ขึ้น/กระโดดออก, Z: ถอยหลังออกมา")]
    [SerializeField] private Vector3 customBottomExitForce = new Vector3(0f, 1f, -2f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // 🌟 เช็คเพิ่มเติมว่า: ตัวละครต้องกำลังปีนอยู่ และกำลังกดปุ่ม S (ไต่ลง) หรือมีความเร็วพุ่งลงข้างล่าง
                // วิธีนี้ต่อให้เดินมาชนเฉยๆ หรือกระโดดลงมาจากที่อื่น ก็จะไม่ถูกดีดมั่วซั่วครับ
                if (Input.GetKey(KeyCode.S))
                {
                    player.ExitClimbingWithForce(customBottomExitForce);
                }
            }
        }
    }
}