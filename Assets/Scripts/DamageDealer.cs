using UnityEngine;
using UnityEngine.Events;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;

    // �� Event �͡�����ͪ�ⴹ Player
    public UnityEvent onHitPlayer;

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
        Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
        if (targetRigidbody == null)
        {
            targetRigidbody = target.GetComponentInParent<Rigidbody>();
        }

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && targetRigidbody != null)
        {
            playerHealth.TakeDamage(damageAmount);
            onHitPlayer?.Invoke(); // ����Ҫ�ⴹ Player ���ǹ�
        }

        if (target.CompareTag("Wall"))
        {
            onHitPlayer?.Invoke(); // ชนกำแพง -> ส่งกลับเข้า Pool ทันที
        }
    }
}