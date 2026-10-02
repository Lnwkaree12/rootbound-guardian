using UnityEngine;

public class TreeSavePoint : MonoBehaviour
{
    [Header("Tree Status")]
    [SerializeField] private bool isRestored = false;
    [SerializeField] private bool healsOxygen = true;

    [Header("UI Prompts (Optional)")]
    [SerializeField] private GameObject interactPromptUI;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private GameObject deadTreeVisual;
    [SerializeField] private GameObject restoredTreeVisual;

    [Header("Restoration VFX & Audio")]
    [SerializeField] private ParticleSystem restoreVfxPrefab;
    [SerializeField] private ParticleSystem restoreVfxInstance;
    [SerializeField] private ParticleSystem ambientFireflies;
    [SerializeField] private AudioClip restoreSound;
    [SerializeField] private AudioSource audioSource;

    private bool isPlayerInRange = false;
    private GameObject playerObject;
    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;

    private void Awake()
    {
        FindVFXReferences();
        FindVisualReferences();
    }

    private void Start()
    {
        FindVFXReferences();
        FindVisualReferences();
        UpdateTreeVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            SetPlayerInRange(true, other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            SetPlayerInRange(false, null);
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        return PlayerTriggerUtility.IsPlayer(other);
    }

    private void SetPlayerInRange(bool inRange, GameObject go)
    {
        isPlayerInRange = inRange;
        playerObject = inRange ? go : null;

        if (inRange && go != null)
        {
            inputHandler = go.GetComponentInParent<PlayerInputHandler>();
            playerHealth = go.GetComponentInParent<PlayerHealth>();
            playerOxygen = go.GetComponentInParent<PlayerOxygen>();
            playerInventory = go.GetComponentInParent<Inventory>();

            if (!isRestored)
            {
                SetPromptActive(true);
            }
            else
            {
                if (healsOxygen && playerOxygen != null)
                {
                    playerOxygen.UpdateSafeZoneState(true);
                }
                SaveGame();
            }
        }
        else
        {
            if (isRestored && healsOxygen && playerOxygen != null)
            {
                playerOxygen.UpdateSafeZoneState(false);
            }

            inputHandler = null;
            playerHealth = null;
            playerOxygen = null;
            playerInventory = null;

            SetPromptActive(false);
        }
    }

    private void SetPromptActive(bool active)
    {
        if (interactPromptUI == null) return;

        TreeInteractUI promptComp = interactPromptUI.GetComponent<TreeInteractUI>();
        if (promptComp != null)
        {
            if (active) promptComp.Show();
            else promptComp.Hide();
        }
        else
        {
            interactPromptUI.SetActive(active);
        }
    }

    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void Update()
    {
        // Proximity detection fallback (คำนวณเฉพาะแกน XZ ในแนวราบ ป้องกันปัญหาความสูงแกน Y ทำให้ระยะคลาดเคลื่อน)
        if (!isRestored)
        {
            if (!isPlayerInRange)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && GetHorizontalDistance(transform.position, player.transform.position) <= 3.8f)
                {
                    SetPlayerInRange(true, player);
                }
            }
            else if (playerObject != null && GetHorizontalDistance(transform.position, playerObject.transform.position) > 5.5f)
            {
                SetPlayerInRange(false, null);
            }
        }

        if (!isPlayerInRange) return;

