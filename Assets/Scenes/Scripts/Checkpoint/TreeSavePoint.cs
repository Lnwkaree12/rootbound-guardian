using System.Collections;
using UnityEngine;

public class TreeSavePoint : MonoBehaviour
{
    [Header("Tree Status")]
    [SerializeField] private bool isRestored = false;
    [SerializeField] private bool healsOxygen = true;

    [Header("Tree Health / Decay Settings")]
    [SerializeField] private float maxTreeHealth = 100f;
    [SerializeField] private float currentTreeHealth;
    [SerializeField] private float healthDecreaseRate = 5f; // ลดลง 5 หน่วยต่อวินาที (ปรับแต่งได้)

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

    public bool IsRestored => isRestored;

    private bool isPlayerInRange = false;
    private GameObject playerObject;
    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;
    private Coroutine treeDecayRoutine;

    private void Awake()
    {
        currentTreeHealth = maxTreeHealth;
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
            SetPlayerInRange(false, other.gameObject);
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

        if (inRange && go != null)
        {
            playerObject = go;
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
                    playerOxygen.SetOxygenSlowDrain(false);
                    playerOxygen.UpdateSafeZoneState(true);
                }
                SaveGame();
            }
        }
        else
        {
            PlayerOxygen targetOxygen = playerOxygen;
            if (targetOxygen == null && go != null)
            {
                targetOxygen = go.GetComponentInParent<PlayerOxygen>();
            }

            if (isRestored && healsOxygen && targetOxygen != null)
            {
                targetOxygen.UpdateSafeZoneState(false);

                OxygenSlowZone slowZone = GetComponentInChildren<OxygenSlowZone>();
                if (slowZone != null && slowZone.IsPlayerInsideSlowZone)
                {
                    playerOxygen.SetOxygenSlowDrain(true);
                }
            }

            playerObject = null;
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

    private void Update()
    {
        if (!isPlayerInRange) return;

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
        currentTreeHealth = maxTreeHealth; // รีเซ็ตเลือดต้นไม้ให้เต็ม

        SetPromptActive(false);
        UpdateTreeVisual();
        PlayRestoreVFX();

        if (playerObject != null && playerOxygen == null)
        {
            playerOxygen = playerObject.GetComponentInParent<PlayerOxygen>();
        }

        if (healsOxygen && playerOxygen != null)
        {
            playerOxygen.SetOxygenSlowDrain(false);
            playerOxygen.UpdateSafeZoneState(true);
        }

        SaveGame();

        // เริ่มต้นกระบวนการลดเลือดของต้นไม้
        if (treeDecayRoutine != null) StopCoroutine(treeDecayRoutine);
        treeDecayRoutine = StartCoroutine(TreeDecayRoutine());
    }

    private IEnumerator TreeDecayRoutine()
    {
        while (isRestored && currentTreeHealth > 0)
        {
            currentTreeHealth -= healthDecreaseRate * Time.deltaTime;
            currentTreeHealth = Mathf.Max(0f, currentTreeHealth);

            if (currentTreeHealth <= 0f)
            {
                CollapseTree();
                yield break;
            }
            yield return null;
        }
    }

    private void CollapseTree()
    {
        isRestored = false;
        currentTreeHealth = 0f;
        UpdateTreeVisual();

        if (restoreVfxInstance != null) restoreVfxInstance.gameObject.SetActive(false);
        if (ambientFireflies != null) ambientFireflies.gameObject.SetActive(false);

        // ถ้าผู้เล่นยังอยู่ในรัศมีต้นไม้ตอนที่มันพัง
        if (isPlayerInRange && playerOxygen != null)
        {
            // 1. บังคับปิดสถานะ Slow Drain ทันที เพื่อดึงค่า originalDrainInterval กลับมา
            playerOxygen.SetOxygenSlowDrain(false);

            // 2. สั่งออกจาก Safe Zone เพื่อให้ระบบเริ่มจับเวลาลดออกซิเจนใหม่ด้วยความเร็วปกติ
            playerOxygen.UpdateSafeZoneState(false);
        }

        // แจ้ง CheckpointManager ว่าจุดเซฟนี้ถูกทำลายแล้ว ให้ลบเซฟทิ้งถ้าเป็นจุดล่าสุด
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.InvalidateCheckpointIfMatch(transform.position);
        }

        Debug.Log($"[TreeSavePoint] ต้นไม้ {gameObject.name} พลังชีวิตหมดและกลับคืนสภาพเดิม จุดเซฟถูกลบ!");
    }

    private void PlayRestoreVFX()
    {
        FindVFXReferences();

        if (restoreVfxPrefab != null)
        {
            GameObject burstObj = Instantiate(restoreVfxPrefab.gameObject, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            burstObj.SetActive(true);
            ParticleSystem ps = burstObj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);

            if (Application.isPlaying) Destroy(burstObj, 5.0f);
            else DestroyImmediate(burstObj);
        }

        if (restoreVfxInstance != null)
        {
            restoreVfxInstance.gameObject.SetActive(true);
            restoreVfxInstance.Play(true);
        }

        if (ambientFireflies != null)
        {
            ambientFireflies.gameObject.SetActive(true);
            ambientFireflies.Play(true);
        }

        if (restoreSound != null)
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null) audioSource.PlayOneShot(restoreSound);
        }
    }

    private void SaveGame()
    {
        if (CheckpointManager.Instance != null)
        {
            Vector3 checkpointPosition = playerObject != null ? playerObject.transform.root.position : transform.position;
            CheckpointManager.Instance.SaveCheckpoint(checkpointPosition, playerHealth, playerInventory, healsOxygen);
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
            if (isRestored && !ambientFireflies.isPlaying) ambientFireflies.Play(true);
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

    private void FindVFXReferences()
    {
        if (restoreVfxInstance == null)
        {
            var burstChild = transform.Find("TreeRestoreVFX");
            if (burstChild != null) restoreVfxInstance = burstChild.GetComponent<ParticleSystem>();
        }

        if (ambientFireflies == null)
        {
            var ambChild = transform.Find("AmbientFireflies");
            if (ambChild != null) ambientFireflies = ambChild.GetComponent<ParticleSystem>();
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void ConfigureVFX(ParticleSystem burstInstance, ParticleSystem fireflies, AudioClip sound, ParticleSystem prefab = null)
    {
        restoreVfxInstance = burstInstance;
        ambientFireflies = fireflies;
        restoreSound = sound;
        if (prefab != null) restoreVfxPrefab = prefab;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void ConfigureVisuals(GameObject deadVisual, GameObject restoredVisual, GameObject promptUI)
    {
        deadTreeVisual = deadVisual;
        restoredTreeVisual = restoredVisual;
        interactPromptUI = promptUI;
    }

    public void ResetTreeState()
    {
        isRestored = false;
        currentTreeHealth = 0f;
        if (treeDecayRoutine != null) StopCoroutine(treeDecayRoutine);
        UpdateTreeVisual();
        if (restoreVfxInstance != null) restoreVfxInstance.gameObject.SetActive(false);
        if (ambientFireflies != null) ambientFireflies.gameObject.SetActive(false);
    }
}