using System.Collections;
using UnityEngine;

public class BoulderSpawner : MonoBehaviour
{
    [Header("การตั้งค่า Spawner")]
    [SerializeField] private GameObject boulderPrefab; // Prefab ลูกหิน
    [SerializeField] private Transform spawnPoint;     // จุดปล่อยหิน (ถ้าไม่ใส่จะใช้ตำแหน่งตัวเอง)
    [SerializeField] private float spawnInterval = 3f;  // ปล่อยหินทุกๆ กี่วินาที
    [SerializeField] private bool autoStart = true;     // เริ่มปล่อยทันทีเมื่อเข้าฉาก
    [SerializeField] private float rollForce = 15f;     // แรงส่งเริ่มต้นให้หินกลิ้ง

    private Coroutine spawnCoroutine;

    private void Start()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (autoStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnBoulder();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnBoulder()
    {
        if (boulderPrefab == null || ObjectPoolManager.Instance == null) return;

        // 1. ดึงหินออกจาก Pool
        GameObject boulder = ObjectPoolManager.Instance.Spawn(boulderPrefab, spawnPoint.position, spawnPoint.rotation);

        if (boulder != null)
        {
            // 2. บังคับ Set Position, Rotation และใส่แรงกลิ้งให้ Rigidbody โดยตรง
            if (boulder.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // ผลักหินไปตามทิศทางหน้าของ spawnPoint
                rb.AddForce(spawnPoint.forward * rollForce, ForceMode.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(point.position, 0.4f);
        Gizmos.DrawRay(point.position, point.forward * 4f);
    }
}