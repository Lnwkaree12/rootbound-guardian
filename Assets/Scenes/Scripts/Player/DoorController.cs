using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;
using TMPro;

public class DoorController : MonoBehaviour
{
    [Header("Single Door Panel Reference (Legacy/Single Doors)")]
    public Transform doorPanel;
    public float openAngle = -90f;

    [Header("Double Door References (Castle Gate)")]
    public Transform leftDoorLeaf;
    public Transform rightDoorLeaf;
    public float doubleDoorOpenAngle = 90f;

    [Header("Door Settings")]
    public float openSpeed = 2.0f;
    public float openDuration = 1.8f;
    public float interactRadius = 8.0f;
    public bool autoUnlockOnContact = false;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public bool allowKeyFBackup = true;

    [Header("Colliders")]
    public Collider barrierCollider;
    public Collider interactionTrigger;

    [Header("Key Requirement Settings")]
    [Tooltip("ระบุชื่อของกุญแจที่ต้องใช้ (ถ้าปล่อยว่างไว้ ขอแค่เป็น ItemType.Key อันไหนก็ได้จะใช้เปิดได้เลย)")]
    public string requiredKeyName = "";
    [Tooltip("ต้องการให้ลบกุญแจออกจาก Inventory เมื่อใช้งานสำเร็จหรือไม่")]
    public bool consumeKeyOnUse = true;

    [Header("Audio")]
    public AudioClip keyUnlockSfx;
    public AudioClip doorOpenSfx;
    private AudioSource audioSource;

    [Header("Victory Settings")]
    public bool triggerWinOnOpen = true;
    public float winUIDelay = 1.2f;

    [Header("Floating Prompt UI")]
    public GameObject promptObject;
    public TextMeshProUGUI promptTextTMP;
    public Text promptTextLegacy;
    public Image promptKeyIcon;
    public Image promptBgImage;

    private bool isOpen = false;
    private bool isOpening = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion closedLeftRot;
    private Quaternion openLeftRot;
    private Quaternion closedRightRot;
    private Quaternion openRightRot;

    private bool isDoubleDoor = false;

    [SerializeField] private bool playerInTrigger = false;

    private PlayerMovement cachedPlayer;
    private readonly HashSet<Collider> playerCollidersInTrigger = new HashSet<Collider>();
    private Coroutine flashCoroutine;
    private Vector3 promptBaseLocalPos;

    public bool IsOpen => isOpen;

    void Start()
    {
        InitializeDoors();
        InitializeAudio();
        SetupFloatingPrompt();
    }

    private void InitializeDoors()
    {
        if (leftDoorLeaf == null) leftDoorLeaf = transform.Find("Door_Leaf_Left");
        if (rightDoorLeaf == null) rightDoorLeaf = transform.Find("Door_Leaf_Right");

        if (leftDoorLeaf != null && rightDoorLeaf != null)
        {
            isDoubleDoor = true;
            closedLeftRot = leftDoorLeaf.localRotation;
            closedRightRot = rightDoorLeaf.localRotation;

            openLeftRot = Quaternion.Euler(0f, doubleDoorOpenAngle, 0f) * closedLeftRot;
            openRightRot = Quaternion.Euler(0f, -doubleDoorOpenAngle, 0f) * closedRightRot;

            EnsureDoorLeafForwarder(leftDoorLeaf);
            EnsureDoorLeafForwarder(rightDoorLeaf);
        }

        if (doorPanel == null)
        {
            doorPanel = transform.Find("DoorPanel");
        }

        if (doorPanel != null)
        {
            closedRotation = doorPanel.localRotation;
            openRotation = Quaternion.Euler(0f, openAngle, 0f) * closedRotation;

            DoorPanelCollisionForwarder forwarder = doorPanel.GetComponent<DoorPanelCollisionForwarder>();
            if (forwarder == null)
            {
                forwarder = doorPanel.gameObject.AddComponent<DoorPanelCollisionForwarder>();
            }
            forwarder.parentDoor = this;
        }

        if (barrierCollider == null)
        {
            Collider[] cols = GetComponents<Collider>();
            foreach (var c in cols)
            {
                if (!c.isTrigger)
                {
                    barrierCollider = c;
                    break;
                }
            }
        }

        if (interactionTrigger == null)
        {
            Collider[] cols = GetComponents<Collider>();
            foreach (var c in cols)
            {
                if (c.isTrigger)
                {
                    interactionTrigger = c;
                    break;
                }
            }
        }
    }

