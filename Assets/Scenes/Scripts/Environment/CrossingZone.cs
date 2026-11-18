using UnityEngine;

public class CrossingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ดึงคอมโพเนนต์ฝั่งผู้เล่นมาสั่งเปลี่ยนสถานะ
            PlayerMovement movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.SetCanTriggerEdgeBumper(true);
                Debug.Log("<color=yellow>[CrossingZone] ผู้เล่นเข้าสู่โซนกลางเหว เปิดสถานะกันชนขอบเหวแล้ว</color>");
            }
        }
    }
}