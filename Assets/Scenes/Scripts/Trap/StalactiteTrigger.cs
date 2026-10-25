using UnityEngine;

public class StalactiteTrigger : MonoBehaviour
{
    [SerializeField] private StalactiteTrap parentTrap;

    private void Awake()
    {
        if (parentTrap == null)
            parentTrap = GetComponentInParent<StalactiteTrap>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            if (parentTrap != null)
            {
                parentTrap.DropStalactite(); // สั่งให้หินย้อยร่วง
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }
}