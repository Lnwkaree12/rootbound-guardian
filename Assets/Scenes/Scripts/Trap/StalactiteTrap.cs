using System.Collections;
using UnityEngine;

public class StalactiteTrap : MonoBehaviour
{
    [Header("Fall Mode Settings")]
    [SerializeField] private bool useRandomDelay = false; // ติ๊กถูกเพื่อเปิดใช้งานระบบสุ่มดีเลย์
    [SerializeField] private float minDelay = 0f;          // หน่วงเวลาต่ำสุด (หรือใช้เป็นค่าคงที่เมื่อไม่ติ๊ก bool)
    [SerializeField] private float maxDelay = 1.5f;        // หน่วงเวลาสูงสุด (ใช้เฉพาะตอนติ๊ก bool)

    [Header("Fall Speed Settings")]
    [SerializeField] private float initialFallVelocity = 10f; // ความเร็วเริ่มต้นในการพุ่งลง
    [SerializeField] private float extraGravityForce = 20f;   // แรงเร่งพิเศษขณะกำลังตก

    [Header("Settings")]
    [SerializeField] private float destroyDelay = 3f; // เวลาลบทิ้งหลังตกถึงพื้น

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private bool hasFallen = false;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // ล็อกหินไว้ให้ลอยอยู่กับที่ในตอนเริ่ม
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    // ฟังก์ชันรับคำสั่งให้เตรียมร่วง (ถูกเรียกจาก Trigger Zone)
    public void DropStalactite()
    {
        if (hasFallen) return;

        // คำนวณเวลาดีเลย์ตามเงื่อนไข bool
        float delayToUse = minDelay; // ค่าเริ่มต้นถ้าไม่ติ๊ก bool จะใช้ minDelay

        if (useRandomDelay)
        {
            // ถ้าติ๊ก bool ให้สุ่มค่าระหว่าง min และ max
            delayToUse = Random.Range(minDelay, maxDelay);
        }

        // เริ่ม Coroutine หน่วงเวลาก่อนปล่อยตก
        StartCoroutine(DelayedDropRoutine(delayToUse));
    }

    private IEnumerator DelayedDropRoutine(float delay)
    {
        // รอตามเวลาดีเลย์ที่คำนวณได้
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ExecuteDrop();
    }

    private void ExecuteDrop()
    {
        if (hasFallen) return;
        hasFallen = true;

        if (rb != null)
        {
            rb.isKinematic = false; // เปิดแรงโน้มถ่วงให้หินร่วง
            rb.useGravity = true;

            // กำหนดความเร็วเริ่มต้นในการพุ่งลง
            rb.linearVelocity = Vector3.down * initialFallVelocity;
        }

        // ลบทิ้งอัตโนมัติเมื่อเวลาผ่านไป เพื่อไม่ให้ค้างในฉาก
        Destroy(gameObject, destroyDelay);
    }

    private void FixedUpdate()
    {
        // เติมแรงดึงลงล่างต่อเนื่องเพื่อเพิ่มความเร่งขณะตก
        if (hasFallen && rb != null)
        {
            rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
        }
    }
}