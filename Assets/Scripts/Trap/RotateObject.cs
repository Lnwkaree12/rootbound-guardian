using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Header("การตั้งค่าความเร็วในการหมุน")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 50f, 0f); // หมุนแกน Y 90 องศา/วินาที

    private void Update()
    {
        // หมุนรอบตัวเองตามความเร็วที่ตั้งไว้
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}