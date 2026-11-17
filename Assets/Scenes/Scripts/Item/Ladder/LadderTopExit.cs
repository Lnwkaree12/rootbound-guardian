using UnityEngine;

public class LadderTopExit : MonoBehaviour
{
    [Header("Exit Force Settings")]
    [SerializeField] private Vector3 customExitForce = new Vector3(0f, 3f, 3f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null && player.IsClimbing)
            {
                // ทำงานเฉพาะตอนที่กำลังไต่ขึ้นมาถึงยอดสุดเท่านั้น
                player.ExitClimbingWithForce(customExitForce);
            }
        }
    }
}