        // กด Interact (E) เพื่อฟื้นฟูต้นไม้และบันทึกจุดเซฟ (รองรับทั้ง InputHandler และปุ่ม E ตรงๆ)
        bool interactPressed = (inputHandler != null && inputHandler.InteractPressed) || Input.GetKeyDown(KeyCode.E);
        if (!isRestored && interactPressed)
        {
            RestoreTree();
        }
    }

    public void RestoreTree()
    {
        if (isRestored) return;
        isRestored = true;

        SetPromptActive(false);
        UpdateTreeVisual();
        PlayRestoreVFX();

        if (healsOxygen && playerOxygen != null)
        {
            playerOxygen.UpdateSafeZoneState(true);
        }

        SaveGame();
    }

    private void PlayRestoreVFX()
    {
        FindVFXReferences();

        // 1. Instantiate fresh world-space VFX burst (clean, guaranteed visible, unaffected by parent state)
        if (restoreVfxPrefab != null)
        {
            GameObject burstObj = Instantiate(restoreVfxPrefab.gameObject, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            burstObj.SetActive(true);
            ParticleSystem ps = burstObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(true);
            }

            if (Application.isPlaying)
            {
                Destroy(burstObj, 5.0f);
            }
            else
            {
                DestroyImmediate(burstObj);
            }
            Debug.Log($"[TreeSavePoint] Instantiated world-space TreeRestoreVFX for: {gameObject.name}");
        }

        // 2. Play attached instance burst VFX if present
        if (restoreVfxInstance != null)
        {
            restoreVfxInstance.gameObject.SetActive(true);
            restoreVfxInstance.Play(true);
        }

        // 3. Play ambient fireflies around canopy
        if (ambientFireflies != null)
        {
            ambientFireflies.gameObject.SetActive(true);
            ambientFireflies.Play(true);
        }

        // 4. Audio Feedback
        if (restoreSound != null)
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(restoreSound);
            }
        }
    }

    private void SaveGame()
    {
        if (CheckpointManager.Instance != null)
        {
            // ใช้ Reference ที่ Cache ไว้ตั้งแต่ OnTriggerEnter
            CheckpointManager.Instance.SaveCheckpoint(transform.position, playerHealth, playerInventory);
            Debug.Log("[Tree Save Point] บันทึกจุด Checkpoint เรียบร้อย!");
        }
        else
        {
            Debug.LogError("❌ หา CheckpointManager.Instance ไม่เจอใน Scene!");
        }
    }

    private void UpdateTreeVisual()
    {
        if (deadTreeVisual != null) deadTreeVisual.SetActive(!isRestored);
        if (restoredTreeVisual != null) restoredTreeVisual.SetActive(isRestored);

        if (ambientFireflies != null)
        {
            ambientFireflies.gameObject.SetActive(isRestored);
            if (isRestored && !ambientFireflies.isPlaying)
            {
                ambientFireflies.Play(true);
            }
        }
    }

    private void FindVisualReferences()
    {
        if (deadTreeVisual == null)
        {
            var dt = transform.Find("SmallTree");
            if (dt != null) deadTreeVisual = dt.gameObject;
        }

        if (restoredTreeVisual == null)
        {
            var rt = transform.Find("SmallTree (1)");
            if (rt != null) restoredTreeVisual = rt.gameObject;
        }

        if (interactPromptUI == null)
        {
            var prompt = transform.Find("InteractUI_Canvas");
            if (prompt != null) interactPromptUI = prompt.gameObject;
        }
    }

    public void ConfigureVisuals(GameObject deadVisual, GameObject restoredVisual, GameObject promptUI)
    {
        deadTreeVisual = deadVisual;
        restoredTreeVisual = restoredVisual;
        interactPromptUI = promptUI;
    }

    private void FindVFXReferences()
    {
        if (restoreVfxInstance == null)
        {
            var burstChild = transform.Find("TreeRestoreVFX");
            if (burstChild != null)
            {
                restoreVfxInstance = burstChild.GetComponent<ParticleSystem>();
            }
        }

        if (ambientFireflies == null)
        {
            var ambChild = transform.Find("AmbientFireflies");
            if (ambChild != null)
            {
                ambientFireflies = ambChild.GetComponent<ParticleSystem>();
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Public setter for editor configuration
    public void ConfigureVFX(ParticleSystem burstInstance, ParticleSystem fireflies, AudioClip sound, ParticleSystem prefab = null)
    {
        restoreVfxInstance = burstInstance;
        ambientFireflies = fireflies;
        restoreSound = sound;
        if (prefab != null) restoreVfxPrefab = prefab;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void ResetTreeState()
    {
        isRestored = false;
        UpdateTreeVisual();
        if (restoreVfxInstance != null) restoreVfxInstance.gameObject.SetActive(false);
        if (ambientFireflies != null) ambientFireflies.gameObject.SetActive(false);
    }
}