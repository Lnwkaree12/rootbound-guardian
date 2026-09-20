using System.Collections;
using UnityEngine;

public class ArrowTrap : MonoBehaviour
{
    [Header("สิ่งของและตำแหน่ง")]
    [SerializeField] private GameObject arrowPrefab; // Prefab ลูกธนู (Key สำหรับ ObjectPoolManager)
    [SerializeField] private Transform spawnPoint;   // จุดยิงลูกธนู (SpawnPoint)

    [Header("การตั้งค่าการยิง")]
    [SerializeField] private float fireRate = 2f;         // ระยะเวลาเว้นช่วงระหว่างการยิง (วินาที)
    [SerializeField] private bool isActiveOnStart = true; // ให้เริ่มยิงทันทีเมื่อเข้าเกมหรือไม่

    [Header("ระบบเสียง (Audio)")]
    [SerializeField] private AudioSource audioSource; // ตัวเล่นเสียง
    [SerializeField] private AudioClip shootSound;   // ไฟล์เสียงยิงธนู

    private Coroutine shootCoroutine;
    private WaitForSeconds waitForFireRate;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        waitForFireRate = new WaitForSeconds(fireRate);
    }

    private void OnEnable()
    {
        if (isActiveOnStart)
        {
            StartShooting();
        }
    }

    private void OnDisable()
    {
        StopShooting();
    }

    public void StartShooting()
    {
        if (shootCoroutine == null)
        {
            shootCoroutine = StartCoroutine(AutoShootLoop());
        }
    }

    public void StopShooting()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }
    }

    private IEnumerator AutoShootLoop()
    {
        while (true)
        {
            ShootArrow();
            yield return waitForFireRate;
        }
    }

    private void ShootArrow()
    {
        if (arrowPrefab == null || spawnPoint == null || ObjectPoolManager.Instance == null) return;

        // 1. ดึงลูกธนูจาก ObjectPoolManager
        GameObject arrow = ObjectPoolManager.Instance.Spawn(arrowPrefab, spawnPoint.position, spawnPoint.rotation);

        if (arrow != null)
        {
            // 2. บังคับ Set Position และ Rotation ให้ Rigidbody โดยตรงอีกที
            if (arrow.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // บังคับดันไปตามทิศทางหน้าปากกระบอกยิง
                rb.linearVelocity = spawnPoint.forward * 30f;
            }
        }

        PlayShootSound();
    }

    private void PlayShootSound()
    {
        if (shootSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(shootSound, spawnPoint.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform pointToDraw = (spawnPoint != null) ? spawnPoint : transform;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(pointToDraw.position, pointToDraw.forward * 5f);
        Gizmos.DrawWireSphere(pointToDraw.position, 0.15f);
    }
}