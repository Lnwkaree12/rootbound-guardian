using UnityEngine;
using UnityEngine.Events;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;

    // ส่ง Event ออกไปเมื่อชนโดน Player
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
        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            onHitPlayer?.Invoke(); // แจ้งว่าชนโดน Player แล้วนะ
        }
    }
}