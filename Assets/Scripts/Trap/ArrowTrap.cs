using UnityEngine;
using System.Collections;

public class ArrowTrap : MonoBehaviour
{
    [Header("สิ่งของและตำแหน่ง")]
    [SerializeField] private GameObject arrowPrefab; // Prefab ลูกธนู
    [SerializeField] private Transform spawnPoint;   // จุดยิงลูกธนู (SpawnPoint)

    [Header("การตั้งค่าการยิง")]
    [SerializeField] private float shootForce = 30f;      // ความเร็ว/แรงส่งของลูกธนู
    [SerializeField] private float fireRate = 2f;         // ระยะเวลาเว้นช่วงระหว่างการยิง (วินาที)
    [SerializeField] private float arrowLifetime = 5f;     // ทำลายลูกธนูทิ้งหลังจากกี่วินาที (กันเกมกระตุก)

    private void Start()
    {
        // เริ่มทำงานยิงอัตโนมัติซ้ำๆ
        StartCoroutine(AutoShootLoop());
    }

    private IEnumerator AutoShootLoop()
    {
        while (true)
        {
            ShootArrow();
            // รอเวลาตามค่า fireRate ก่อนยิงนัดถัดไป
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void ShootArrow()
    {
        if (arrowPrefab == null || spawnPoint == null) return;

        // 1. สร้างลูกธนูขึ้นมาจากตำแหน่ง และ ทิศทาง ของ spawnPoint
        GameObject arrow = Instantiate(arrowPrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. ดึง Rigidbody ของลูกธนูแล้วเพิ่มแรงส่งไปทิศทางด้านหน้า (forward)
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = spawnPoint.forward * shootForce; // ถ้าใช้ Unity เวอร์ชั่นเก่ากว่า 2023.3 ให้ใช้ rb.velocity
        }

        // 3. สั่งทำลายลูกธนูอัตโนมัติเมื่อผ่านไปตามเวลาที่ตั้งไว้ เพื่อไม่ให้รก Scene
        Destroy(arrow, arrowLifetime);
    }

    // วาดเส้นแนวการยิงในหน้า Scene เพื่อให้ตั้งทิศทางง่ายขึ้น
    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 5f);
        }
    }
}