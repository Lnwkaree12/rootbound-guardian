using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ควบคุมลำดับ Tutorial ช่วงเริ่มเกม (9 Step) ใน MainGameplay
/// เมื่อจบ Step ที่ 9 ก่อนเปิดให้ผู้เล่นเดิน จะทำการเซฟข้อมูลสำคัญ (ต้นไม้, เลือดต้นไม้, เลือดผู้เล่น, ออกซิเจนผู้เล่น, ของในกระเป๋า)
/// หากถูกสั่งข้าม Tutorial (เช่น กดเล่นใหม่จาก Game Over) จะข้ามเข้าสู่โหมดหลังจบ Tutorial ทันที
/// </summary>
public class OpeningWakeupTutorialController : MonoBehaviour
{
    [Header("Core Scene References")]
    [SerializeField] private GameObject playerObj;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TreeSavePoint firstSaveTree;
    [SerializeField] private GameObject rightSideBlocker;
    [SerializeField] private ItemData startingPotionItem;

    [Header("Reverse Dead0 Animation (0001..0008)")]
    [SerializeField] private Sprite[] deadFramesOrdered = new Sprite[8];

    [Header("UI & VFX Assets")]
    [SerializeField] private Sprite spacePlankSprite;
    [SerializeField] private Sprite keySquareSprite;
    [SerializeField] private Sprite bluePotionSprite;
    [SerializeField] private Sprite waterDropSprite;
    [SerializeField] private GameObject wateringVfxPrefab;
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private AudioClip waterPourSound;

    [Header("Timing & Camera Parameters")]
    [SerializeField] private float initialHoldBeforeZoom = 0.5f;
    [SerializeField] private float zoomInDuration = 1.4f;
    [SerializeField] private float zoomOutDuration = 1.0f;
    [SerializeField] private Vector3 zoomedCamLocalPos = new Vector3(0f, 0.78f, -1.82f);
    [SerializeField] private Vector3 zoomedCamLocalEuler = new Vector3(11.5f, 0f, 0f);
    [SerializeField] private float zoomedCamFov = 48f;
    [SerializeField] private float standUpFrameDuration = 0.10f;
    [SerializeField] private float waitAfterStandUp = 3.0f;
    [SerializeField] private float treeApproachRadius = 4.2f;
    [SerializeField] private float wateringDuration = 2.2f;
    [SerializeField] private float bloomShowcaseDuration = 1.2f;
    [SerializeField] private float letterboxBarHeight = 120f;
    [SerializeField] private int startingOxygen = 20;

    [Header("Movement Speed Tuning")]
    [Tooltip("ความเร็วในการเดินช่วง Tutorial (หากตั้งค่า <= 0 จะใช้ความเร็วปกติของ PlayerMovement)")]
    [SerializeField] private float tutorialMoveSpeed = 5f;
    [Tooltip("คืนค่าความเร็วเดิมของ PlayerMovement เมื่อจบ Tutorial")]
    [SerializeField] private bool restoreNormalMoveSpeedAfterTutorial = true;

    // ตัวแปรอ้างอิงคอมโพเนนต์ของผู้เล่นและระบบในฉาก
    private PlayerInputHandler playerInputHandler;
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private PlayerHealth playerHealth;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;
    private Rigidbody playerRb;
    private LumiCompanion lumi;
    private AudioSource sfxAudioSource;

    private float savedNormalMoveSpeed = 7f;
    private bool hasSavedNormalMoveSpeed = false;

    // ตัวแปรอ้างอิง HUD
    private GameObject hudCanvasObj;
    private CanvasGroup hudCanvasCg;
    private GameObject questPanelObj;
    private CanvasGroup questPanelCg;
    private GameObject dashCooldownObj;
    private CanvasGroup dashCooldownCg;
    private GameObject potionQuickSlotObj;
    private CanvasGroup potionQuickSlotCg;
    private bool originalQuestPanelActive = true;

    // ค่าเริ่มต้นของกล้องก่อนซูม
    private Vector3 origCamLocalPos;
    private Quaternion origCamLocalRot;
    private float origCamFov;

    // UI ที่สร้างขึ้นขณะรันไทม์
    private GameObject letterboxCanvasObj;
    private CanvasGroup letterboxCg;
    private RectTransform topLetterboxBar;
    private RectTransform bottomLetterboxBar;

    private GameObject spacePromptObj;
    private CanvasGroup spacePromptCg;
    private Vector3 spacePromptBasePos;
    private TextMeshProUGUI spacePromptText;
    private UnityEngine.UI.Image spacePromptBgImage;

    private GameObject wasdPromptObj;
    private CanvasGroup wasdPromptCg;
    private Vector3 wasdPromptBasePos;
    private TextMeshProUGUI controllerMovePromptText;

    private GameObject wateringUiObj;
    private CanvasGroup wateringUiCg;
    private RectTransform bottlePivotRt;
    private RectTransform[] waterDropletRts;
    private UnityEngine.UI.Image[] waterDropletImgs;

    // สถานะการทำงานของ Tutorial
    private bool holdLyingSprite = false;
    private bool wateringTriggered = false;
    private bool tutorialFinished = false;
    public bool IsTutorialFinished => tutorialFinished;
    private GameObject activeWaterVfxInstance;

    [Header("Skip Tutorial Settings")]
    // เปิด/ปิด การอนุญาตให้กดปุ่ม Enter เพื่อข้าม Tutorial
    [SerializeField] private bool allowEnterToSkip = true;

    // ปุ่ม UI บนหน้าจอสำหรับกดข้าม Tutorial
    [SerializeField] private Button skipTutorialButton;

    // ตัวแปรสถานะว่าถูกสั่งข้ามแล้วหรือยัง
    private bool skipRequested = false;

