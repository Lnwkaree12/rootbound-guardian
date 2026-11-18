using UnityEngine;

public class ResetBumperZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Foot"))
        {
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                movement.SetCanTriggerEdgeBumper(false);
                Debug.Log("<color=orange>[ResetBumperZone] รีเซ็ตสถานะ EdgeBumper เป็น false แล้ว</color>");
            }
        }
    }
}