using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [SerializeField] private Transform defaultSpawnPoint; // จุดเกิดเริ่มต้นของเกม
    private Vector3 currentCheckpointPosition;

    private void Awake()
    {
        // ทำระบบ Singleton เพื่อให้เรียกใช้ง่ายจากทุกที่
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (defaultSpawnPoint != null)
        {
            currentCheckpointPosition = defaultSpawnPoint.position;
        }
        else
        {
            currentCheckpointPosition = transform.position;
        }
    }

    // สั่งอัปเดตจุดเซฟใหม่
    public void SetCheckpoint(Vector3 newPosition)
    {
        currentCheckpointPosition = newPosition;
        Debug.Log("Checkpoint Updated: " + newPosition);
    }

    // วาร์ปตัวละครกลับไปจุดเซฟล่าสุด
    public void RespawnPlayer(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            // เรียกใช้ฟังก์ชัน Teleport ที่เราเขียนไว้ใน PlayerMovement
            movement.Teleport(currentCheckpointPosition);
        }
        else
        {
            // กรณีสำรองถ้าไม่ได้ใช้ PlayerMovement
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = currentCheckpointPosition;
            if (controller != null) controller.enabled = true;
        }
    }
}