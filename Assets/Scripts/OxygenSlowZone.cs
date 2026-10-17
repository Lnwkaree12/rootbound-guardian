using UnityEngine;

public class OxygenSlowZone : MonoBehaviour
{
    [SerializeField]
    private TreeSavePoint parentTree;
    private bool isPlayerInsideSlowZone = false;

    public bool IsPlayerInsideSlowZone => isPlayerInsideSlowZone;

    private void Awake()
    {
        if (parentTree == null)
            parentTree = GetComponentInParent<TreeSavePoint>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInsideSlowZone = true;
            ApplySlowDrain(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInsideSlowZone = true;
            ApplySlowDrain(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInsideSlowZone = false;
            PlayerOxygen oxygen = GetPlayerOxygen(other);
            if (oxygen != null)
            {
                oxygen.SetOxygenSlowDrain(false);
            }
        }
    }

    private void ApplySlowDrain(Collider other)
    {
        if (parentTree != null && parentTree.IsRestored)
        {
            PlayerOxygen oxygen = GetPlayerOxygen(other);
            if (oxygen != null && !oxygen.IsInSafeZone)
            {
                oxygen.SetOxygenSlowDrain(true);
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private PlayerOxygen GetPlayerOxygen(Collider other)
    {
        if (other == null) return null;
        return other.GetComponentInParent<PlayerOxygen>();
    }
}