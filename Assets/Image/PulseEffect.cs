using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    [Header("Settings")]
    public float pulseSpeed = 2.0f;  // ความเร็วในการขยาย
    public float maxScale = 1.1f;    // ขนาดใหญ่ที่สุด (1.1 คือ 110%)
    public float minScale = 0.9f;    // ขนาดเล็กที่สุด (0.9 คือ 90%)

    private Vector3 initialScale;

    void Start()
    {
        // เก็บค่าขนาดเริ่มต้นไว้
        initialScale = transform.localScale;
    }

    void Update()
    {
        // ใช้ฟังก์ชัน Sin เพื่อสร้างค่าที่แกว่งไปมา -1 ถึง 1
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed);

        // แปลงค่าให้อยู่ในช่วง minScale ถึง maxScale
        float currentScale = Mathf.Lerp(minScale, maxScale, (scaleOffset + 1f) / 2f);

        // นำไปคูณกับขนาดเริ่มต้น
        transform.localScale = initialScale * currentScale;
    }
}