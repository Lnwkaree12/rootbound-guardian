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

    [Header("Juicy Polish")]
    [SerializeField] private Slider ghostSlider;
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float ghostSpeed = 3f;

    private float targetValue = -1f;
    private float targetMaxValue = 100f;
    private bool initialized = false;

    public Slider GhostSlider
    {
        get => ghostSlider;
        set => ghostSlider = value;
    }

    public bool SmoothTransition
    {
        get => smoothTransition;
        set => smoothTransition = value;
    }

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

    private void Update()
    {
        if (!smoothTransition || healthSlider == null || targetValue < 0f) return;

        // Smoothly interpolate primary health slider
        if (Mathf.Abs(healthSlider.value - targetValue) > 0.05f)
        {
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, targetValue, smoothSpeed * Time.deltaTime * targetMaxValue * 0.5f);
        }
        else
        {
            healthSlider.value = targetValue;
        }

        // Smoothly interpolate ghost slider behind
        if (ghostSlider != null)
        {
            if (ghostSlider.maxValue != targetMaxValue)
                ghostSlider.maxValue = targetMaxValue;

            if (ghostSlider.value > healthSlider.value)
            {
                ghostSlider.value = Mathf.MoveTowards(ghostSlider.value, healthSlider.value, ghostSpeed * Time.deltaTime * targetMaxValue * 0.25f);
            }
            else
            {
                ghostSlider.value = healthSlider.value;
            }
        }
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

        targetMaxValue = maxHealth;
        targetValue = currentHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            if (!initialized || !smoothTransition || !Application.isPlaying)
            {
                healthSlider.value = currentHealth;
                if (ghostSlider != null)
                {
                    ghostSlider.maxValue = maxHealth;
                    ghostSlider.value = currentHealth;
                }
                initialized = true;
            }
        }

        if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(prefix)
                ? $"{currentHealth} / {maxHealth}"
                : $"{prefix}{currentHealth} / {maxHealth}";
        }

        Debug.Log($"[UI Debug] {gameObject.name} Current: {currentHealth} | Max: {maxHealth}");
    }
}