    private void EnsureDoorLeafForwarder(Transform leaf)
    {
        if (leaf == null) return;
        DoorPanelCollisionForwarder forwarder = leaf.GetComponent<DoorPanelCollisionForwarder>();
        if (forwarder == null)
        {
            forwarder = leaf.gameObject.AddComponent<DoorPanelCollisionForwarder>();
        }
        forwarder.parentDoor = this;
    }

    private void InitializeAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }

        if (keyUnlockSfx == null)
        {
#if UNITY_EDITOR
            keyUnlockSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key.mp3");
#endif
        }
    }

    private void SetupFloatingPrompt()
    {
        if (promptObject != null)
        {
            promptBaseLocalPos = promptObject.transform.localPosition;
            return;
        }

        Vector3 spawnLocalPos = new Vector3(-0.6f, 1.4f, 0f);
        if (!isDoubleDoor)
        {
            spawnLocalPos = new Vector3(0f, 2.35f, 0f);
        }

        GameObject promptCanvasGO = new GameObject("DoorPromptCanvas");
        promptCanvasGO.transform.SetParent(transform, false);
        promptCanvasGO.transform.localPosition = spawnLocalPos;
        promptBaseLocalPos = spawnLocalPos;

        Canvas canvas = promptCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRect = promptCanvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(460f, 90f);
        promptCanvasGO.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f);

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(promptCanvasGO.transform, false);
        promptBgImage = bgGO.AddComponent<Image>();
        promptBgImage.color = new Color(0.10f, 0.08f, 0.06f, 0.92f);

#if UNITY_EDITOR
        Sprite pillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Cute_Tree_Bubble_Bg.png");
        if (pillSprite == null) pillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Prompt_Pill_Bg.png");
        if (pillSprite != null)
        {
            promptBgImage.sprite = pillSprite;
            promptBgImage.type = Image.Type.Sliced;
        }
#endif

        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject iconGO = new GameObject("KeyIcon");
        iconGO.transform.SetParent(bgGO.transform, false);
        promptKeyIcon = iconGO.AddComponent<Image>();
#if UNITY_EDITOR
        Sprite keySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Cute_Key_E_Candy.png");
        if (keySprite == null) keySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Prompt_Key_E_Cute.png");
        if (keySprite != null)
        {
            promptKeyIcon.sprite = keySprite;
        }
#endif
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector3(48f, 0f, 0f);
        iconRect.sizeDelta = new Vector2(64f, 64f);

        GameObject textGO = new GameObject("TextTMP");
        textGO.transform.SetParent(bgGO.transform, false);
        promptTextTMP = textGO.AddComponent<TextMeshProUGUI>();

#if UNITY_EDITOR
        TMP_FontAsset thaiFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_Thai_SDF.asset");
        if (thaiFont != null)
        {
            promptTextTMP.font = thaiFont;
        }
