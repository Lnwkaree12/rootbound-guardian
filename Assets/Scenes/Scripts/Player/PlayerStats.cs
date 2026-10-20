using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Health & Stamina")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 15f;

    [Header("HUD UI References")]
    public Image hpBarImage;
    public Image staminaBarImage;

    void Start()
    {
        // Self-healing: Destroy duplicate HUD Canvases if any exist in the scene
        PlayerStats[] allStats = FindObjectsOfType<PlayerStats>();
        if (allStats.Length > 1)
        {
            foreach (PlayerStats stats in allStats)
            {
                if (stats != this && stats.gameObject.name.Contains("Canvas"))
                {
                    Debug.LogWarning("[PlayerStats] Found duplicate HUD Canvas in scene! Destroying: " + stats.gameObject.name);
                    Destroy(stats.gameObject);
                }
            }
        }

        currentHP = maxHP * 0.7f; // Start at 70% health for testing heal potions
        currentStamina = maxStamina;
        
        // Find HUD elements if not set
        FindHUDReferences();
    }

    void Update()
    {
        // Regenerate stamina
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        UpdateHUD();
        
        // Keyboard inputs for testing damage
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(15f);
        }
    }

    private void FindHUDReferences()
    {
        if (hpBarImage == null)
        {
            GameObject hpObj = GameObject.Find("HPBar");
            if (hpObj != null) hpBarImage = hpObj.GetComponent<Image>();
        }
        if (staminaBarImage == null)
        {
            GameObject stObj = GameObject.Find("StaminaBar");
            if (stObj != null) staminaBarImage = stObj.GetComponent<Image>();
        }

        // Configure Image Type to Filled for smooth bar scaling
        ConfigureBarImage(hpBarImage);
        ConfigureBarImage(staminaBarImage);
    }

    private void ConfigureBarImage(Image img)
    {
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    public void UpdateHUD()
    {
        if (hpBarImage != null)
        {
            hpBarImage.fillAmount = currentHP / maxHP;
        }
        if (staminaBarImage != null)
        {
            staminaBarImage.fillAmount = currentStamina / maxStamina;
        }
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);
        Debug.Log($"[PlayerStats] Healed {amount} HP. Current HP: {currentHP}/{maxHP}");
        UpdateHUD();
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateHUD();
            return true;
        }
        return false;
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);
        Debug.Log($"[PlayerStats] Took {amount} damage. Current HP: {currentHP}");
        UpdateHUD();
    }
}
