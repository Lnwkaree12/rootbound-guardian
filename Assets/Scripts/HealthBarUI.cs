using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public enum BarType { Auto, Health, Oxygen }

    [Header("Bar Settings")]
    [SerializeField] private BarType barType = BarType.Auto;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text valueText;
    [SerializeField] private string prefix = "";

    public BarType Type
    {
        get => barType;
        set => barType = value;
    }

    public Slider HealthSlider
    {
        get => healthSlider;
        set => healthSlider = value;
    }

    public Text ValueText
    {
        get => valueText;
        set => valueText = value;
    }

    public string Prefix
    {
        get => prefix;
        set => prefix = value;
    }

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (valueText == null)
            valueText = GetComponentInChildren<Text>();
    }

    private void Start()
    {
        // Fallback auto-binding: ensures the bar is updated even if Inspector event was missed
        AutoBindToPlayer();
    }

    public void AutoBindToPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("Player Variant");
        if (player == null) return;

        bool isHealth = barType == BarType.Health || 
                       (barType == BarType.Auto && (name.ToLower().Contains("hp") || name.ToLower().Contains("health")));
        bool isOxygen = barType == BarType.Oxygen || 
                       (barType == BarType.Auto && (name.ToLower().Contains("oxygen") || name.ToLower().Contains("o2")));

        if (isHealth)
        {
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
                pHealth.onHealthChanged.AddListener(UpdateHealthBar);
                UpdateHealthBar(pHealth.CurrentHealth, pHealth.MaxHealth);
            }
        }
        else if (isOxygen)
        {
            PlayerOxygen pOxygen = player.GetComponent<PlayerOxygen>();
            if (pOxygen != null)
            {
                pOxygen.onOxygenChanged.RemoveListener(UpdateHealthBar);
                pOxygen.onOxygenChanged.AddListener(UpdateHealthBar);
                UpdateHealthBar(pOxygen.CurrentOxygen, pOxygen.MaxOxygen);
            }
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (healthSlider != null)
        {
            // ตั้ง Max Value ก่อนตั้ง Value เสมอ
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(prefix)
                ? $"{currentHealth} / {maxHealth}"
                : $"{prefix}{currentHealth} / {maxHealth}";
        }

        // พิมพ์ดูค่าใน Console ว่า currentHealth มันลดลงจริงไหม
        Debug.Log($"[UI Debug] {gameObject.name} Current: {currentHealth} | Max: {maxHealth}");
    }
}