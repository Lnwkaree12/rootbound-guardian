using UnityEngine;

public class EdgeBumper : MonoBehaviour
{
    [Header("จุดหรือระยะที่จะให้วาร์ปถอยหลังกลับเข้ามาในแผ่นดิน")]
    [SerializeField] private float safeBackDistance = 2f; // ระยะถอยหลังหนีขอบ

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Foot"))
        {
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null && movement.CanTriggerEdgeBumper)
            {
                Rigidbody rb = other.GetComponentInParent<Rigidbody>();
                if (rb != null)
                {
                    // 1. ล้างค่าความเร็วทั้งหมดเพื่อไม่ให้แรงเหวี่ยงพาไหลต่อ
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // 2. คำนวณตำแหน่งถอยหลังตามทิศทางหน้าของ Bumper หรือดึงถอยหลังออกมาจากขอบ
                    // (ใช้ transform.forward ถอยหลังหนีขอบเหวขึ้นมาบน Terrain ฝั่งปลอดภัย)
                    Vector3 safePosition = transform.position - (transform.forward * safeBackDistance);
                    safePosition.y = rb.position.y + 0.5f; // ยกขึ้นเล็กน้อยกันจมพื้น Terrain

                    // 3. สั่งวาร์ปผ่านระบบฟิสิกส์
                    rb.MovePosition(safePosition);

                    Debug.Log("<color=cyan>[EdgeBumper] วาร์ปหนีขอบ Terrain สำเร็จ!</color>");
                    movement.SetCanTriggerEdgeBumper(false);
                }
            }
        }
    }
}