using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnImpact = false; // ติ๊กถูกถ้าเป็นลูกกระสุนที่ชนแล้วหายไป
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
       if (destroyOnImpact)
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ค้นหา PlayerHealth จากตัวที่มาชน หรือตัว Parent ของมัน
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }

    // กรณีที่ใช้ Collision (Collider ไม่ได้ติ๊ก Is Trigger)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage);

            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }
}