using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controls the 9-step Opening Wake-Up & First Save Tree Watering Tutorial sequence in MainGameplay:
/// 1. Starts lying flat on the floor (Dead0 frame 0008) with black cinematic letterbox bars and 1 starting Blue Potion.
/// 2. Zooms camera closer to show the lying character clearly.
/// 3. Shows [SPACE BAR] wooden plank prompt above head; pressing Space plays Dead0 in reverse (0008 -> 0001) to stand up.
/// 4. Waits 3 seconds after standing animation finishes with movement locked.
/// 5. Shows permanent [W][A][S][D] wooden key prompts and unlocks movement.
/// 6. Player walks right toward the first TreeSavePoint (past cleared Thorn Traps) with a right-side blocker collider preventing walking past the tree.
/// 7. Approaching the tree reveals the Potion HUD slot (x1) and the [E] prompt while still blocking walking further right.
/// 8. Pressing E consumes the potion (x0), locks movement, keeps camera zoomed, and animates tilting the Blue Potion bottle pouring water onto the tree.
/// 9. Watering UI disappears, tree blooms with leaves and golden-green light glow, blocker opens, camera zooms out & letterbox slides away, normal gameplay HUD resumes.
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

    // Runtime cached player components
    private PlayerInputHandler playerInputHandler;
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private PlayerOxygen playerOxygen;
    private Inventory playerInventory;
    private Rigidbody playerRb;
    private LumiCompanion lumi;
    private AudioSource sfxAudioSource;

    // HUD references
    private GameObject hudCanvasObj;
    private CanvasGroup hudCanvasCg;
    private GameObject questPanelObj;
    private CanvasGroup questPanelCg;
    private GameObject dashCooldownObj;
    private CanvasGroup dashCooldownCg;
    private GameObject potionQuickSlotObj;
    private CanvasGroup potionQuickSlotCg;
    private bool originalQuestPanelActive = true;

    // Original camera state
    private Vector3 origCamLocalPos;
    private Quaternion origCamLocalRot;
    private float origCamFov;

    // Runtime generated UI
    private GameObject letterboxCanvasObj;
    private CanvasGroup letterboxCg;
    private RectTransform topLetterboxBar;
    private RectTransform bottomLetterboxBar;

    private GameObject spacePromptObj;
    private CanvasGroup spacePromptCg;
    private Vector3 spacePromptBasePos;

    private GameObject wasdPromptObj;
    private CanvasGroup wasdPromptCg;
    private Vector3 wasdPromptBasePos;

    private GameObject wateringUiObj;
    private CanvasGroup wateringUiCg;
    private RectTransform bottlePivotRt;
    private RectTransform[] waterDropletRts;
    private UnityEngine.UI.Image[] waterDropletImgs;

    // State flags
    private bool holdLyingSprite = false;
    private bool wateringTriggered = false;
    private bool tutorialFinished = false;
    public bool IsTutorialFinished => tutorialFinished;
    private GameObject activeWaterVfxInstance;

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
        StartCoroutine(TutorialSequenceRoutine());
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
            playerOxygen = playerObj.GetComponent<PlayerOxygen>();
            playerInventory = playerObj.GetComponentInChildren<Inventory>(true);
            playerRb = playerObj.GetComponent<Rigidbody>();
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
        // Step 1 Setup: Ensure 1 Blue Potion in inventory from start, safe oxygen, lock movement, lie flat on frame 0008
        if (playerInventory != null && playerInventory.GetPotionCount() == 0 && startingPotionItem != null)
        {
            playerInventory.AddItem(startingPotionItem);
        }

        if (playerOxygen != null)
        {
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

        // Step 1 HUD state: hide all gameplay HUD cleanly via CanvasGroup (keeps UI listeners active) until watering completes
        SetObjectActive(hudCanvasObj, true);
        SetObjectActive(dashCooldownObj, true);
        SetObjectActive(potionQuickSlotObj, true);
        SetCanvasGroupAlpha(hudCanvasCg, 0f, false);
        SetCanvasGroupAlpha(questPanelCg, 0f, false);
        SetCanvasGroupAlpha(dashCooldownCg, 0f, false);
        SetCanvasGroupAlpha(potionQuickSlotCg, 0f, false);
        SetObjectActive(letterboxCanvasObj, true);

        // Hold briefly on Step 1 wide framing, then proceed to Step 2
        yield return new WaitForSeconds(initialHoldBeforeZoom);

        // Step 2: Smoothly zoom camera closer to see lying player
        yield return StartCoroutine(LerpCameraRoutine(
            origCamLocalPos, origCamLocalRot, origCamFov,
            zoomedCamLocalPos, Quaternion.Euler(zoomedCamLocalEuler), zoomedCamFov,
            zoomInDuration));

        // Step 3: Show [SPACE BAR] prompt above head; wait for Space key; play Dead0 reverse 0008 -> 0001
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

        // Play Dead0 frames in reverse: index 7 (0008) down to index 0 (0001)
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

        // Step 4: Stand idle with controls locked for 3 seconds
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Play("Idle", 0, 0f);
        }
        if (playerAnimation != null)
        {
            playerAnimation.enabled = true;
        }

        SetGameplayControlsEnabled(false, false);
        yield return new WaitForSeconds(waitAfterStandUp);

        // Step 5: Show permanent [W][A][S][D] prompt and unlock movement
        if (playerObj != null)
        {
            wasdPromptBasePos = playerObj.transform.position + new Vector3(1.55f, 1.85f, 0f);
        }
        if (wasdPromptObj != null)
        {
            wasdPromptObj.transform.position = wasdPromptBasePos;
            yield return StartCoroutine(FadeCanvasGroupRoutine(wasdPromptObj, wasdPromptCg, true, 0.25f));
        }

        SetGameplayControlsEnabled(true, false);

        // Step 6 & Step 7: Walk right toward the first save tree; reveal Potion HUD (x1) when approaching tree
        yield return new WaitUntil(() =>
        {
            if (firstSaveTree == null || playerObj == null) return true;
            if (firstSaveTree.IsPlayerInRange) return true;
            Vector3 dp = playerObj.transform.position - firstSaveTree.transform.position;
            float horizDist = Mathf.Sqrt(dp.x * dp.x + dp.z * dp.z);
            return horizDist <= treeApproachRadius;
        });

        // Reveal PotionQuickSlotUI (now showing x1 starting potion!)
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

        // Wait for player to press E at the tree (consumes potion to x0 and fires OnDeferredWateringStarted)
        yield return new WaitUntil(() => wateringTriggered);

        // Step 8: Lock movement (camera stays zoomed!), display pouring Blue Potion UI + water VFX
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
            yield return StartCoroutine(FadeCanvasGroupRoutine(wateringUiObj, wateringUiCg, false, 0.18f));
        }
        else
        {
            yield return new WaitForSeconds(wateringDuration);
        }

        if (activeWaterVfxInstance != null)
        {
            Destroy(activeWaterVfxInstance, 0.4f);
            activeWaterVfxInstance = null;
        }

        // Step 9: Complete tree bloom + bright glowing aura light, smoothly fade out low-oxygen dark vignette, then open blocker & fade HUD back in
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

        SetGameplayControlsEnabled(true, false);

        if (playerOxygen != null && (firstSaveTree == null || !firstSaveTree.IsRestored))
        {
            playerOxygen.UpdateSafeZoneState(firstSaveTree != null && firstSaveTree.IsPlayerInRange);
        }

        // Simultaneously zoom camera back out, fade letterbox bars away, and fade gameplay HUD in
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
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        return false;
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
            glowLight.intensity = Mathf.Lerp(10f, 2.8f, elapsed / settleTime);
            yield return null;
        }
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

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(spacePromptObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchoredPosition = new Vector2(0f, -12f);
        txtRt.sizeDelta = new Vector2(300f, 70f);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "SPACE BAR";
        if (uiFont != null) tmp.font = uiFont;
        tmp.fontSize = 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.227f, 0.133f, 0.071f, 1f);
        tmp.raycastTarget = false;

        spacePromptObj.SetActive(false);
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

        wasdPromptObj.SetActive(false);
    }

    private void CreateKeyTile(Transform parent, string keyLetter, Vector2 anchoredPos)
    {
        GameObject tileObj = new GameObject($"Key_{keyLetter}");
        tileObj.transform.SetParent(parent, false);
        RectTransform tileRt = tileObj.AddComponent<RectTransform>();
        tileRt.anchoredPosition = anchoredPos;
        tileRt.sizeDelta = new Vector2(82f, 82f);

        // UI_language.png is 1920x1080 with centered square wood frame; scale slightly larger inside tile
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

        // Water stream droplets underneath bottle
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

        // Bottle pivot
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
}
