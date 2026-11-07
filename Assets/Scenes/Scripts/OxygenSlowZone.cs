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
        // เช็คว่า parentTree มีตัวตนและ "ต้องถูกฟื้นฟูอยู่เท่านั้น" ถึงจะสโลว์
        if (parentTree != null && parentTree.IsRestored)
        {
            PlayerOxygen oxygen = GetPlayerOxygen(other);
            if (oxygen != null && !oxygen.IsInSafeZone)
            {
                oxygen.SetOxygenSlowDrain(true);
            }
        }
        else
        {
            // ถ้าต้นไม้ยังไม่ฟื้นฟูหรือพังไปแล้ว ให้บังคับปิด Slow ทันทีแม้จะยืนอยู่ในโซน
            PlayerOxygen oxygen = GetPlayerOxygen(other);
            if (oxygen != null)
            {
                oxygen.SetOxygenSlowDrain(false);
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