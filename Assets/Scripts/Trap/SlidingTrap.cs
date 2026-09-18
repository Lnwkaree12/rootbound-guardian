using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlidingTrap : MonoBehaviour
{
    [Header("การตั้งค่าระยะทางและการเคลื่อนที่")]
    [SerializeField] private Vector3 moveDirection = Vector3.right; // ทิศทางการเลื่อน (default: แกน X ซ้าย-ขวา)
    [SerializeField] private float distance = 5f;                   // ระยะทางที่จะเลื่อนไป-กลับ
    [SerializeField] private float speed = 3f;                      // ความเร็วในการเลื่อน

    private Vector3 startPosition;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // บังคับเป็น Kinematic เพื่อให้ควบคุมผ่านโค้ดได้อย่างเสถียร
    }

    private void Start()
    {
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // คำนวณระยะทางไป-กลับ ด้วย Mathf.PingPong
        float pingPongValue = Mathf.PingPong(Time.time * speed, distance);

        // คำนวณตำแหน่งใหม่
        Vector3 targetPosition = startPosition + (moveDirection.normalized * pingPongValue);

        // เคลื่อนที่ด้วย Rigidbody เพื่อให้ระบบ Physics ทำงานถูกต้อง (ผู้เล่นยืนบนแผ่นแล้วไม่ไถลตก)
        rb.MovePosition(targetPosition);
    }

    // ระบบส่งตัวละครให้เคลื่อนที่ไปพร้อมกับแผ่นเลื่อนเมื่อขึ้นมายืน
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    // วาดเส้นไกด์ไลน์ในหน้า Scene View เพื่อให้ปรับระยะง่ายขึ้น
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + (moveDirection.normalized * distance));
    }
}