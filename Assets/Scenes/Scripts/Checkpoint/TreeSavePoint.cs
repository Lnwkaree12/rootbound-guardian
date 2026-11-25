using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จุดเซฟต้นไม้ในเกม: ฟื้นฟูต้นไม้ด้วย Potion เซฟจุด Checkpoint และลดเลือดต้นไม้ลงตามเวลา
/// </summary>
public class TreeSavePoint : MonoBehaviour
{
    [Header("Tree Status")]
    [SerializeField] private bool isRestored = false;
    [SerializeField] private bool healsOxygen = true;
    [SerializeField] private bool hasBeenRestoredBefore = false;

    [Header("Tree Health / Decay Settings")]
    [SerializeField] private float maxTreeHealth = 100f;
    [SerializeField] private float currentTreeHealth;
    [SerializeField] private float healthDecreaseRate = 5f;

    [Header("Tree Health UI")]
    [SerializeField] private GameObject treeHealthCanvasUI;
    [SerializeField] private Slider treeHealthSlider;

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
    public bool IsPlayerInRange => isPlayerInRange;
    public float CurrentTreeHealth => currentTreeHealth;
    public float MaxTreeHealth => maxTreeHealth;
    public float HealthDecreaseRate => healthDecreaseRate;
    public bool DeferRestorationToExternalController { get; set; } = false;
    public event System.Action<TreeSavePoint> OnDeferredWateringStarted;

    private bool isPlayerInRange = false;
    private bool isProcessing = false; // ป้องกันการกดปุ่มซ้ำซ้อนขณะกำลังประมวลผล
    private GameObject playerObject;
    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;
    private Coroutine treeDecayRoutine;

    // Stylized leafy canopy & golden bloom glow references
    private Transform canopyVisualTransform;
    private Transform glowBudsTransform;
    private GameObject bloomAuraObj;
    private Light bloomPointLight;
    private Coroutine canopyBloomRoutine;
    private Vector3 canopyBaseScale = Vector3.one;
    private Vector3 glowBudsBaseScale = Vector3.one;
    private Vector3 auraBaseScale = Vector3.one;
    private float baseBloomLightIntensity = 3.8f;
    private bool baseBloomCached = false;

    private void Awake()
    {
        currentTreeHealth = maxTreeHealth;
        FindVFXReferences();
        FindVisualReferences();
        EnsureCooldownCanvasAlwaysOnTop();
        UpdateTreeHealthUI();
    }

    private void Start()
    {
        FindVFXReferences();
        FindVisualReferences();
        EnsureCooldownCanvasAlwaysOnTop();
        UpdateTreeVisual();
        UpdateTreeHealthUI();
    }

