using UnityEngine;
using UnityEngine.Events;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float wallHitDelay = 0.05f;

    [Header("Smash Trap Settings")]
    [SerializeField] private bool checkTopSmash = true;

    public UnityEvent onHitPlayer;

    private float spawnTime;
    private BoxCollider myCollider;

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ถ้าไม่ได้ติ๊ก Is Trigger ไว้ จะไม่ทำดาเมจตามเงื่อนไข
        if (myCollider != null && !myCollider.isTrigger) return;

        HandleDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // OnCollision จะทำงานเฉพาะตอนที่ isTrigger เป็น false (ชนกันแบบกายภาพ)
        // ดังนั้นถ้าใช้หลักการนี้ OnCollision จะไม่ทำดาเมจ (ปล่อยให้ชนบล็อกเฉยๆ)
        HandleDamage(collision.collider);
    }

    private void HandleDamage(Collider other)
    {
        GameObject target = other.gameObject;
        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            if (checkTopSmash)
            {
                // เช็คแค่ Is Trigger ตามไอเดียของคุณเลยครับ!
                // ถ้า isTrigger เปิดอยู่ (กำลังทุบ) และชนผู้เล่น -> ทำดาเมจ
                if (myCollider != null && myCollider.isTrigger)
                {
                    playerHealth.TakeDamage(damageAmount);
                    onHitPlayer?.Invoke();
                    Debug.Log("[DamageDealer] ✅ หินเปิด Trigger ทับผู้เล่น ทำดาเมจสำเร็จ!");
                }
                else
                {
                    Debug.Log("[DamageDealer] 🛡️ ปิด Trigger อยู่: ชนบล็อกธรรมดา ไม่ทำดาเมจ");
                }
            }
            else
            {
                playerHealth.TakeDamage(damageAmount);
                onHitPlayer?.Invoke();
            }
            return;
        }

        if (Time.time - spawnTime < wallHitDelay) return;

        if (target.CompareTag("Wall") || target.transform.root.CompareTag("Wall"))
        {
            onHitPlayer?.Invoke();
        }
    }
}