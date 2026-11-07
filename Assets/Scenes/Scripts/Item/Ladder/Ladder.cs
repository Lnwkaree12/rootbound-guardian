using UnityEngine;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Alignment Direction")]
    [Tooltip("ติ๊กถูกถ้าบันไดขนานหน้าฉาก (ล็อกแกน X), เอาออกถ้าเป็นบันไดด้านข้าง (ล็อกแกน Z)")]
    [SerializeField] private bool alignToX = true;

    [Header("Side/Direction Settings")]
    [Tooltip("เปิดใช้งานเพื่อให้บันไดปีนได้เฉพาะฝั่งที่หันหน้าเข้าหาเท่านั้น")]
    [SerializeField] private bool restrictToOneSide = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                if (restrictToOneSide)
                {
                    Vector3 dirToPlayer = (other.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, dirToPlayer);

                    // 🔍 พิมพ์เช็คค่า Dot ใน Console (ถ้าติดลบแปลว่าอยู่ด้านหลัง จะไม่ให้ปีน)
                    Debug.Log("Ladder Dot Value: " + dot);

                    if (dot < 0f) return;
                }

                player.EnableClimbing(true, transform.position, alignToX);
            }
        }
    }
}