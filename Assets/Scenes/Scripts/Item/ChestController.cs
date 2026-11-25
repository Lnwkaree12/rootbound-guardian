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

    [SerializeField] 
    private Vector3 camFocus ;

    public bool IsOpened => isOpened;

    [Header("Save Settings")]
    [SerializeField] private string chestID;

    public string ChestID => chestID;

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

        CheckInitialOpenState();
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

        // 🌟 บันทึกสถานะว่าหีบใบนี้ถูกเปิดแล้วลงในระบบเซฟ
        
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.MarkChestAsOpened(chestID);
        }
        

        SetPromptActive(false);
        StartCoroutine(ChestOpeningCinematicRoutine());
    }

    private IEnumerator ChestOpeningCinematicRoutine()
    {
        // 1. Lock Player Movement briefly while opening the chest (no camera zoom)
        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (playerController == null && playerObject != null) playerController = playerObject.GetComponentInParent<PlayerController>() ?? playerObject.GetComponentInChildren<PlayerController>();
        if (playerMovement == null && playerObject != null) playerMovement = playerObject.GetComponentInParent<PlayerMovement>() ?? playerObject.GetComponentInChildren<PlayerMovement>();
        if (playerInventory == null && playerObject != null) playerInventory = playerObject.GetComponentInParent<Inventory>() ?? playerObject.GetComponentInChildren<Inventory>();

        if (playerController != null) playerController.enabled = false;
        if (playerMovement != null) playerMovement.Move(Vector2.zero);

        // 2. Play Open Sound
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // 3. Animate Lid Opening
        float lidDuration = 0.2f;
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

        // 4. Activate Key / Reward & Radiant Light Burst
        Vector3 keyTargetScale = new Vector3(0.24f, 0.24f, 0.24f);
        Vector3 keyStartScale = new Vector3(0.12f, 0.12f, 0.12f);
        float currentSpinAngle = 0f;

        float keyZRot = (keyTransform != null && keyTransform.GetComponentInChildren<SpriteRenderer>() != null) ? 0f : -90f;

        if (keyTransform != null)
        {
            keyTransform.gameObject.SetActive(true);
            keyTransform.localPosition = keyInitialLocalPos;
            keyTransform.localScale = keyStartScale;
            keyTransform.localRotation = Quaternion.Euler(0f, 0f, keyZRot);
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

        // 5. Reward Rising & Spinning (keep gameplay camera unchanged)
        float riseDuration = 0.85f;
        float riseElapsed = 0f;
        Vector3 keyTargetLocalPos = keyInitialLocalPos + Vector3.up * keyRiseHeight;

        while (riseElapsed < riseDuration)
        {
            riseElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, riseElapsed / riseDuration);

            if (keyTransform != null)
            {
                currentSpinAngle += keySpinSpeed * Time.deltaTime;
                keyTransform.localPosition = Vector3.Lerp(keyInitialLocalPos, keyTargetLocalPos, t);
                keyTransform.localScale = Vector3.Lerp(keyStartScale, keyTargetScale, t);
                keyTransform.localRotation = Quaternion.Euler(0f, currentSpinAngle, keyZRot);
            }

            if (radiantLight != null)
            {
                float lightCurve = Mathf.Sin(t * Mathf.PI);
                radiantLight.intensity = Mathf.Lerp(0f, 5.0f, lightCurve);
            }

            yield return null;
        }

        // 6. Award the configured item; fall back to the legacy quest key when unset.
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

        // 7. Reward Scales Down & Disperse
        float returnDuration = 0.35f;
        float returnElapsed = 0f;

        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, returnElapsed / returnDuration);

            if (keyTransform != null)
            {
                currentSpinAngle += keySpinSpeed * 1.5f * Time.deltaTime;
                keyTransform.localScale = Vector3.Lerp(keyTargetScale, Vector3.zero, t);
                keyTransform.localRotation = Quaternion.Euler(0f, currentSpinAngle, keyZRot);
            }

            if (radiantLight != null)
            {
                radiantLight.intensity = Mathf.Lerp(3.5f, 0f, t);
            }

            yield return null;
        }

        if (radiantLight != null) radiantLight.enabled = false;
        if (keyTransform != null)
        {
            keyTransform.gameObject.SetActive(false);
            keyTransform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }

        // 8. Unlock Player Movement
        if (playerController != null) playerController.enabled = true;

        Debug.Log("[ChestController] Chest opening complete (no camera zoom).");
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(chestID))
        {
            chestID = gameObject.name + "_" + transform.position.ToString();
        }
    }

    private void CheckInitialOpenState()
    {
        // สมมติว่าคุณเก็บสถานะ Chest IDs ไว้ใน CheckpointManager หรือระบบ Save ของคุณ
        // ตัวอย่างเช่นถ้า CheckpointManager มีฟังก์ชันเช็ค IsChestOpened(chestID)

        if (CheckpointManager.Instance != null && CheckpointManager.Instance.IsChestOpened(chestID))
        {
            isOpened = true;
            if (lidTransform != null)
            {
                Quaternion lidTargetRot = lidInitialRot * Quaternion.Euler(lidOpenAngle, 0f, 0f);
                lidTransform.localRotation = lidTargetRot;
            }
            if (radiantLight != null) radiantLight.enabled = false;
            if (keyTransform != null) keyTransform.gameObject.SetActive(false);
            return;
        }
    }

    public void ResetChestState(bool opened)
    {
        isOpened = opened;

        if (lidTransform != null)
        {
            if (isOpened)
            {
                Quaternion lidTargetRot = lidInitialRot * Quaternion.Euler(lidOpenAngle, 0f, 0f);
                lidTransform.localRotation = lidTargetRot;
            }
            else
            {
                lidTransform.localRotation = lidInitialRot;
            }
        }

        if (keyTransform != null)
        {
            keyTransform.gameObject.SetActive(false);
        }

        if (radiantLight != null)
        {
            radiantLight.enabled = false;
        }

        // เปิด Prompt กลับมาถ้ายังไม่เคยเปิด
        if (!isOpened && isPlayerInRange)
        {
            SetPromptActive(true);
        }
    }
}

