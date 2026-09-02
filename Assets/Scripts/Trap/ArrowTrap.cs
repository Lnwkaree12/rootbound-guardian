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

    [Header("ระบบเสียง (Audio)")]
    [SerializeField] private AudioSource audioSource; // ตัวเล่นเสียง (ถ้าไม่ใส่ สคริปต์จะหาให้อัตโนมัติ)
    [SerializeField] private AudioClip shootSound;   // ไฟล์เสียงยิงธนู

    private void Start()
    {
        // ถ้าไม่ได้ลาก AudioSource มาใส่ ให้ลองหาจาก GameObject ตัวนี้อัตโนมัติ
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

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

        // 3. เล่นเสียงยิงธนู
        PlayShootSound();

        // 4. สั่งทำลายลูกธนูอัตโนมัติเมื่อผ่านไปตามเวลาที่ตั้งไว้ เพื่อไม่ให้รก Scene
        Destroy(arrow, arrowLifetime);
    }

    private void PlayShootSound()
    {
        if (shootSound == null) return;

        // เล่นเสียงที่ตำแหน่ง spawnPoint (รองรับ Spatial Blend / เสียง 3D ตามระยะห่าง)
        if (audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            // หากไม่ได้ใส่ AudioSource สคริปต์จะเล่นเสียง 3D ณ จุด spawnPoint ให้ทันที
            AudioSource.PlayClipAtPoint(shootSound, spawnPoint.position);
        }
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