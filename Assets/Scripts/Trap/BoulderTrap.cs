using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoulderTrap : MonoBehaviour
{
    [Header("การตั้งค่าหินกลิ้ง")]
    [SerializeField] private float maxLifetime = 5f;     // เวลาถอยหลังคืนเข้า Pool (วินาที)

    [Header("อ้างอิง Component")]
    [SerializeField] private DamageDealer damageDealer;   // อ้างอิง DamageDealer ที่ติดอยู่บน Object เดียวกัน

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // หากไม่ได้ลากมาใส่ใน Inspector ให้หาอัตโนมัติ
        if (damageDealer == null)
        {
            damageDealer = GetComponent<DamageDealer>();
        }
    }

    private void OnEnable()
    {
        // 1. ลงทะเบียนรับฟัง Event เมื่อชนผู้เล่น ให้สั่ง ReturnToPool
        if (damageDealer != null)
        {
            damageDealer.onHitPlayer.AddListener(ReturnToPool);
        }

        // 2. ตั้งเวลาถอยหลัง คืนเข้า Pool เมื่อหมดเวลา
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    private void OnDisable()
    {
        // ยกเลิกการฟัง Event เมื่อถูกปิดใช้งาน
        if (damageDealer != null)
        {
            damageDealer.onHitPlayer.RemoveListener(ReturnToPool);
        }
    }

    public void ReturnToPool()
    {
        CancelInvoke(nameof(ReturnToPool));

        if (ObjectPoolManager.Instance != null)
        {
            // ส่งตัวมันเองเข้า Release ได้เลย ไม่ต้องพึ่ง Prefab Key อีกต่อไป
            ObjectPoolManager.Instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}