    private void LateUpdate()
    {
        if (!isRestored) return;

        if (bloomAuraObj != null && bloomAuraObj.activeSelf)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                bloomAuraObj.transform.rotation = cam.transform.rotation;
            }
            if (canopyBloomRoutine == null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 2.4f) * 0.045f;
                bloomAuraObj.transform.localScale = auraBaseScale * pulse;
            }
        }

        if (bloomPointLight != null && bloomPointLight.enabled && canopyBloomRoutine == null)
        {
            bloomPointLight.intensity = baseBloomLightIntensity + Mathf.Sin(Time.time * 2.8f) * 0.4f;
        }
    }

    private void Update()
    {
        // ถ้าต้นไม้กำลังฟื้นฟูอยู่ ให้ลดค่าพลังชีวิตลงเรื่อยๆ ตามเวลา
        if (isRestored && currentTreeHealth > 0f)
        {
            currentTreeHealth -= healthDecreaseRate * Time.deltaTime;
            currentTreeHealth = Mathf.Max(0f, currentTreeHealth);
            UpdateTreeHealthUI();

            if (currentTreeHealth <= 0f)
            {
                CollapseTree();
                return;
            }
        }

        // ไม่รับอินพุตหากต้นไม้ฟื้นฟูแล้ว ผู้เล่นอยู่นอกระยะ หรือกำลังประมวลผล
        if (isRestored || !isPlayerInRange || isProcessing) return;

        bool interactPressed = (inputHandler != null && inputHandler.InteractPressed) || Input.GetKeyDown(KeyCode.E);

        if (interactPressed)
        {
            StartCoroutine(TryRestoreTreeWithPotionRoutine());
        }
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
            else if (healsOxygen && playerOxygen != null)
            {
                playerOxygen.SetOxygenSlowDrain(false);
                playerOxygen.UpdateSafeZoneState(true);
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
                    targetOxygen.SetOxygenSlowDrain(true);
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

    private IEnumerator TryRestoreTreeWithPotionRoutine()
    {
        isProcessing = true;

        if (isRestored || playerInventory == null)
        {
            isProcessing = false;
            yield break;
        }

        if (!playerInventory.HasPotion())
        {
            Debug.Log("⚠️ คุณไม่มี Potion!");
            float waitTime = 0.4f;
            while (waitTime > 0f)
            {
                waitTime -= Time.deltaTime;
                yield return null;
            }
            isProcessing = false;
            yield break;
        }

        bool success = playerInventory.ConsumePotionDirectly();

        if (success)
        {
            if (DeferRestorationToExternalController)
            {
                SetPromptActive(false);
                OnDeferredWateringStarted?.Invoke(this);
                yield break;
            }

            isRestored = true;
            ExecuteTreeRestoration();
            Debug.Log("✨ ใช้ Potion ฟื้นฟูต้นไม้สำเร็จ!");

            isProcessing = false;
            yield break;
        }

        isProcessing = false;
    }

    private void ExecuteTreeRestoration()
    {
        currentTreeHealth = maxTreeHealth;

        SetPromptActive(false);
        UpdateTreeVisual();
        PlayCanopyBloomAnimation();
        UpdateTreeHealthUI();
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

        // ฟื้นฟูรอบแรกให้เซฟข้อมูลลง CheckpointManager ทันที
        if (!hasBeenRestoredBefore)
        {
            SaveGame();
            hasBeenRestoredBefore = true;
            Debug.Log("[TreeSavePoint] ฟื้นฟูรอบแรก: ทำการบันทึก Checkpoint สำเร็จ");
        }
        else
        {
            Debug.Log("[TreeSavePoint] ฟื้นฟูรอบที่ 2 ขึ้นไป: ฟื้นฟูต้นไม้อย่างเดียว ไม่บันทึก Checkpoint ซ้ำ");
        }
    }

    public void RestoreTree()
    {
        if (isRestored) return;
        isRestored = true;
        ExecuteTreeRestoration();
    }

    public void CompleteDeferredRestoration()
    {
        DeferRestorationToExternalController = false;
        isProcessing = false;
        if (!isRestored)
        {
            isRestored = true;
            ExecuteTreeRestoration();
            Debug.Log("✨ ใช้ Potion ฟื้นฟูต้นไม้สำเร็จ!");
        }
    }

    /// <summary>
    /// คืนค่าเลือดและสถานะของต้นไม้กลับไปตรงกับตอนที่บันทึกจุดเซฟ
    /// </summary>
    public void RestoreFromSavedState(float savedHealth)
    {
        currentTreeHealth = Mathf.Clamp(savedHealth, 0f, maxTreeHealth);
        isRestored = currentTreeHealth > 0f;
        isProcessing = false;

        UpdateTreeVisual();
        UpdateTreeHealthUI();

        if (isRestored && isPlayerInRange && healsOxygen && playerOxygen != null)
        {
            playerOxygen.SetOxygenSlowDrain(false);
            playerOxygen.UpdateSafeZoneState(true);
        }
    }

    private void CollapseTree()
    {
        isRestored = false;
        currentTreeHealth = 0f;
        UpdateTreeVisual();
        UpdateTreeHealthUI();

        if (restoreVfxInstance != null) restoreVfxInstance.gameObject.SetActive(false);
        if (ambientFireflies != null) ambientFireflies.gameObject.SetActive(false);

        if (isPlayerInRange && playerOxygen != null)
        {
            playerOxygen.SetOxygenSlowDrain(false);
            playerOxygen.UpdateSafeZoneState(false);
        }

        if (isPlayerInRange)
        {
            SetPromptActive(true);
        }

        Debug.Log($"[TreeSavePoint] ต้นไม้ {gameObject.name} พลังชีวิตหมดและกลับคืนสภาพเดิม");
    }

    private void UpdateTreeHealthUI()
    {
        if (treeHealthSlider == null)
        {
            treeHealthSlider = GetComponentInChildren<Slider>(true);
        }

        if (treeHealthCanvasUI == null)
        {
            if (treeHealthSlider != null)
            {
                var canvas = treeHealthSlider.GetComponentInParent<Canvas>(true);
                if (canvas != null) treeHealthCanvasUI = canvas.gameObject;
            }
            if (treeHealthCanvasUI == null)
            {
                var cd = transform.Find("CoolDown Tree Canvas");
                if (cd == null) cd = transform.Find("CooldownTree Canvas");
                if (cd == null) cd = transform.Find("Canvas");
                if (cd != null) treeHealthCanvasUI = cd.gameObject;
            }
        }

        if (treeHealthSlider != null)
        {
            treeHealthSlider.maxValue = maxTreeHealth;
            treeHealthSlider.value = currentTreeHealth;
        }

        if (treeHealthCanvasUI != null)
        {
            treeHealthCanvasUI.SetActive(isRestored && currentTreeHealth > 0f);
        }
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
            CheckpointManager.Instance.SaveCheckpoint(checkpointPosition, playerHealth, playerInventory, healsOxygen, this);
            Debug.Log("[Tree Save Point] บันทึกจุด Checkpoint เรียบร้อย!");
        }
        else if (Application.isPlaying)
        {
            Debug.LogError("❌ หา CheckpointManager.Instance ไม่เจอใน Scene!");
        }
    }

    private void UpdateTreeVisual()
    {
        if (!isRestored && canopyBloomRoutine != null)
        {
            StopCoroutine(canopyBloomRoutine);
            canopyBloomRoutine = null;
        }

        if (deadTreeVisual != null) deadTreeVisual.SetActive(!isRestored);
        if (restoredTreeVisual != null) restoredTreeVisual.SetActive(isRestored);

        if (canopyVisualTransform != null && canopyBloomRoutine == null)
        {
            canopyVisualTransform.localScale = canopyBaseScale;
        }
        if (glowBudsTransform != null && canopyBloomRoutine == null)
        {
            glowBudsTransform.localScale = glowBudsBaseScale;
        }
        if (bloomAuraObj != null)
        {
            bloomAuraObj.SetActive(isRestored);
            if (canopyBloomRoutine == null) bloomAuraObj.transform.localScale = auraBaseScale;
        }
        if (bloomPointLight != null)
        {
            bloomPointLight.enabled = isRestored;
            if (canopyBloomRoutine == null) bloomPointLight.intensity = baseBloomLightIntensity;
        }

        if (ambientFireflies != null)
        {
            ambientFireflies.gameObject.SetActive(isRestored);
            if (isRestored && !ambientFireflies.isPlaying) ambientFireflies.Play(true);
        }
    }

    private void PlayCanopyBloomAnimation()
    {
        FindVisualReferences();
        if (!Application.isPlaying || !gameObject.activeInHierarchy) return;

        if (canopyBloomRoutine != null) StopCoroutine(canopyBloomRoutine);
        canopyBloomRoutine = StartCoroutine(CanopyBloomRoutine());
    }

    private IEnumerator CanopyBloomRoutine()
    {
        float duration = 0.55f;
        float elapsed = 0f;

        if (canopyVisualTransform != null) canopyVisualTransform.localScale = canopyBaseScale * 0.05f;
        if (glowBudsTransform != null) glowBudsTransform.localScale = glowBudsBaseScale * 0.05f;
        if (bloomAuraObj != null)
        {
            bloomAuraObj.SetActive(true);
            bloomAuraObj.transform.localScale = auraBaseScale * 0.25f;
        }
        if (bloomPointLight != null)
        {
            bloomPointLight.enabled = true;
            bloomPointLight.intensity = 0f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth ease-out back bounce (0 -> ~1.12 -> 1.0)
            float inv = 1f - t;
            float back = 1f + 1.8f * Mathf.Pow(inv, 3f) * Mathf.Sin(t * Mathf.PI) - Mathf.Pow(inv, 3f);
            float sproutScale = Mathf.Lerp(0.05f, 1.0f, Mathf.Max(0f, back));

            if (canopyVisualTransform != null) canopyVisualTransform.localScale = canopyBaseScale * sproutScale;
            if (glowBudsTransform != null) glowBudsTransform.localScale = glowBudsBaseScale * sproutScale;
            if (bloomAuraObj != null) bloomAuraObj.transform.localScale = auraBaseScale * Mathf.Lerp(0.25f, 1.08f, Mathf.SmoothStep(0f, 1f, t));
            if (bloomPointLight != null)
            {
                float lightCurve = t < 0.45f
                    ? Mathf.Lerp(0f, baseBloomLightIntensity * 1.95f, t / 0.45f)
                    : Mathf.Lerp(baseBloomLightIntensity * 1.95f, baseBloomLightIntensity, (t - 0.45f) / 0.55f);
                bloomPointLight.intensity = lightCurve;
            }

            yield return null;
        }

        if (canopyVisualTransform != null) canopyVisualTransform.localScale = canopyBaseScale;
        if (glowBudsTransform != null) glowBudsTransform.localScale = glowBudsBaseScale;
        if (bloomAuraObj != null) bloomAuraObj.transform.localScale = auraBaseScale;
        if (bloomPointLight != null) bloomPointLight.intensity = baseBloomLightIntensity;
        canopyBloomRoutine = null;
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

        if (restoredTreeVisual != null)
        {
            if (canopyVisualTransform == null) canopyVisualTransform = restoredTreeVisual.transform.Find("Canopy_Leaves");
            if (glowBudsTransform == null) glowBudsTransform = restoredTreeVisual.transform.Find("Blossom_GlowBuds");
            if (bloomAuraObj == null) bloomAuraObj = restoredTreeVisual.transform.Find("BloomAuraHalo")?.gameObject;
            if (bloomPointLight == null) bloomPointLight = restoredTreeVisual.transform.Find("BloomPointLight")?.GetComponent<Light>();

            if (!baseBloomCached)
            {
                baseBloomCached = true;
                if (canopyVisualTransform != null) canopyBaseScale = canopyVisualTransform.localScale;
                if (glowBudsTransform != null) glowBudsBaseScale = glowBudsTransform.localScale;
                if (bloomAuraObj != null) auraBaseScale = bloomAuraObj.transform.localScale;
                if (bloomPointLight != null && bloomPointLight.intensity > 0.01f) baseBloomLightIntensity = bloomPointLight.intensity;
            }
        }

        if (interactPromptUI == null)
        {
            var prompt = transform.Find("InteractUI_Canvas");
            if (prompt != null) interactPromptUI = prompt.gameObject;
        }

        if (treeHealthCanvasUI == null)
        {
            var cd = transform.Find("CoolDown Tree Canvas");
            if (cd == null) cd = transform.Find("CooldownTree Canvas");
            if (cd == null) cd = transform.Find("Canvas");
            if (cd != null)
            {
                treeHealthCanvasUI = cd.gameObject;
                if (treeHealthSlider == null)
                {
                    treeHealthSlider = cd.GetComponentInChildren<Slider>(true);
                }
            }
        }
    }

    private void EnsureCooldownCanvasAlwaysOnTop()
    {
        Canvas cdCanvas = null;
        if (treeHealthCanvasUI != null)
        {
            cdCanvas = treeHealthCanvasUI.GetComponent<Canvas>();
        }
        if (cdCanvas == null)
        {
            foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
            {
                if (c.name.IndexOf("CoolDown", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cdCanvas = c;
                    break;
                }
            }
        }
        if (cdCanvas == null) return;

        cdCanvas.overrideSorting = true;
        cdCanvas.sortingOrder = 60;

        Shader overlayShader = Shader.Find("UI/OverlayAlwaysOnTop");
        if (overlayShader != null)
        {
            Material overlayMat = new Material(overlayShader);
            foreach (Image img in cdCanvas.GetComponentsInChildren<Image>(true))
            {
                img.material = overlayMat;
            }
            foreach (RawImage raw in cdCanvas.GetComponentsInChildren<RawImage>(true))
            {
                raw.material = overlayMat;
            }
        }

        Shader tmpOverlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
        foreach (TMPro.TextMeshProUGUI tmp in cdCanvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            if (tmp.fontSharedMaterial != null)
            {
                Material mat = new Material(tmp.fontSharedMaterial);
                string shaderName = mat.shader != null ? mat.shader.name : string.Empty;
                bool isDistanceField = shaderName.IndexOf("Distance Field", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (isDistanceField)
                {
                    if (tmpOverlayShader != null)
                    {
                        mat.shader = tmpOverlayShader;
                    }
                    mat.EnableKeyword("OUTLINE_ON");
                    if (mat.HasProperty("_OutlineWidth"))
                    {
                        mat.SetFloat("_OutlineWidth", 0.22f);
                    }
                    if (mat.HasProperty("_OutlineColor"))
                    {
                        mat.SetColor("_OutlineColor", new Color(0.16f, 0.08f, 0.04f, 1f));
                    }
                    tmp.fontStyle |= TMPro.FontStyles.Bold;
                }
                else
                {
                    mat.renderQueue = 4000;
                    if (mat.HasProperty("unity_GUIZTestMode"))
                    {
                        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    }
                }

                tmp.fontMaterial = mat;
            }
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
        UpdateTreeVisual();
        UpdateTreeHealthUI();
        if (restoreVfxInstance != null) restoreVfxInstance.gameObject.SetActive(false);
        if (ambientFireflies != null) ambientFireflies.gameObject.SetActive(false);
    }
}