    private void Awake()
    {
        ResolveReferences();

        if (playerCamera != null)
        {
            origCamLocalPos = playerCamera.transform.localPosition;
            origCamLocalRot = playerCamera.transform.localRotation;
            origCamFov = playerCamera.fieldOfView;
        }
    }

    private void Start()
    {
        ResolveReferences();

        bool shouldRunTutorial = CheckpointManager.Instance == null || CheckpointManager.Instance.ShouldRunTutorialOnSceneStart();
        if (!shouldRunTutorial)
        {
            SkipTutorialImmediately();
            return;
        }

        StartCoroutine(TutorialSequenceRoutine());
    }

    private void Update()
    {
        if (tutorialFinished) return;

        if (skipRequested || WasEnterPressedThisFrame())
        {
            skipRequested = false;
            SkipTutorialToGameplay();
        }
    }

    private void OnDestroy()
    {
        if (firstSaveTree != null)
        {
            firstSaveTree.OnDeferredWateringStarted -= HandleDeferredWateringStarted;
        }
        if (playerOxygen != null)
        {
            playerOxygen.SetFreezeOxygen(false);
        }
        RestoreNormalMoveSpeedIfNeeded();
    }

    private void LateUpdate()
    {
        if (holdLyingSprite && playerSpriteRenderer != null && deadFramesOrdered != null && deadFramesOrdered.Length >= 8 && deadFramesOrdered[7] != null)
        {
            playerSpriteRenderer.flipX = false;
            playerSpriteRenderer.sprite = deadFramesOrdered[7];
        }

        if (playerCamera != null)
        {
            Quaternion camRot = playerCamera.transform.rotation;
            if (spacePromptObj != null && spacePromptObj.activeSelf)
            {
                spacePromptObj.transform.rotation = camRot;
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.06f;
                spacePromptObj.transform.position = spacePromptBasePos + new Vector3(0f, bob, 0f);
            }
            if (wasdPromptObj != null && wasdPromptObj.activeSelf)
            {
                wasdPromptObj.transform.rotation = camRot;
            }
            if (wateringUiObj != null && wateringUiObj.activeSelf)
            {
                wateringUiObj.transform.rotation = camRot;
            }
        }
    }

