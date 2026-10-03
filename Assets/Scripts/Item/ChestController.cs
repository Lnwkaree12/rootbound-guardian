using System.Collections;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    [Header("Chest Components")]
    [SerializeField] private Transform lidTransform;
    [SerializeField] private Transform keyTransform;
    [SerializeField] private ItemData rewardItem;
    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private ParticleSystem radiantBurstVFX;
    [SerializeField] private ParticleSystem keyAuraVFX;
    [SerializeField] private Light radiantLight;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip fanfareSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Cinematic Settings")]
    [SerializeField] private float interactRange = 3.5f;
    [SerializeField] private float lidOpenAngle = -85f;
    [SerializeField] private float keyRiseHeight = 0.42f;
    [SerializeField] private float keySpinSpeed = 160f;
    [SerializeField] private float cinematicDuration = 3.2f;
    [SerializeField] private float targetCameraFOV = 38f;

    [Header("State")]
    [SerializeField] private bool isOpened = false;

    private bool isPlayerInRange = false;
    private GameObject playerObject;
    private PlayerInputHandler inputHandler;
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private Inventory playerInventory;
    private TreeInteractUI promptUI;

    private Quaternion lidInitialRot;
    private Vector3 keyInitialLocalPos;

    public bool IsOpened => isOpened;

    public void ConfigureChest(Transform lid, Transform key, GameObject promptUIObj, ParticleSystem burstVFX, ParticleSystem auraVFX, Light lightComp, AudioClip sfxOpen, AudioClip sfxFanfare)
    {
        lidTransform = lid;
        keyTransform = key;
        interactPromptUI = promptUIObj;
        radiantBurstVFX = burstVFX;
        keyAuraVFX = auraVFX;
        radiantLight = lightComp;
        openSound = sfxOpen;
        fanfareSound = sfxFanfare;
    }

    private void Awake()
    {
        if (lidTransform != null)
        {
            lidInitialRot = lidTransform.localRotation;
        }

        if (keyTransform != null)
        {
            keyInitialLocalPos = keyTransform.localPosition;
            // Key starts hidden inside closed chest
            keyTransform.gameObject.SetActive(false);
        }

        if (radiantLight != null)
        {
            radiantLight.enabled = false;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (interactPromptUI != null)
        {
            promptUI = interactPromptUI.GetComponent<TreeInteractUI>();
            SetPromptActive(false);
        }
    }

    private void Update()
    {
        if (isOpened) return;

        // Proximity detection (Horizontal distance)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            float dist = GetHorizontalDistance(transform.position, player.transform.position);
            if (!isPlayerInRange && dist <= interactRange)
            {
                SetPlayerInRange(true, player);
            }
            else if (isPlayerInRange && dist > (interactRange + 1.2f))
            {
                SetPlayerInRange(false, null);
            }
        }

        if (!isPlayerInRange) return;

        // Check Interact input (E key or InputHandler)
        bool interactPressed = (inputHandler != null && inputHandler.InteractPressed) || Input.GetKeyDown(KeyCode.E);
        if (interactPressed && !isOpened)
        {
            OpenChest();
        }
    }

    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void SetPlayerInRange(bool inRange, GameObject pGO)
    {
        isPlayerInRange = inRange;
        playerObject = inRange ? pGO : null;

        if (inRange && pGO != null)
        {
            inputHandler = pGO.GetComponentInParent<PlayerInputHandler>() ?? pGO.GetComponentInChildren<PlayerInputHandler>();
            playerController = pGO.GetComponentInParent<PlayerController>() ?? pGO.GetComponentInChildren<PlayerController>();
            playerMovement = pGO.GetComponentInParent<PlayerMovement>() ?? pGO.GetComponentInChildren<PlayerMovement>();
            playerInventory = pGO.GetComponentInParent<Inventory>() ?? pGO.GetComponentInChildren<Inventory>();

            SetPromptActive(true);
        }
        else
        {
            inputHandler = null;
            playerController = null;
            playerMovement = null;
            playerInventory = null;

            SetPromptActive(false);
        }
    }

    private void SetPromptActive(bool active)
    {
        if (interactPromptUI == null) return;

        if (promptUI != null)
        {
            if (active) promptUI.Show();
            else promptUI.Hide();
        }
        else
        {
            interactPromptUI.SetActive(active);
        }
    }

    public void OpenChest()
    {
        if (isOpened) return;
        isOpened = true;

        SetPromptActive(false);
        StartCoroutine(ChestOpeningCinematicRoutine());
    }

    private IEnumerator ChestOpeningCinematicRoutine()
    {
        // 1. Lock Player Movement for cinematic
        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (playerController == null && playerObject != null) playerController = playerObject.GetComponentInParent<PlayerController>() ?? playerObject.GetComponentInChildren<PlayerController>();
        if (playerMovement == null && playerObject != null) playerMovement = playerObject.GetComponentInParent<PlayerMovement>() ?? playerObject.GetComponentInChildren<PlayerMovement>();
        if (playerInventory == null && playerObject != null) playerInventory = playerObject.GetComponentInParent<Inventory>() ?? playerObject.GetComponentInChildren<Inventory>();

        if (playerController != null) playerController.enabled = false;
        if (playerMovement != null) playerMovement.Move(Vector2.zero);

        // 2. Hide player mesh/renderers during cinematic so player's back doesn't obstruct camera/key
        Renderer[] playerRenderers = null;
        if (playerObject != null)
        {
            playerRenderers = playerObject.GetComponentsInChildren<Renderer>();
            foreach (var r in playerRenderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        // 3. Play Open Sound
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // 4. Cache Camera info
        Camera mainCam = Camera.main ?? FindFirstObjectByType<Camera>();
        float originalFOV = 60f;
        Vector3 originalCamLocalPos = new Vector3(0f, 1.13f, -2.36f);
        Quaternion originalCamLocalRot = Quaternion.Euler(17.36f, 0f, 0f);
        Transform camTransform = null;

        if (mainCam != null)
        {
            originalFOV = mainCam.fieldOfView;
            camTransform = mainCam.transform;
            originalCamLocalPos = camTransform.localPosition;
            originalCamLocalRot = camTransform.localRotation;
        }

        // 5. Animate Lid Opening
        float lidDuration = 0.6f;
        float lidElapsed = 0f;
        Quaternion lidTargetRot = lidInitialRot * Quaternion.Euler(lidOpenAngle, 0f, 0f);

        while (lidElapsed < lidDuration)
        {
            lidElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, lidElapsed / lidDuration);
            if (lidTransform != null)
            {
                lidTransform.localRotation = Quaternion.Slerp(lidInitialRot, lidTargetRot, t);
            }
            yield return null;
        }
        if (lidTransform != null) lidTransform.localRotation = lidTargetRot;

        // 6. Activate Key & Radiant Light Burst (แสงจ้าออกมา)
        Vector3 keyTargetScale = new Vector3(0.24f, 0.24f, 0.24f);
        Vector3 keyStartScale = new Vector3(0.12f, 0.12f, 0.12f);
        float currentSpinAngle = 0f;

        if (keyTransform != null)
        {
            keyTransform.gameObject.SetActive(true);
            keyTransform.localPosition = keyInitialLocalPos;
            keyTransform.localScale = keyStartScale;
            keyTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        if (radiantLight != null)
        {
            radiantLight.enabled = true;
            radiantLight.intensity = 0f;
        }

        if (radiantBurstVFX != null)
        {
            radiantBurstVFX.gameObject.SetActive(true);
            radiantBurstVFX.Play(true);
        }

        if (keyAuraVFX != null)
        {
            keyAuraVFX.gameObject.SetActive(true);
            keyAuraVFX.Play(true);
        }

        if (fanfareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fanfareSound);
        }

        // 7. Cinematic Close-up Zoom & Key Rising
        float riseDuration = 1.6f;
        float riseElapsed = 0f;
        Vector3 keyTargetLocalPos = keyInitialLocalPos + Vector3.up * keyRiseHeight;

        Vector3 camStartWorldPos = camTransform != null ? camTransform.position : Vector3.zero;
        Quaternion camStartWorldRot = camTransform != null ? camTransform.rotation : Quaternion.identity;

        // Calculate close-up world position focusing directly on the rising key hovering above chest
        Vector3 keyFocusWorldPos = transform.position + Vector3.up * (keyInitialLocalPos.y + keyRiseHeight + 0.1f);
        Vector3 camCloseUpWorldPos = keyFocusWorldPos + new Vector3(0.45f, 0.35f, -1.9f);
        Quaternion camCloseUpWorldRot = Quaternion.LookRotation((keyFocusWorldPos - Vector3.up * 0.1f) - camCloseUpWorldPos);
        float closeUpFOV = 48f;

        while (riseElapsed < riseDuration)
        {
            riseElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, riseElapsed / riseDuration);

            // Key rises up, scales into full view and spins upright
            if (keyTransform != null)
            {
                currentSpinAngle += keySpinSpeed * Time.deltaTime;
                keyTransform.localPosition = Vector3.Lerp(keyInitialLocalPos, keyTargetLocalPos, t);
                keyTransform.localScale = Vector3.Lerp(keyStartScale, keyTargetScale, t);
                keyTransform.localRotation = Quaternion.Euler(0f, currentSpinAngle, -90f);
            }

            // Camera smoothly glides into dramatic Close-Up
            if (mainCam != null)
            {
                mainCam.fieldOfView = Mathf.Lerp(originalFOV, closeUpFOV, t);
                camTransform.position = Vector3.Lerp(camStartWorldPos, camCloseUpWorldPos, t);
                camTransform.rotation = Quaternion.Slerp(camStartWorldRot, camCloseUpWorldRot, t);
            }

            // Light flares up brightly (แสงจ้าออกมา)
            if (radiantLight != null)
            {
                float lightCurve = Mathf.Sin(t * Mathf.PI);
                radiantLight.intensity = Mathf.Lerp(0f, 6.0f, lightCurve);
            }

            yield return null;
        }

        // 8. Majestic Hover Pause while Key Shines in Close-up
        float hoverDuration = 1.2f;
        float hoverElapsed = 0f;

        while (hoverElapsed < hoverDuration)
        {
            hoverElapsed += Time.deltaTime;

            if (keyTransform != null)
            {
                currentSpinAngle += keySpinSpeed * 1.2f * Time.deltaTime;
                float bob = Mathf.Sin(Time.time * 4f) * 0.02f;
                keyTransform.localPosition = keyTargetLocalPos + new Vector3(0f, bob, 0f);
                keyTransform.localRotation = Quaternion.Euler(0f, currentSpinAngle, -90f);
            }

            if (radiantLight != null)
            {
                radiantLight.intensity = 4.0f + Mathf.Sin(Time.time * 8f) * 1.2f;
            }

            yield return null;
        }

        // 9. Award the configured item; fall back to the legacy quest key when unset.
        if (rewardItem != null && playerInventory != null)
        {
            if (!playerInventory.AddItem(rewardItem))
            {
                Debug.LogWarning("[ChestController] Inventory is full; chest reward was not added.");
            }
        }
        else if (rewardItem == null && QuestManager.Instance != null)
        {
            QuestManager.Instance.CollectKey();
        }
        else if (rewardItem != null)
        {
            Debug.LogWarning("[ChestController] Player Inventory was not found; chest reward was not added.");
        }

        // 10. Camera Return & Key Disperse
        float returnDuration = 0.9f;
        float returnElapsed = 0f;

        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, returnElapsed / returnDuration);

            // Camera smoothly returns back to original gameplay view
            if (mainCam != null)
            {
                mainCam.fieldOfView = Mathf.Lerp(closeUpFOV, originalFOV, t);
                Vector3 targetOrigWorldPos = camTransform.parent != null ? camTransform.parent.TransformPoint(originalCamLocalPos) : camStartWorldPos;
                Quaternion targetOrigWorldRot = camTransform.parent != null ? camTransform.parent.rotation * originalCamLocalRot : camStartWorldRot;
                camTransform.position = Vector3.Lerp(camCloseUpWorldPos, targetOrigWorldPos, t);
                camTransform.rotation = Quaternion.Slerp(camCloseUpWorldRot, targetOrigWorldRot, t);
            }

            // Key scales down into player/inventory
            if (keyTransform != null)
            {
                currentSpinAngle += keySpinSpeed * 1.5f * Time.deltaTime;
                keyTransform.localScale = Vector3.Lerp(keyTargetScale, Vector3.zero, t);
                keyTransform.localRotation = Quaternion.Euler(0f, currentSpinAngle, -90f);
            }

            // Light dims down
            if (radiantLight != null)
            {
                radiantLight.intensity = Mathf.Lerp(4.0f, 0f, t);
            }

            yield return null;
        }

        // Finalize camera and restore exact original local transform
        if (mainCam != null)
        {
            mainCam.fieldOfView = originalFOV;
            camTransform.localPosition = originalCamLocalPos;
            camTransform.localRotation = originalCamLocalRot;
        }

        if (radiantLight != null) radiantLight.enabled = false;
        if (keyTransform != null)
        {
            keyTransform.gameObject.SetActive(false);
            keyTransform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }

        // 11. Restore Player Visibility & Unlock Movement
        if (playerRenderers != null)
        {
            foreach (var r in playerRenderers)
            {
                if (r != null) r.enabled = true;
            }
        }

        if (playerController != null) playerController.enabled = true;

        Debug.Log("[ChestController] Chest opening cinematic complete! Camera returned to original angle.");
    }
}

