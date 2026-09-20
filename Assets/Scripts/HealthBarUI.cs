using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        // พิมพ์ดูค่าใน Console ว่า currentHealth มันลดลงจริงไหม
        Debug.Log($"[UI Debug] Current: {currentHealth} | Max: {maxHealth}");

        if (healthSlider != null)
        {
            // ตั้ง Max Value ก่อนตั้ง Value เสมอ
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
}