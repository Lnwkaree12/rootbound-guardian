using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the interaction with the giant World Tree Root in MainGameplay:
/// - Detects player proximity in the final chamber.
/// - Displays a floating billboard UI prompt: "กด E รดน้ำต้นไม้".
/// - When player presses E:
///     1. Freezes player controls on the ground.
///     2. Plays water SFX and splash on the root.
///     3. Gradually transitions the root's Shadows Color from #1B0E0A to #3D1E16.
///     4. AFTER the shadow color transition finishes:
///        - Triggers player warp VFX (ground portal ring, upward light beam, sparkles, rising holy leaves, whoosh SFX).
///        - Pulls the Player upwards into the air with smooth acceleration and ascension float.
///        - Main camera tilts up without switching cameras, tracking player ascending into the heavens.
///        - Seamless warp flash / fade transitions directly into EndScene.
/// </summary>
public class WorldTreeRootInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRadius = 8.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool allowKeyFBackup = true;

    [Header("Prompt UI References")]
    [SerializeField] private GameObject promptCanvasObject;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TextMeshProUGUI promptTextTMP;
    [SerializeField] private Image promptKeyIcon;
    [SerializeField] private Image promptSproutIcon;
    [SerializeField] private Image promptBg;

    [Header("Root Shadow Color Transition Settings")]
    [SerializeField] private Renderer rootRenderer;
    [SerializeField] private string shadowColorProperty = "_Shadows_Color";
    [SerializeField] private Color startShadowColor = new Color(0.10588235f, 0.05490196f, 0.03921569f, 0f); // #1B0E0A
    [SerializeField] private Color targetShadowColor = new Color(0.23921569f, 0.11764706f, 0.08627451f, 0f); // #3D1E16
    [SerializeField] private float shadowTransitionDuration = 2.4f;

    [Header("Ascension & Warp Settings")]
    [SerializeField] private float ascendHeight = 18f;
    [SerializeField] private float ascendDuration = 3.8f;
    [SerializeField] private string endSceneName = "EndScene";
    [SerializeField] private bool transitionToEndScene = true;

    [Header("Audio & VFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip waterSFX;
    [SerializeField] private AudioClip successChimeSFX;
    [SerializeField] private AudioClip warpWhooshSFX;
    [SerializeField] private GameObject warpVFXPrefab;
    [SerializeField] private GameObject waterSplashVFXPrefab;
    [SerializeField] private Transform waterSplashSpawnPoint;

    [Header("Animation")]
    [SerializeField] private float floatSpeed = 3.2f;
    [SerializeField] private float floatHeight = 0.14f;
    [SerializeField] private float pulseSpeed = 3.5f;
    [SerializeField] private float pulseScale = 0.04f;

    private Transform playerTransform;
    private GameObject playerRootObject;
    private PlayerMovement playerMovement;
    private Camera mainCamera;
    private Vector3 initialPromptLocalPos;
    private Vector3 initialPromptScale;
    private bool playerInRange = false;
    private bool isWatered = false;
    private List<Material> dynamicRootMaterials = new List<Material>();

    private void Awake()
    {
        if (promptCanvasObject != null)
        {
            initialPromptLocalPos = promptCanvasObject.transform.localPosition;
            initialPromptScale = promptCanvasObject.transform.localScale;
        }

        if (promptCanvasGroup == null && promptCanvasObject != null)
        {
            promptCanvasGroup = promptCanvasObject.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null) promptCanvasGroup = promptCanvasObject.AddComponent<CanvasGroup>();
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.blocksRaycasts = false;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.3f;
        }

        SetupRootMaterials();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        RefreshPromptText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        RefreshPromptText();
    }

    private void RefreshPromptText()
    {
        if (promptTextTMP != null && !isWatered)
        {
            promptTextTMP.text = LocalizationManager.Instance != null 
                ? LocalizationManager.Instance.Get("root_prompt_water") 
                : "กด E รดน้ำต้นไม้";
        }
    }

    private void Start()
    {
        FindPlayer();
        mainCamera = Camera.main;
        RefreshPromptText();

        // Ensure root starts with initial shadow color #1B0E0A
        ApplyShadowColor(startShadowColor);
    }

    private void SetupRootMaterials()
    {
        dynamicRootMaterials.Clear();

        if (rootRenderer == null)
        {
            Transform rootParent = transform.parent;
            if (rootParent != null)
            {
                Transform rootsChild = rootParent.Find("Coral_Giant_Roots");
                if (rootsChild != null) rootRenderer = rootsChild.GetComponent<Renderer>();
                if (rootRenderer == null) rootRenderer = rootParent.GetComponentInChildren<Renderer>();
            }
        }

        if (rootRenderer != null)
        {
            foreach (var mat in rootRenderer.materials)
            {
                if (mat != null && mat.HasProperty(shadowColorProperty))
                {
                    dynamicRootMaterials.Add(mat);
                }
            }
        }
    }

    private void ApplyShadowColor(Color color)
    {
        if (dynamicRootMaterials.Count == 0) SetupRootMaterials();

        foreach (var mat in dynamicRootMaterials)
        {
            if (mat != null)
            {
                mat.SetColor(shadowColorProperty, color);
            }
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.Find("Player Variant");
        if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (playerObj != null)
        {
            playerRootObject = playerObj;
            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponentInParent<PlayerMovement>();
            if (playerMovement == null) playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (isWatered) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool shouldBeInRange = (dist <= interactRadius);

        if (shouldBeInRange != playerInRange)
        {
            playerInRange = shouldBeInRange;
            StopAllCoroutines();
            StartCoroutine(FadePromptRoutine(playerInRange ? 1f : 0f, 0.25f));
        }

        if (playerInRange)
        {
            UpdateBillboardAndAnimation();

            bool ePressed = Input.GetKeyDown(interactKey);
            bool fPressed = allowKeyFBackup && Input.GetKeyDown(KeyCode.F);

            if (ePressed || fPressed)
            {
                TriggerWatering();
            }
        }
    }

    private void UpdateBillboardAndAnimation()
    {
        if (promptCanvasObject == null) return;

        // Billboard towards active main camera without switching
        Camera camToLook = (mainCamera != null && mainCamera.isActiveAndEnabled) ? mainCamera : Camera.main;
        if (camToLook != null)
        {
            promptCanvasObject.transform.rotation = camToLook.transform.rotation;
        }

        // Gentle floating hover & pulse
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        promptCanvasObject.transform.localPosition = initialPromptLocalPos + new Vector3(0f, yOffset, 0f);

        float breath = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        promptCanvasObject.transform.localScale = new Vector3(
            initialPromptScale.x * (1f + breath),
            initialPromptScale.y * (1f - breath * 0.6f),
            initialPromptScale.z
        );
    }

    private IEnumerator FadePromptRoutine(float targetAlpha, float duration)
    {
        if (promptCanvasGroup == null) yield break;

        float startAlpha = promptCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        promptCanvasGroup.alpha = targetAlpha;
    }

    public void TriggerWatering()
    {
        if (isWatered) return;
        isWatered = true;

        Debug.Log("[WorldTreeRootInteraction] Player watered root! Beginning Root Revival & Shadow Color Transition...");

        // Update Prompt message and fade it out
        if (promptTextTMP != null)
        {
            promptTextTMP.text = LocalizationManager.Instance != null 
                ? LocalizationManager.Instance.Get("root_prompt_restoring") 
                : "<color=#7CE29C><b>รากไม้โลกกำลังฟื้นคืนพลัง...</b></color>";
        }
        StartCoroutine(FadePromptRoutine(0f, 0.6f));

        // Hide HUD for cinematic presentation
        HideGameplayHUD();

        // Freeze player movement on the ground (DO NOT ascend yet)
        FreezePlayerMovement();

        // Play root water splash SFX
        if (audioSource != null && waterSFX != null)
        {
            audioSource.PlayOneShot(waterSFX);
        }

        // Spawn Water VFX on the Root if assigned
        if (waterSplashVFXPrefab != null)
        {
            Vector3 spawnPos = (waterSplashSpawnPoint != null) ? waterSplashSpawnPoint.position : (transform.position + Vector3.up * 1.5f);
            GameObject vfx = Instantiate(waterSplashVFXPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, 6.0f);
        }

        // Start the Sequence: 1. Transition Shadow Color, THEN 2. Trigger Warp & Ascension
        StartCoroutine(RootRevivalAndAscensionSequence());
    }

    private void FreezePlayerMovement()
    {
        if (playerRootObject == null) FindPlayer();
        if (playerRootObject == null) return;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        MonoBehaviour[] scripts = playerRootObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts)
        {
            string typeName = s.GetType().Name;
            if (typeName == "PlayerInputHandler" || typeName == "PlayerController" || typeName == "PlayerAnimation")
            {
                s.enabled = false;
            }
            if (typeName == "PlayerHealth")
            {
                var healthField = s.GetType().GetField("isInSafeZone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (healthField != null) healthField.SetValue(s, true);
            }
        }

        Rigidbody rb = playerRootObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void HideGameplayHUD()
    {
        string[] hudNames = new string[] { "HUD Canvas", "HUD_Canvas_Quest Variant" };
        foreach (var name in hudNames)
        {
            GameObject hud = GameObject.Find(name);
            if (hud != null)
            {
                CanvasGroup cg = hud.GetComponent<CanvasGroup>();
                if (cg == null) cg = hud.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
        }
    }

    private IEnumerator RootRevivalAndAscensionSequence()
    {
        // -------------------------------------------------------------
        // STEP 1: Gradually change Shadow Color from 1B0E0A to 3D1E16
        // -------------------------------------------------------------
        Debug.Log("[WorldTreeRootInteraction] Step 1: Smoothly transitioning Shadow Color from #1B0E0A to #3D1E16...");
        float shadowElapsed = 0f;

        while (shadowElapsed < shadowTransitionDuration)
        {
            shadowElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(shadowElapsed / shadowTransitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Color currentShadowColor = Color.Lerp(startShadowColor, targetShadowColor, smoothT);
            ApplyShadowColor(currentShadowColor);

            yield return null;
        }

        ApplyShadowColor(targetShadowColor);
        Debug.Log("[WorldTreeRootInteraction] Step 1 Complete: Shadow Color is now #3D1E16!");

        // Brief dramatic pause (0.25s) before ascension begins
        yield return new WaitForSeconds(0.25f);

        // -------------------------------------------------------------
        // STEP 2: Play Warp VFX & Pull Player Upwards ("หลังจากเปลี่ยนเสร็จค่อยเล่น vfx ให้ player ลอยขึ้นไป")
        // -------------------------------------------------------------
        Debug.Log("[WorldTreeRootInteraction] Step 2: Triggering Warp VFX and pulling Player upwards...");

        // Play SFX
        if (audioSource != null)
        {
            if (successChimeSFX != null) audioSource.PlayOneShot(successChimeSFX);
            if (warpWhooshSFX != null) audioSource.PlayOneShot(warpWhooshSFX);
        }

        // Spawn Warp VFX at player position
        if (warpVFXPrefab != null && playerTransform != null)
        {
            Vector3 groundVFXPos = playerTransform.position;
            GameObject warpInstance = Instantiate(warpVFXPrefab, groundVFXPos, Quaternion.identity);
            Destroy(warpInstance, 10.0f);
        }

        // Disable physics and colliders for smooth levitation
        Rigidbody rb = playerRootObject != null ? playerRootObject.GetComponent<Rigidbody>() : null;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (playerRootObject != null)
        {
            Collider[] colliders = playerRootObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }

        // Keep active main camera without switching ("ไม่ต้องสลับมุมกล้อง")
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null && playerTransform != null && mainCamera.transform.IsChildOf(playerTransform))
        {
            mainCamera.transform.SetParent(null, true);
        }

        // Attach dynamic ascending celestial point light on player
        GameObject ascendLightObj = new GameObject("AscensionGlowLight");
        ascendLightObj.transform.SetParent(playerTransform);
        ascendLightObj.transform.localPosition = new Vector3(0f, 1f, 0f);
        Light ascendLight = ascendLightObj.AddComponent<Light>();
        ascendLight.type = LightType.Point;
        ascendLight.color = new Color(0.48f, 1.0f, 0.78f);
        ascendLight.range = 14f;
        ascendLight.intensity = 2f;

        // Screen fade overlay Canvas
        GameObject fadeCanvasObj = new GameObject("Ascension_ScreenFade_Canvas");
        Canvas fadeCanvas = fadeCanvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;
        CanvasGroup fadeCanvasGroup = fadeCanvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject fadeImageObj = new GameObject("FadeImage");
        fadeImageObj.transform.SetParent(fadeCanvasObj.transform, false);
        Image fadeImage = fadeImageObj.AddComponent<Image>();
        fadeImage.color = new Color(0.95f, 1.0f, 0.96f, 1.0f);
        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // Upward Pulling Animation Loop ("ทำให้เหมือนมีอะไรดึง Player ขึ้นไปข้างบน")
        Vector3 startPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        float ascendElapsed = 0f;

        while (ascendElapsed < ascendDuration)
        {
            ascendElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(ascendElapsed / ascendDuration);

            float pullCurve = Mathf.Pow(t, 1.6f);
            float currentY = startPos.y + pullCurve * ascendHeight;

            float swayX = Mathf.Sin(ascendElapsed * 9.5f) * 0.08f * (1f - t * 0.4f);
            float swayZ = Mathf.Cos(ascendElapsed * 8.2f) * 0.08f * (1f - t * 0.4f);

            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(startPos.x + swayX, currentY, startPos.z + swayZ);
            }

            if (ascendLight != null)
            {
                ascendLight.intensity = Mathf.Lerp(2.5f, 11f, t);
                ascendLight.range = Mathf.Lerp(12f, 22f, t);
            }

            if (mainCamera != null && playerTransform != null)
            {
                Vector3 playerLookPoint = playerTransform.position + Vector3.up * 0.4f;
                Vector3 toPlayer = playerLookPoint - mainCamera.transform.position;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRot, Time.deltaTime * 3.5f);
                }
            }

            if (t > 0.72f)
            {
                float fadeT = (t - 0.72f) / (1f - 0.72f);
                fadeCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeT);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(0.2f);

        // -------------------------------------------------------------
        // STEP 3: Transition to EndScene ("พร้อมไปที่ฉาก Endscene")
        // -------------------------------------------------------------
        if (transitionToEndScene && !string.IsNullOrEmpty(endSceneName))
        {
            Debug.Log("[WorldTreeRootInteraction] Ascension complete! Loading " + endSceneName + "...");
            SceneManager.LoadScene(endSceneName);
        }
    }

    private void OnDestroy()
    {
        // Reset Shadow Color to 1B0E0A when exiting/destroying
        ApplyShadowColor(startShadowColor);
    }

    private void OnApplicationQuit()
    {
        ApplyShadowColor(startShadowColor);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
