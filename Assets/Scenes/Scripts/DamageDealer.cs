using UnityEngine;
using UnityEngine.Events;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float wallHitDelay = 0.05f; // หน่วงเวลาเช็คกำแพง 0.05 วินาทีหลังเกิด

    public UnityEvent onHitPlayer;

    private float spawnTime;

    private void OnEnable()
    {
        spawnTime = Time.time; // บันทึกเวลาที่ถูกดึงออกจาก Pool
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleDamage(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleDamage(collision.gameObject);
    }

    private void HandleDamage(GameObject target)
    {
        // 1. เช็คว่าเป็น Player หรือไม่ (ความเสียหายโดนได้ทันที)
        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            onHitPlayer?.Invoke();
            return;
        }

        // 2. ถ้าเป็น Wall ให้เช็คก่อนว่าเกิดมานานพอหรือยัง (กันกรณีSpawnมาซ้อนกำแพง)
        if (Time.time - spawnTime < wallHitDelay) return;

        if (target.CompareTag("Wall") || target.transform.root.CompareTag("Wall"))
        {
            onHitPlayer?.Invoke();
        }
    }
}