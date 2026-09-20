using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoulderTrap : MonoBehaviour
{
    //[Header("การตั้งค่าหินกลิ้ง")]
    //[SerializeField] private float rollForce = 15f;       // แรงส่งเริ่มต้นให้หินกลิ้ง
    //[SerializeField] private float maxLifetime = 5f;     // เวลาถอยหลังคืนเข้า Pool (วินาที)

    //[Header("อ้างอิง Prefab และ Component")]
    //[SerializeField] private GameObject boulderPrefabKey; // Prefab ตัวเองเพื่อเป็น Key คืนเข้า Pool
    //[SerializeField] private DamageDealer damageDealer;   // อ้างอิง DamageDealer ที่ติดอยู่บน Object เดียวกัน

    //private Rigidbody rb;

    //private void Awake()
    //{
    //    rb = GetComponent<Rigidbody>();

    //    // หากไม่ได้ลากมาใส่ใน Inspector ให้หาอัตโนมัติ
    //    if (damageDealer == null)
    //    {
    //        damageDealer = GetComponent<DamageDealer>();
    //    }
    //}

    //private void OnEnable()
    //{
    //    // 1. Reset ฟิสิกส์ให้สะอาด และใส่แรงกลิ้งใหม่
    //    if (rb != null)
    //    {
    //        rb.linearVelocity = Vector3.zero;
    //        rb.angularVelocity = Vector3.zero;
    //        rb.AddForce(transform.forward * rollForce, ForceMode.Impulse);
    //    }

    //    // 2. ลงทะเบียนรับฟัง Event เมื่อชนผู้เล่น ให้สั่ง ReturnToPool
    //    if (damageDealer != null)
    //    {
    //        damageDealer.onHitPlayer.AddListener(ReturnToPool);
    //    }

    //    // 3. ตั้งเวลาถอยหลัง คืนเข้า Pool เมื่อหมดเวลา
    //    CancelInvoke(nameof(ReturnToPool));
    //    Invoke(nameof(ReturnToPool), maxLifetime);
    //}

    //private void OnDisable()
    //{
    //    // ยกเลิกการฟัง Event เมื่อถูกปิดใช้งาน
    //    if (damageDealer != null)
    //    {
    //        damageDealer.onHitPlayer.RemoveListener(ReturnToPool);
    //    }
    //}

    //public void ReturnToPool()
    //{
    //    CancelInvoke(nameof(ReturnToPool));

    //    if (ObjectPoolManager.Instance != null && boulderPrefabKey != null)
    //    {
    //        ObjectPoolManager.Instance.Release(boulderPrefabKey, gameObject);
    //    }
    //    else
    //    {
    //        gameObject.SetActive(false);
    //    }
    //}
}