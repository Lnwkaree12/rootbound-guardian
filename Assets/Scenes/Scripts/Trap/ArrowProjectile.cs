using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("การตั้งค่าลูกธนู")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float maxLifetime = 5f;

    [Header("อ้างอิง Component")]
    [SerializeField] private DamageDealer damageDealer;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (damageDealer == null) damageDealer = GetComponent<DamageDealer>();
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = transform.position;
            rb.rotation = transform.rotation;
            rb.linearVelocity = transform.forward * speed;
        }

        if (damageDealer != null)
        {
            damageDealer.onHitPlayer.AddListener(ReturnToPool);
        }

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    private void OnDisable()
    {
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
            // ส่งตัวมันเอง (gameObject) เข้า Release ได้เลยทันที!
            ObjectPoolManager.Instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}