#endif
        promptTextTMP.fontSize = 28f;
        promptTextTMP.alignment = TextAlignmentOptions.MidlineLeft;
        promptTextTMP.color = new Color(1f, 0.92f, 0.65f);
        promptTextTMP.text = "กด [E] เพื่อเปิดประตู";

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(90f, 5f);
        textRect.offsetMax = new Vector2(-20f, -5f);

        promptObject = promptCanvasGO;
        promptObject.SetActive(false);
    }

    void Update()
    {
        if (isOpen || isOpening)
        {
            if (promptObject != null && promptObject.activeSelf)
            {
                promptObject.SetActive(false);
            }
            return;
        }

        // เช็คจาก Trigger อย่างเดียวเท่านั้น
        if (playerInTrigger)
        {
            bool hasKey = CheckPlayerHasKey();
            UpdatePromptDisplay(hasKey);

            bool interactPressed = Input.GetKeyDown(interactKey) || (allowKeyFBackup && Input.GetKeyDown(KeyCode.F));
            if (interactPressed)
            {
                TryOpenDoor();
            }
        }
        else
        {
            if (promptObject != null && promptObject.activeSelf)
            {
                promptObject.SetActive(false);
            }
        }
    }

    private void UpdatePromptDisplay(bool hasKey)
    {
        if (promptObject == null) return;
        if (!promptObject.activeSelf) promptObject.SetActive(true);

        float hover = Mathf.Sin(Time.time * 3.5f) * 0.05f;
        promptObject.transform.localPosition = promptBaseLocalPos + new Vector3(0f, hover, 0f);

        if (flashCoroutine != null) return;

        if (promptTextTMP != null)
        {
            if (hasKey)
            {
                promptTextTMP.text = "กด <color=#FFEE44>[E]</color> เพื่อเปิดประตู";
                promptTextTMP.color = new Color(1f, 0.95f, 0.7f);
                if (promptKeyIcon != null) promptKeyIcon.gameObject.SetActive(true);
            }
            else
            {
                promptTextTMP.text = "<color=#FFAA77>ประตูล็อคแน่นหนา (ต้องหากุญแจก่อน)</color>";
                promptTextTMP.color = new Color(1f, 0.7f, 0.5f);
                if (promptKeyIcon != null) promptKeyIcon.gameObject.SetActive(false);
            }
        }

        if (promptTextLegacy != null)
        {
            promptTextLegacy.text = hasKey ? "กด [E] เพื่อเปิดประตู" : "ประตูล็อคแน่นหนา (ต้องหากุญแจก่อน)";
        }
    }

    private Inventory GetPlayerInventory()
    {
        return cachedPlayer != null ? cachedPlayer.GetComponent<Inventory>() : null;
    }

    public bool CheckPlayerHasKey()
    {
        if (!playerInTrigger || cachedPlayer == null) return false;

        if (QuestManager.Instance != null && QuestManager.Instance.hasKey)
        {
            return true;
        }

        Inventory inventory = GetPlayerInventory();
        if (inventory != null)
        {
            foreach (ItemData item in inventory.Items)
            {
                if (item == null) continue;

                if (item.itemType == ItemType.Key)
                {
                    if (string.IsNullOrEmpty(requiredKeyName) || item.itemName == requiredKeyName)
                    {
                        return true;
                    }
                }
            }

            if (inventory.HasKey()) return true;
        }

        return false;
    }

    private void ConsumeKeyFromInventory()
    {
        if (!consumeKeyOnUse) return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UseKeyOnDoor();
        }

        Inventory inventory = GetPlayerInventory();
        if (inventory == null) return;

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemData item = inventory.Items[i];
            if (item != null && item.itemType == ItemType.Key)
            {
                if (string.IsNullOrEmpty(requiredKeyName) || item.itemName == requiredKeyName)
                {
                    Debug.Log($"[DoorController] Consumed key: {item.itemName}");
                    inventory.RemoveItem(i);
                    break;
                }
            }
        }
    }

    public void TryOpenDoor()
    {
        if (isOpen || isOpening || !playerInTrigger || cachedPlayer == null) return;

        if (CheckPlayerHasKey())
        {
            StartCoroutine(OpenDoorSequence());
        }
        else
        {
            Debug.Log("[DoorController] Player does not have the required key.");
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashLockedPrompt());
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        isOpening = true;
        Debug.Log("[DoorController] Key found! Opening door with animation...");

        if (promptObject != null) promptObject.SetActive(false);

        if (barrierCollider != null)
        {
            barrierCollider.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.isTrigger = true;
        }

        ConsumeKeyFromInventory();
        PlayDoorOpenAudio();

        if (cachedPlayer != null)
        {
            cachedPlayer.Move(Vector2.zero);
        }

        if (triggerWinOnOpen)
        {
            StartCoroutine(TriggerWinDelayed(winUIDelay));
        }

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);

            if (isDoubleDoor)
            {
                if (leftDoorLeaf != null)
                {
                    leftDoorLeaf.localRotation = Quaternion.Slerp(closedLeftRot, openLeftRot, t);
                }
                if (rightDoorLeaf != null)
                {
                    rightDoorLeaf.localRotation = Quaternion.Slerp(closedRightRot, openRightRot, t);
                }
            }
            else if (doorPanel != null)
            {
                doorPanel.localRotation = Quaternion.Slerp(closedRotation, openRotation, t);
            }

            yield return null;
        }

        if (isDoubleDoor)
        {
            if (leftDoorLeaf != null) leftDoorLeaf.localRotation = openLeftRot;
            if (rightDoorLeaf != null) rightDoorLeaf.localRotation = openRightRot;
        }
        else if (doorPanel != null)
        {
            doorPanel.localRotation = openRotation;
        }

        isOpen = true;
        isOpening = false;
        Debug.Log("[DoorController] Door fully opened!");
    }

    private IEnumerator TriggerWinDelayed(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        TriggerWinUI();
    }

    private void TriggerWinUI()
    {
        WinUIManager[] managers = FindObjectsByType<WinUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        WinUIManager winManager = null;
        int highestSortingOrder = int.MinValue;

        foreach (WinUIManager manager in managers)
        {
            if (manager == null || manager.winPanel == null || manager.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            Canvas canvas = manager.winPanel.GetComponentInParent<Canvas>();
            int sortingOrder = canvas != null ? canvas.sortingOrder : 0;
            if (winManager == null || sortingOrder > highestSortingOrder)
            {
                winManager = manager;
                highestSortingOrder = sortingOrder;
            }
        }

        if (winManager == null)
        {
            Debug.LogError("[DoorController] No WinUIManager with a WinPanel was found in the active scene.");
            return;
        }

        Debug.Log("[DoorController] Triggering Game Win UI!");
        winManager.TriggerWin(0.2f);
    }

    private IEnumerator FlashLockedPrompt()
    {
        if (promptObject == null) yield break;
        promptObject.SetActive(true);

        if (promptTextTMP != null)
        {
            promptTextTMP.text = "<color=#FF3333>ประตูล็อคแน่นหนา! ต้องใช้กุญแจ</color>";
        }
        if (promptTextLegacy != null)
        {
            promptTextLegacy.text = "ประตูล็อคแน่นหนา! ต้องใช้กุญแจ";
            promptTextLegacy.color = Color.red;
        }
        if (promptBgImage != null)
        {
            promptBgImage.color = new Color(0.4f, 0.1f, 0.1f, 0.95f);
        }

        yield return new WaitForSeconds(1.8f);

        if (promptBgImage != null)
        {
            promptBgImage.color = new Color(0.10f, 0.08f, 0.06f, 0.92f);
        }

        flashCoroutine = null;
    }

    private void PlayDoorOpenAudio()
    {
        if (audioSource == null) return;

        if (keyUnlockSfx != null)
        {
            audioSource.PlayOneShot(keyUnlockSfx);
        }

        if (doorOpenSfx != null)
        {
            audioSource.PlayOneShot(doorOpenSfx);
        }
        else
        {
            AudioClip unlockClip = CreateProceduralUnlockClip();
            if (unlockClip != null)
            {
                audioSource.PlayOneShot(unlockClip);
            }
        }
    }

    private AudioClip CreateProceduralUnlockClip()
    {
        int sampleRate = 44100;
        float duration = 1.2f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float click = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-50f * t);
            float groan = Mathf.Sin(2f * Mathf.PI * (80f + t * 30f) * t) * Mathf.Exp(-1.8f * t) * 0.6f;
            float rumble = (Mathf.PerlinNoise(t * 15f, 0.5f) - 0.5f) * Mathf.Exp(-1.2f * t) * 0.4f;
            samples[i] = (click * 0.3f + groan * 0.45f + rumble * 0.35f);
        }

        AudioClip clip = AudioClip.Create("ProceduralCastleDoorUnlock", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || (cachedPlayer != null && cachedPlayer != player)) return;

        cachedPlayer = player;
        playerCollidersInTrigger.Add(other);
        playerInTrigger = playerCollidersInTrigger.Count > 0;

        if (autoUnlockOnContact && CheckPlayerHasKey() && !isOpen && !isOpening)
        {
            TryOpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerCollidersInTrigger.Remove(other)) return;

        playerInTrigger = playerCollidersInTrigger.Count > 0;
        if (playerInTrigger) return;

        cachedPlayer = null;
        if (promptObject != null && promptObject.activeSelf)
        {
            promptObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        playerCollidersInTrigger.Clear();
        cachedPlayer = null;
        playerInTrigger = false;
    }

    private void OnCollisionEnter(Collision collision) { }
    private void OnCollisionExit(Collision collision) { }
}

public class DoorPanelCollisionForwarder : MonoBehaviour
{
    public DoorController parentDoor;

    private void OnCollisionEnter(Collision collision) { }
    private void OnCollisionStay(Collision collision) { }

}