    private void OnEnable()
    {
        InputSystemInput.OnLastInputDeviceChanged += OnInputDeviceChanged;
        RefreshControllerPrompts();

        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.AddListener(RequestSkipTutorial);
        }
    }

    private void OnDisable()
    {
        InputSystemInput.OnLastInputDeviceChanged -= OnInputDeviceChanged;

        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.RemoveListener(RequestSkipTutorial);
        }
    }

    private void OnInputDeviceChanged(bool isController)
    {
        RefreshControllerPrompts();
    }

    private bool WasEnterPressedThisFrame()
    {
        if (!allowEnterToSkip) return false;

        if (InputSystemInput.SubmitPressedThisFrame ||
            InputSystemInput.GetKeyDown(KeyCode.Return) ||
            InputSystemInput.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player Variant") ?? GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObj != null)
        {
            if (playerSpriteRenderer == null) playerSpriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>(true);
            if (playerAnimator == null) playerAnimator = playerObj.GetComponentInChildren<Animator>(true);
            if (playerCamera == null) playerCamera = playerObj.GetComponentInChildren<Camera>(true);

            playerInputHandler = playerObj.GetComponent<PlayerInputHandler>();
            playerController = playerObj.GetComponent<PlayerController>();
            playerMovement = playerObj.GetComponent<PlayerMovement>();
            playerAnimation = playerObj.GetComponent<PlayerAnimation>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerOxygen = playerObj.GetComponent<PlayerOxygen>();
            playerInventory = playerObj.GetComponentInChildren<Inventory>(true);
            playerRb = playerObj.GetComponent<Rigidbody>();

            if (playerMovement != null && !hasSavedNormalMoveSpeed)
            {
                savedNormalMoveSpeed = playerMovement.MoveSpeed;
                hasSavedNormalMoveSpeed = true;
            }
        }

        if (playerCamera == null) playerCamera = Camera.main;
        if (lumi == null) lumi = FindFirstObjectByType<LumiCompanion>(FindObjectsInactive.Include);

        if (firstSaveTree == null)
        {
            GameObject tObj = GameObject.Find("TreeSavePoint2");
            if (tObj != null) firstSaveTree = tObj.GetComponent<TreeSavePoint>();
        }

        if (rightSideBlocker == null)
        {
            rightSideBlocker = GameObject.Find("TutorialTreeBlocker");
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.spatialBlend = 0f;
            }
        }

        CacheHudReferences();
    }

    private void RestoreNormalMoveSpeedIfNeeded()
    {
        if (restoreNormalMoveSpeedAfterTutorial && hasSavedNormalMoveSpeed && playerMovement != null)
        {
            playerMovement.SetMoveSpeed(savedNormalMoveSpeed);
        }
    }

    private void CacheHudReferences()
    {
        hudCanvasObj = GameObject.Find("HUD Canvas");
        hudCanvasCg = EnsureCanvasGroup(hudCanvasObj);

        GameObject hudQ = GameObject.Find("HUD_Canvas_Quest Variant");
        if (hudQ != null)
        {
            Transform q = hudQ.transform.Find("QuestPanel");
            if (q != null)
            {
                questPanelObj = q.gameObject;
                originalQuestPanelActive = questPanelObj.activeSelf;
                questPanelCg = EnsureCanvasGroup(questPanelObj);
            }

            Transform dash = hudQ.transform.Find("DashCooldownUI");
            if (dash != null)
            {
                dashCooldownObj = dash.gameObject;
                dashCooldownCg = EnsureCanvasGroup(dashCooldownObj);
            }

            Transform pot = hudQ.transform.Find("PotionQuickSlotUI");
            if (pot != null)
            {
                potionQuickSlotObj = pot.gameObject;
                potionQuickSlotCg = EnsureCanvasGroup(potionQuickSlotObj);
            }
        }
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();
        return cg;
    }

    private static void SetCanvasGroupAlpha(CanvasGroup cg, float alpha, bool interactable)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
    }

    private IEnumerator TutorialSequenceRoutine()
    {
        if (CheckpointManager.Instance != null &&
            (!CheckpointManager.Instance.ShouldRunTutorialOnSceneStart() || CheckpointManager.Instance.IsTutorialCompleted()))
        {
            SkipTutorialImmediately();
            yield break;
        }

        // Step 1-2: เตรียมไอเทมเริ่มต้นและสถานะช่วงต้น Tutorial
        if (playerInventory != null && playerInventory.GetPotionCount() == 0 && startingPotionItem != null)
        {
            playerInventory.AddItem(startingPotionItem);
        }

        if (playerOxygen != null)
        {
            playerOxygen.SetLowOxygenAudioEnabled(false);
            playerOxygen.SetFreezeOxygen(true);
            playerOxygen.SetOxygen(startingOxygen);
        }

        if (lumi != null)
        {
            lumi.disableAutoDialogue = true;
            lumi.HideBubble();
        }

        SetGameplayControlsEnabled(false, true);
        if (playerAnimator != null) playerAnimator.enabled = false;
        if (playerAnimation != null) playerAnimation.enabled = false;

        if (playerSpriteRenderer != null && deadFramesOrdered != null && deadFramesOrdered.Length >= 8 && deadFramesOrdered[7] != null)
        {
            holdLyingSprite = true;
            playerSpriteRenderer.flipX = false;
            playerSpriteRenderer.sprite = deadFramesOrdered[7];
        }

        if (rightSideBlocker != null)
        {
            rightSideBlocker.SetActive(true);
        }

        if (firstSaveTree != null)
        {
            firstSaveTree.DeferRestorationToExternalController = true;
            firstSaveTree.OnDeferredWateringStarted -= HandleDeferredWateringStarted;
            firstSaveTree.OnDeferredWateringStarted += HandleDeferredWateringStarted;
        }

        BuildRuntimeUI();

        // ซ่อน HUD เกมเพลย์ระหว่าง Tutorial
        SetObjectActive(hudCanvasObj, true);
        SetObjectActive(dashCooldownObj, true);
        SetObjectActive(potionQuickSlotObj, true);
        SetCanvasGroupAlpha(hudCanvasCg, 0f, false);
        SetCanvasGroupAlpha(questPanelCg, 0f, false);
        SetCanvasGroupAlpha(dashCooldownCg, 0f, false);
        SetCanvasGroupAlpha(potionQuickSlotCg, 0f, false);
        SetObjectActive(letterboxCanvasObj, true);

        if (playerController != null)
        {
            playerController.SetDashUnlocked(false);
        }
        if (playerMovement != null)
        {
            playerMovement.SetDashEnabled(false);
        }

        yield return new WaitForSeconds(initialHoldBeforeZoom);

        // Step 3: ซูมกล้องเข้าหาตัวละคร
        yield return StartCoroutine(LerpCameraRoutine(
            origCamLocalPos, origCamLocalRot, origCamFov,
            zoomedCamLocalPos, Quaternion.Euler(zoomedCamLocalEuler), zoomedCamFov,
            zoomInDuration));

        // Step 4: แสดงปุ่ม [SPACE BAR] และรอผู้เล่นกดลุกขึ้น
        if (playerObj != null)
        {
            spacePromptBasePos = playerObj.transform.position + new Vector3(1.45f, 1.35f, 0f);
        }
        if (spacePromptObj != null)
        {
            spacePromptObj.transform.position = spacePromptBasePos;
            yield return StartCoroutine(FadeCanvasGroupRoutine(spacePromptObj, spacePromptCg, true, 0.22f));
        }

        yield return new WaitUntil(() => WasSpacePressedThisFrame());

        if (spacePromptObj != null)
        {
            yield return StartCoroutine(FadeCanvasGroupRoutine(spacePromptObj, spacePromptCg, false, 0.12f));
        }

        // Step 5: เล่นแอนิเมชันลุกขึ้นย้อนกลับ (เฟรม 7 -> 0)
        holdLyingSprite = false;
        if (playerSpriteRenderer != null && deadFramesOrdered != null && deadFramesOrdered.Length >= 8)
        {
            for (int i = 7; i >= 0; i--)
            {
                if (deadFramesOrdered[i] != null)
                {
                    playerSpriteRenderer.flipX = false;
                    playerSpriteRenderer.sprite = deadFramesOrdered[i];
                }
                yield return new WaitForSeconds(standUpFrameDuration);
            }
        }

        

        // รอหลังลุกขึ้นก่อนเปิดให้เดิน
       if (playerAnimator != null)
{
    playerAnimator.enabled = true;
}
if (playerAnimation != null)
{
    playerAnimation.enabled = true;
    playerAnimation.SetTutorialWalkMode(true); // เล่น Idle3 ตั้งแต่ลุกขึ้นยืน
}
        if (playerAnimation != null)
        {
            playerAnimation.enabled = true;
        }

        if (playerOxygen != null)
        {
            playerOxygen.SetLowOxygenAudioEnabled(true);
        }

        SetGameplayControlsEnabled(false, false);
        yield return new WaitForSeconds(waitAfterStandUp);

        // Step 6: แสดงปุ่ม [WASD] และเปิดให้เดิน

        if (playerAnimation != null)
{
    playerAnimation.SetTutorialWalkMode(true);
}

        if (playerObj != null)
        {
            wasdPromptBasePos = playerObj.transform.position + new Vector3(1.55f, 1.85f, 0f);
        }
        if (wasdPromptObj != null)
        {
            wasdPromptObj.transform.position = wasdPromptBasePos;
            yield return StartCoroutine(FadeCanvasGroupRoutine(wasdPromptObj, wasdPromptCg, true, 0.25f));
        }

        if (playerMovement != null && tutorialMoveSpeed > 0f)
        {
            if (!hasSavedNormalMoveSpeed)
            {
                savedNormalMoveSpeed = playerMovement.MoveSpeed;
                hasSavedNormalMoveSpeed = true;
            }
            playerMovement.SetMoveSpeed(tutorialMoveSpeed);
        }

        SetGameplayControlsEnabled(true, false);

        // Step 7: เดินเข้าใกล้ต้นไม้เซฟแรกแล้วแสดง Potion HUD
        yield return new WaitUntil(() =>
        {
            if (firstSaveTree == null || playerObj == null) return true;
            if (firstSaveTree.IsPlayerInRange) return true;
            Vector3 dp = playerObj.transform.position - firstSaveTree.transform.position;
            float horizDist = Mathf.Sqrt(dp.x * dp.x + dp.z * dp.z);
            return horizDist <= treeApproachRadius;
        });

        if (potionQuickSlotObj != null)
        {
            potionQuickSlotObj.SetActive(true);
            var potionUi = potionQuickSlotObj.GetComponent<PotionHUDUI>();
            if (potionUi != null) potionUi.RefreshDisplay(true);
            if (potionQuickSlotCg != null)
            {
                yield return StartCoroutine(FadeCanvasGroupAlphaRoutine(potionQuickSlotCg, 0f, 1f, 0.3f, true));
            }
        }

        // Step 8: รอจนผู้เล่นกดรดน้ำต้นไม้ด้วย Potion
        yield return new WaitUntil(() => wateringTriggered);

        SetGameplayControlsEnabled(false, false);

        var potionHud = potionQuickSlotObj != null ? potionQuickSlotObj.GetComponent<PotionHUDUI>() : null;
        if (potionHud != null) potionHud.RefreshDisplay(false);

        if (sfxAudioSource != null && waterPourSound != null)
        {
            sfxAudioSource.PlayOneShot(waterPourSound, 0.85f);
        }

        if (firstSaveTree != null && wateringVfxPrefab != null)
        {
            Vector3 vfxPos = firstSaveTree.transform.position + new Vector3(0.15f, 1.85f, -0.1f);
            activeWaterVfxInstance = Instantiate(wateringVfxPrefab, vfxPos, Quaternion.identity);
            activeWaterVfxInstance.SetActive(true);
            foreach (var ps in activeWaterVfxInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Play(true);
            }
        }

        if (wateringUiObj != null)
        {
            if (firstSaveTree != null)
            {
                wateringUiObj.transform.position = firstSaveTree.transform.position + new Vector3(0.65f, 2.75f, -0.25f);
            }
            yield return StartCoroutine(AnimatePouringWaterRoutine(wateringDuration));
            yield return StartCoroutine(FadeCanvasGroupRoutine(wateringUiObj, wateringUiCg, false, 0.16f));
            wateringUiObj.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(wateringDuration);
        }

        // Stop & clear watering VFX immediately once watering finishes, before the tree blooms
        if (activeWaterVfxInstance != null)
        {
            foreach (var ps in activeWaterVfxInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            activeWaterVfxInstance.SetActive(false);
            Destroy(activeWaterVfxInstance);
            activeWaterVfxInstance = null;
        }

        if (wasdPromptObj != null && wasdPromptObj.activeSelf)
        {
            StartCoroutine(FadeCanvasGroupRoutine(wasdPromptObj, wasdPromptCg, false, 0.18f));
        }

        // Step 9: ฟื้นฟูต้นไม้ เติมออกซิเจน และเปิดทางเดินต่อ (ก่อนเปิดให้ผู้เล่นเดินเล่นต่อเต็มรูปแบบ)
        if (firstSaveTree != null)
        {
            firstSaveTree.CompleteDeferredRestoration();
            StartCoroutine(TreeBloomGlowRoutine(firstSaveTree.transform.position));
        }

        if (playerOxygen != null)
        {
            int startO2 = playerOxygen.CurrentOxygen;
            int targetO2 = playerOxygen.MaxOxygen;
            float elapsedO2 = 0f;
            float durO2 = Mathf.Max(0.1f, bloomShowcaseDuration);
            while (elapsedO2 < durO2)
            {
                elapsedO2 += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedO2 / durO2));
                playerOxygen.SetOxygen(Mathf.RoundToInt(Mathf.Lerp(startO2, targetO2, t)));
                yield return null;
            }
            playerOxygen.SetOxygen(targetO2);
            playerOxygen.SetFreezeOxygen(false);
        }
        else
        {
            yield return new WaitForSeconds(bloomShowcaseDuration);
        }

        if (rightSideBlocker != null)
        {
            rightSideBlocker.SetActive(false);
        }

        // ✅ เซฟข้อมูลครบถ้วน ณ จุดจบ Tutorial Step 9 (ก่อนเปิดให้ผู้เล่นเดิน)
        // เก็บทั้ง ตำแหน่งผู้เล่น, สถานะ/เลือดต้นไม้, เลือดผู้เล่น, ออกซิเจนผู้เล่น และของในกระเป๋า
        SaveTutorialCompletionCheckpoint();

        if (playerAnimation != null)
{
    playerAnimation.SetTutorialWalkMode(false);
}

        RestoreNormalMoveSpeedIfNeeded();
        SetGameplayControlsEnabled(true, false);

        if (playerOxygen != null && (firstSaveTree == null || !firstSaveTree.IsRestored))
        {
            playerOxygen.UpdateSafeZoneState(firstSaveTree != null && firstSaveTree.IsPlayerInRange);
        }

        if (playerController != null)
        {
            playerController.SetDashUnlocked(true);
        }
        if (playerMovement != null)
        {
            playerMovement.SetDashEnabled(true);
        }

        tutorialFinished = true;

        // คืนกล้อง ซ่อน letterbox และแสดง HUD กลับมา
        Coroutine camOut = StartCoroutine(LerpCameraRoutine(
            zoomedCamLocalPos, Quaternion.Euler(zoomedCamLocalEuler), zoomedCamFov,
            origCamLocalPos, origCamLocalRot, origCamFov,
            zoomOutDuration));
        Coroutine barsOut = StartCoroutine(HideLetterboxBarsRoutine(1.0f));
        Coroutine hudIn = StartCoroutine(FadeInGameplayHudRoutine(1.0f));

        yield return camOut;
        yield return barsOut;
        yield return hudIn;

        if (lumi != null)
        {
            lumi.disableAutoDialogue = false;
        }

        tutorialFinished = true;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkTutorialCompleted();
        }
    }

    /// <summary>
    /// บันทึกข้อมูลจุดเซฟ ณ จุดจบ Tutorial Step 9 (ก่อนเปิดให้ผู้เล่นเดิน)
    /// </summary>
    private void SaveTutorialCompletionCheckpoint()
    {
        if (CheckpointManager.Instance == null || playerObj == null) return;

        PlayerHealth health = playerHealth != null ? playerHealth : playerObj.GetComponent<PlayerHealth>();
        Inventory inventory = playerInventory != null ? playerInventory : playerObj.GetComponentInChildren<Inventory>(true);
        Vector3 checkpointPos = playerObj.transform.root.position;

        CheckpointManager.Instance.SaveCheckpoint(
            checkpointPos,
            health,
            inventory,
            true,
            firstSaveTree
        );
        CheckpointManager.Instance.MarkCurrentCheckpointAsTutorialCompletion();

        Debug.Log("[OpeningWakeupTutorialController] เซฟข้อมูลจุดจบ Tutorial Step 9 เรียบร้อย (ต้นไม้, เลือดต้นไม้, เลือดผู้เล่น, ออกซิเจน, ของในกระเป๋า)");
    }

    

    private void HandleDeferredWateringStarted(TreeSavePoint tree)
    {
        wateringTriggered = true;
    }

    private void SetGameplayControlsEnabled(bool enabledState, bool keepAnimatorDisabled)
    {
        if (playerInputHandler != null)
        {
            if (enabledState)
            {
                playerInputHandler.enabled = true;
                playerInputHandler.ResetInputState();
                playerInputHandler.ResetDashFlag();
            }
            else
            {
                playerInputHandler.ResetInputState();
                playerInputHandler.ResetDashFlag();
                playerInputHandler.enabled = false;
            }
        }

        if (playerController != null)
        {
            playerController.enabled = enabledState;
        }

        if (playerMovement != null)
        {
            if (!enabledState) playerMovement.Move(Vector2.zero);
            playerMovement.enabled = enabledState;
        }

        if (!enabledState && playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (!enabledState && !keepAnimatorDisabled && playerAnimator != null && playerAnimator.enabled)
        {
            playerAnimator.SetFloat("Speed", 0f);
        }
    }

    private bool WasSpacePressedThisFrame()
    {
        return InputSystemInput.SubmitPressedThisFrame || InputSystemInput.GetKeyDown(KeyCode.Space);
    }

    private IEnumerator LerpCameraRoutine(Vector3 fromPos, Quaternion fromRot, float fromFov, Vector3 toPos, Quaternion toRot, float toFov, float duration)
    {
        if (playerCamera == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            playerCamera.transform.localPosition = Vector3.Lerp(fromPos, toPos, t);
            playerCamera.transform.localRotation = Quaternion.Slerp(fromRot, toRot, t);
            playerCamera.fieldOfView = Mathf.Lerp(fromFov, toFov, t);
            yield return null;
        }

        playerCamera.transform.localPosition = toPos;
        playerCamera.transform.localRotation = toRot;
        playerCamera.fieldOfView = toFov;
    }

    private IEnumerator FadeCanvasGroupRoutine(GameObject targetObj, CanvasGroup cg, bool show, float duration)
    {
        if (targetObj == null || cg == null) yield break;

        if (show)
        {
            targetObj.SetActive(true);
            cg.alpha = 0f;
        }

        float startAlpha = cg.alpha;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        cg.alpha = endAlpha;
        if (!show)
        {
            targetObj.SetActive(false);
        }
    }

    private IEnumerator AnimatePouringWaterRoutine(float duration)
    {
        if (wateringUiObj == null) yield break;

        wateringUiObj.SetActive(true);
        wateringUiCg.alpha = 1f;

        if (bottlePivotRt != null) bottlePivotRt.localRotation = Quaternion.identity;

        float tiltInTime = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tiltT = Mathf.Clamp01(elapsed / tiltInTime);
            float baseAngle = Mathf.Lerp(0f, 125f, Mathf.SmoothStep(0f, 1f, tiltT));
            float pourWobble = elapsed > tiltInTime ? Mathf.Sin((elapsed - tiltInTime) * 10f) * 5f : 0f;

            if (bottlePivotRt != null)
            {
                bottlePivotRt.localRotation = Quaternion.Euler(0f, 0f, baseAngle + pourWobble);
            }

            if (waterDropletRts != null)
            {
                for (int i = 0; i < waterDropletRts.Length; i++)
                {
                    if (waterDropletRts[i] == null || waterDropletImgs[i] == null) continue;
                    float cycle = ((elapsed * 1.9f) + (i / (float)waterDropletRts.Length)) % 1f;
                    float dropX = Mathf.Lerp(-28f, -58f, cycle) + Mathf.Sin(elapsed * 8f + i) * 4f;
                    float dropY = Mathf.Lerp(-12f, -155f, cycle);
                    waterDropletRts[i].anchoredPosition = new Vector2(dropX, dropY);
                    waterDropletRts[i].localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, Mathf.Sin(cycle * Mathf.PI));
                    Color dc = waterDropletImgs[i].color;
                    dc.a = tiltT * Mathf.Sin(cycle * Mathf.PI);
                    waterDropletImgs[i].color = dc;
                }
            }

            yield return null;
        }
    }

    private IEnumerator TreeBloomGlowRoutine(Vector3 treePos)
    {
        GameObject glowObj = new GameObject("OpeningTutorial_TreeBloomGlow");
        glowObj.transform.position = treePos + new Vector3(0f, 2.2f, -0.5f);
        Light glowLight = glowObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(0.78f, 1.0f, 0.48f, 1f);
        glowLight.range = 9f;
        glowLight.intensity = 0f;

        float burstTime = 0.45f;
        float elapsed = 0f;
        while (elapsed < burstTime)
        {
            elapsed += Time.deltaTime;
            glowLight.intensity = Mathf.Lerp(0f, 10f, elapsed / burstTime);
            yield return null;
        }

        float settleTime = 1.6f;
        elapsed = 0f;
        while (elapsed < settleTime)
        {
            elapsed += Time.deltaTime;
            glowLight.intensity = Mathf.Lerp(10f, 0f, elapsed / settleTime);
            yield return null;
        }

        Destroy(glowObj);
    }

    private IEnumerator FadeCanvasGroupAlphaRoutine(CanvasGroup cg, float from, float to, float duration, bool endInteractable)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        SetCanvasGroupAlpha(cg, to, endInteractable);
    }

    private IEnumerator FadeInGameplayHudRoutine(float duration)
    {
        SetObjectActive(hudCanvasObj, true);
        SetObjectActive(questPanelObj, originalQuestPanelActive);
        SetObjectActive(dashCooldownObj, true);
        SetObjectActive(potionQuickSlotObj, true);

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            if (hudCanvasCg != null) hudCanvasCg.alpha = t;
            if (questPanelCg != null && originalQuestPanelActive) questPanelCg.alpha = t;
            if (dashCooldownCg != null) dashCooldownCg.alpha = t;
            yield return null;
        }

        SetCanvasGroupAlpha(hudCanvasCg, 1f, true);
        if (originalQuestPanelActive)
        {
            SetCanvasGroupAlpha(questPanelCg, 1f, true);
        }
        SetCanvasGroupAlpha(dashCooldownCg, 1f, true);
        SetCanvasGroupAlpha(potionQuickSlotCg, 1f, true);
    }

    private IEnumerator HideLetterboxBarsRoutine(float duration)
    {
        if (letterboxCanvasObj == null) yield break;

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, duration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            if (letterboxCg != null)
            {
                letterboxCg.alpha = Mathf.Lerp(1f, 0f, t);
            }
            yield return null;
        }

        if (letterboxCg != null)
        {
            letterboxCg.alpha = 0f;
        }
        letterboxCanvasObj.SetActive(false);
    }

    private void BuildRuntimeUI()
    {
        BuildLetterboxCanvas();
        BuildSpacePrompt();
        BuildWasdPrompt();
        BuildWateringUi();
    }

    private void BuildLetterboxCanvas()
    {
        letterboxCanvasObj = new GameObject("OpeningTutorial_LetterboxCanvas");
        Canvas canvas = letterboxCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        letterboxCg = letterboxCanvasObj.AddComponent<CanvasGroup>();
        letterboxCg.alpha = 1f;
        letterboxCg.interactable = false;
        letterboxCg.blocksRaycasts = false;

        CanvasScaler scaler = letterboxCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        topLetterboxBar = CreateBarRect(letterboxCanvasObj.transform, "TopBar", true, letterboxBarHeight);
        bottomLetterboxBar = CreateBarRect(letterboxCanvasObj.transform, "BottomBar", false, letterboxBarHeight);
    }

    private RectTransform CreateBarRect(Transform parent, string barName, bool isTop, float height)
    {
        GameObject barObj = new GameObject(barName);
        barObj.transform.SetParent(parent, false);
        RectTransform rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);

        UnityEngine.UI.Image img = barObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }

    private void BuildSpacePrompt()
    {
        spacePromptObj = new GameObject("OpeningTutorial_SpacePrompt");
        Canvas canvas = spacePromptObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 60;

        RectTransform rootRt = spacePromptObj.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(360f, 120f);
        rootRt.localScale = Vector3.one * 0.008f;

        spacePromptCg = spacePromptObj.AddComponent<CanvasGroup>();
        spacePromptCg.alpha = 0f;

        GameObject bgObj = new GameObject("PlankBg");
        bgObj.transform.SetParent(spacePromptObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(520f, 292f);
        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.sprite = spacePlankSprite;
        bgImg.preserveAspect = true;
        bgImg.raycastTarget = false;
        spacePromptBgImage = bgImg;

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(spacePromptObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchoredPosition = new Vector2(0f, -12f);
        txtRt.sizeDelta = new Vector2(300f, 70f);
        spacePromptText = txtObj.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) spacePromptText.font = uiFont;
        spacePromptText.fontSize = 29f;
        spacePromptText.fontStyle = FontStyles.Bold;
        spacePromptText.alignment = TextAlignmentOptions.Center;
        spacePromptText.color = new Color(0.227f, 0.133f, 0.071f, 1f);
        spacePromptText.raycastTarget = false;

        spacePromptObj.SetActive(false);
        RefreshControllerPrompts();
    }

    private void BuildWasdPrompt()
    {
        wasdPromptObj = new GameObject("OpeningTutorial_WASDPrompt");
        Canvas canvas = wasdPromptObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 60;

        RectTransform rootRt = wasdPromptObj.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(320f, 220f);
        rootRt.localScale = Vector3.one * 0.008f;

        wasdPromptCg = wasdPromptObj.AddComponent<CanvasGroup>();
        wasdPromptCg.alpha = 0f;

        CreateKeyTile(wasdPromptObj.transform, "W", new Vector2(0f, 48f));
        CreateKeyTile(wasdPromptObj.transform, "A", new Vector2(-88f, -42f));
        CreateKeyTile(wasdPromptObj.transform, "S", new Vector2(0f, -42f));
        CreateKeyTile(wasdPromptObj.transform, "D", new Vector2(88f, -42f));

        GameObject controllerPromptObject = new GameObject(
            "ControllerMovePrompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        controllerPromptObject.transform.SetParent(wasdPromptObj.transform, false);
        RectTransform controllerPromptRect = controllerPromptObject.GetComponent<RectTransform>();
        controllerPromptRect.anchorMin = Vector2.zero;
        controllerPromptRect.anchorMax = Vector2.one;
        controllerPromptRect.offsetMin = Vector2.zero;
        controllerPromptRect.offsetMax = Vector2.zero;

        controllerMovePromptText = controllerPromptObject.GetComponent<TextMeshProUGUI>();
        controllerMovePromptText.text =
            InputSystemInput.GetControllerPromptTag(InputSystemInput.PromptAction.Move);
        if (uiFont != null) controllerMovePromptText.font = uiFont;
        controllerMovePromptText.fontSize = 28f;
        controllerMovePromptText.fontStyle = FontStyles.Bold;
        controllerMovePromptText.alignment = TextAlignmentOptions.Center;
        controllerMovePromptText.color = new Color(0.227f, 0.133f, 0.071f, 1f);
        controllerMovePromptText.raycastTarget = false;

        wasdPromptObj.SetActive(false);
        RefreshControllerPrompts();
    }

    private void RefreshControllerPrompts()
    {
        bool isController = InputSystemInput.IsControllerInputActive;
        if (spacePromptBgImage != null)
        {
            spacePromptBgImage.enabled = !isController;
        }

        if (spacePromptText != null)
        {
            spacePromptText.text = isController
                ? InputSystemInput.GetControllerPromptTag(InputSystemInput.PromptAction.Confirm)
                : "SPACE BAR";
        }

        if (wasdPromptObj != null)
        {
            foreach (Transform child in wasdPromptObj.transform)
            {
                if (child.name.StartsWith("Key_"))
                {
                    child.gameObject.SetActive(!isController);
                }
            }
        }

        if (controllerMovePromptText != null)
        {
            controllerMovePromptText.enabled = isController;
        }
    }

    private void CreateKeyTile(Transform parent, string keyLetter, Vector2 anchoredPos)
    {
        GameObject tileObj = new GameObject($"Key_{keyLetter}");
        tileObj.transform.SetParent(parent, false);
        RectTransform tileRt = tileObj.AddComponent<RectTransform>();
        tileRt.anchoredPosition = anchoredPos;
        tileRt.sizeDelta = new Vector2(82f, 82f);

        GameObject bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(tileObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(260f, 146f);
        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.sprite = keySquareSprite;
        bgImg.preserveAspect = true;
        bgImg.raycastTarget = false;

        GameObject txtObj = new GameObject("Letter");
        txtObj.transform.SetParent(tileObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchoredPosition = Vector2.zero;
        txtRt.sizeDelta = new Vector2(72f, 72f);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = keyLetter;
        if (uiFont != null) tmp.font = uiFont;
        tmp.fontSize = 38f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.227f, 0.133f, 0.071f, 1f);
        tmp.raycastTarget = false;
    }

    private void BuildWateringUi()
    {
        wateringUiObj = new GameObject("OpeningTutorial_WateringUI");
        Canvas canvas = wateringUiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 65;

        RectTransform rootRt = wateringUiObj.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(240f, 260f);
        rootRt.localScale = Vector3.one * 0.009f;

        wateringUiCg = wateringUiObj.AddComponent<CanvasGroup>();
        wateringUiCg.alpha = 0f;

        GameObject streamRoot = new GameObject("WaterStreamRoot");
        streamRoot.transform.SetParent(wateringUiObj.transform, false);
        RectTransform streamRt = streamRoot.AddComponent<RectTransform>();
        streamRt.anchoredPosition = new Vector2(0f, 10f);
        streamRt.sizeDelta = new Vector2(180f, 200f);

        int dropletCount = 6;
        waterDropletRts = new RectTransform[dropletCount];
        waterDropletImgs = new UnityEngine.UI.Image[dropletCount];
        for (int i = 0; i < dropletCount; i++)
        {
            GameObject dropObj = new GameObject($"WaterDrop_{i}");
            dropObj.transform.SetParent(streamRoot.transform, false);
            RectTransform dRt = dropObj.AddComponent<RectTransform>();
            dRt.sizeDelta = new Vector2(28f, 36f);
            UnityEngine.UI.Image dImg = dropObj.AddComponent<UnityEngine.UI.Image>();
            dImg.sprite = waterDropSprite;
            dImg.color = new Color(0.369f, 0.890f, 1f, 0f);
            dImg.preserveAspect = true;
            dImg.raycastTarget = false;

            waterDropletRts[i] = dRt;
            waterDropletImgs[i] = dImg;
        }

        GameObject bottleObj = new GameObject("BottlePivot");
        bottleObj.transform.SetParent(wateringUiObj.transform, false);
        bottlePivotRt = bottleObj.AddComponent<RectTransform>();
        bottlePivotRt.anchoredPosition = new Vector2(18f, 35f);
        bottlePivotRt.sizeDelta = new Vector2(110f, 168f);

        UnityEngine.UI.Image bottleImg = bottleObj.AddComponent<UnityEngine.UI.Image>();
        bottleImg.sprite = bluePotionSprite;
        bottleImg.preserveAspect = true;
        bottleImg.raycastTarget = false;

        wateringUiObj.SetActive(false);
    }

    private void SetObjectActive(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// ข้ามลำดับ Tutorial และเซฟข้อมูลจุดจบ Tutorial (ก่อนเปิดให้เดิน)
    /// </summary>
    private void SkipTutorialToGameplay()
    {
        if (tutorialFinished) return;

        if (playerOxygen != null)
        {
            playerOxygen.SetLowOxygenAudioEnabled(true);
        }

        StopAllCoroutines();

        holdLyingSprite = false;
        wateringTriggered = false;

        if (activeWaterVfxInstance != null)
        {
            Destroy(activeWaterVfxInstance);
            activeWaterVfxInstance = null;
        }

        if (spacePromptObj != null) spacePromptObj.SetActive(false);
        if (wasdPromptObj != null) wasdPromptObj.SetActive(false);
        if (wateringUiObj != null) wateringUiObj.SetActive(false);
        if (letterboxCanvasObj != null) letterboxCanvasObj.SetActive(false);

        if (firstSaveTree != null && !firstSaveTree.IsRestored)
        {
            firstSaveTree.CompleteDeferredRestoration();
        }

        if (rightSideBlocker != null)
        {
            rightSideBlocker.SetActive(false);
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = origCamLocalPos;
            playerCamera.transform.localRotation = origCamLocalRot;
            playerCamera.fieldOfView = origCamFov;
        }

        if (playerAnimator != null) playerAnimator.enabled = true;
        if (playerAnimation != null) playerAnimation.enabled = true;

        if (playerOxygen != null)
        {
            playerOxygen.SetFreezeOxygen(false);
            playerOxygen.SetOxygen(playerOxygen.MaxOxygen);
            playerOxygen.UpdateSafeZoneState(firstSaveTree != null && firstSaveTree.IsPlayerInRange);
        }

        SaveTutorialCompletionCheckpoint();

        RestoreNormalMoveSpeedIfNeeded();
        SetGameplayControlsEnabled(true, false);

        if (playerController != null)
        {
            playerController.SetDashUnlocked(true);
        }
        if (playerMovement != null)
        {
            playerMovement.SetDashEnabled(true);
        }

        SetObjectActive(hudCanvasObj, true);
        SetObjectActive(questPanelObj, originalQuestPanelActive);
        SetObjectActive(dashCooldownObj, true);
        SetObjectActive(potionQuickSlotObj, true);

        SetCanvasGroupAlpha(hudCanvasCg, 1f, true);
        if (originalQuestPanelActive) SetCanvasGroupAlpha(questPanelCg, 1f, true);
        SetCanvasGroupAlpha(dashCooldownCg, 1f, true);
        SetCanvasGroupAlpha(potionQuickSlotCg, 1f, true);

        if (lumi != null)
        {
            lumi.disableAutoDialogue = false;
        }

        tutorialFinished = true;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkTutorialCompleted();
        }
    }

    /// <summary>
    /// ข้าม Tutorial ทันทีเมื่อเริ่มฉากใหม่ในโหมดไม่เล่น Tutorial (เช่น กดเล่นใหม่จาก Game Over หรือมีจุดเซฟแล้ว)
    /// </summary>
    public void SkipTutorialImmediately()
    {
        if (tutorialFinished) return;

        if (playerOxygen != null)
        {
            playerOxygen.SetLowOxygenAudioEnabled(true);
        }

        StopAllCoroutines();

        holdLyingSprite = false;
        wateringTriggered = false;

        if (activeWaterVfxInstance != null)
        {
            Destroy(activeWaterVfxInstance);
            activeWaterVfxInstance = null;
        }

        if (spacePromptObj != null) spacePromptObj.SetActive(false);
        if (wasdPromptObj != null) wasdPromptObj.SetActive(false);
        if (wateringUiObj != null) wateringUiObj.SetActive(false);
        if (letterboxCanvasObj != null) letterboxCanvasObj.SetActive(false);

        if (firstSaveTree != null && !firstSaveTree.IsRestored)
        {
            firstSaveTree.CompleteDeferredRestoration();
        }

        if (rightSideBlocker != null)
        {
            rightSideBlocker.SetActive(false);
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = origCamLocalPos;
            playerCamera.transform.localRotation = origCamLocalRot;
            playerCamera.fieldOfView = origCamFov;
        }

        if (playerAnimator != null) playerAnimator.enabled = true;
        if (playerAnimation != null) playerAnimation.enabled = true;

        if (playerOxygen != null)
        {
            playerOxygen.SetFreezeOxygen(false);
            playerOxygen.UpdateSafeZoneState(firstSaveTree != null && firstSaveTree.IsPlayerInRange);
        }

        RestoreNormalMoveSpeedIfNeeded();
        SetGameplayControlsEnabled(true, false);

        if (playerController != null)
        {
            playerController.SetDashUnlocked(true);
        }
        if (playerMovement != null)
        {
            playerMovement.SetDashEnabled(true);
        }

        SetObjectActive(hudCanvasObj, true);
        SetObjectActive(questPanelObj, originalQuestPanelActive);
        SetObjectActive(dashCooldownObj, true);
        SetObjectActive(potionQuickSlotObj, true);

        SetCanvasGroupAlpha(hudCanvasCg, 1f, true);
        if (originalQuestPanelActive) SetCanvasGroupAlpha(questPanelCg, 1f, true);
        SetCanvasGroupAlpha(dashCooldownCg, 1f, true);
        SetCanvasGroupAlpha(potionQuickSlotCg, 1f, true);

        if (lumi != null)
        {
            lumi.disableAutoDialogue = false;
        }

        tutorialFinished = true;
    }

    public void RequestSkipTutorial()
    {
        if (!tutorialFinished)
        {
            skipRequested = true;
        }
    }
}