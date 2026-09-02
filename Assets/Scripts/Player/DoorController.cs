using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DoorController : MonoBehaviour
{
    [Header("Door Panel Reference")]
    public Transform doorPanel;

    [Header("Door Settings")]
    public float openAngle = -90f;
    public float openSpeed = 3.5f;
    public float interactRadius = 3.0f;
    public bool autoUnlockOnContact = true;

    [Header("Key Requirement Settings")]
    [Tooltip("ระบุชื่อของกุญแจที่ต้องใช้ (ถ้าปล่อยว่างไว้ ขอแค่เป็น ItemType.Key อันไหนก็ได้จะใช้เปิดได้เลย)")]
    public string requiredKeyName = "";
    [Tooltip("ต้องการให้ลบกุญแจออกจาก Inventory เมื่อใช้งานสำเร็จหรือไม่")]
    public bool consumeKeyOnUse = true;

    [Header("Audio")]
    public AudioClip doorOpenSfx;
    private AudioSource audioSource;

    [Header("Floating Prompt UI")]
    public GameObject promptObject;
    public Text promptText;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool playerInRange = false;
    private bool playerInTrigger = false;
    private PlayerMovement cachedPlayer;

    void Start()
    {
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }

        cachedPlayer = FindObjectOfType<PlayerMovement>();
        SetupFloatingPrompt();
    }

    private void SetupFloatingPrompt()
    {
        if (promptObject != null) return;

        GameObject promptCanvasGO = new GameObject("DoorPromptCanvas");
        promptCanvasGO.transform.SetParent(transform, false);
        promptCanvasGO.transform.localPosition = new Vector3(0f, 2.35f, 0f);

        Canvas canvas = promptCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = promptCanvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 60f);
        promptCanvasGO.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(promptCanvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.08f, 0.06f, 0.88f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(bgGO.transform, false);
        promptText = textGO.AddComponent<Text>();

        Font customFont = Resources.Load<Font>("Itim-Regular");
        if (customFont == null)
        {
#if UNITY_EDITOR
            customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
#endif
        }
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }

        promptText.font = customFont;
        promptText.fontSize = 22;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(1f, 0.85f, 0.3f);
        promptText.text = "🔒 ประตูล็อคอยู่";
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        promptObject = promptCanvasGO;
        promptObject.SetActive(false);
    }

    void Update()
    {
        if (doorPanel != null)
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            doorPanel.localRotation = Quaternion.Slerp(doorPanel.localRotation, targetRot, Time.deltaTime * openSpeed);
        }

        if (isOpen)
        {
            if (promptObject != null && promptObject.activeSelf)
            {
                promptObject.SetActive(false);
            }
            return;
        }

        CheckProximityToPlayer();

        if (playerInRange)
        {
            bool hasKey = CheckPlayerHasKey();
            if (promptObject != null)
            {
                if (!promptObject.activeSelf) promptObject.SetActive(true);

                if (promptText != null)
                {
                    if (hasKey)
                    {
                        promptText.text = "🗝️ กด [F] เพื่อไขประตู (มีกุญแจแล้ว)";
                        promptText.color = new Color(0.4f, 1f, 0.4f);
                    }
                    else
                    {
                        promptText.text = "🔒 ประตูล็อคอยู่! (ต้องหากุญแจก่อน)";
                        promptText.color = new Color(1f, 0.6f, 0.4f);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
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

    private void CheckProximityToPlayer()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<PlayerMovement>();
            if (cachedPlayer == null) return;
        }

        float dist = Vector3.Distance(transform.position, cachedPlayer.transform.position);
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 1f);
        float effectiveRadius = Mathf.Max(interactRadius * maxScale, 6.5f);

        if (dist <= effectiveRadius)
        {
            playerInRange = true;
            float touchRadius = Mathf.Max(2.5f * maxScale, 4.0f);
            if (autoUnlockOnContact && dist <= touchRadius && CheckPlayerHasKey() && !isOpen)
            {
                TryOpenDoor();
            }
        }
        else if (!playerInTrigger)
        {
            playerInRange = false;
        }
    }

    void LateUpdate()
    {
        if (promptObject != null && promptObject.activeSelf)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                promptObject.transform.rotation = cam.transform.rotation;
            }
        }
    }

    // ========================================================================
    // 🔑 การเชื่อมต่อกับคลาส Inventory
    // ========================================================================

    private Inventory GetPlayerInventory()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<PlayerMovement>();
        }

        if (cachedPlayer != null)
        {
            return cachedPlayer.GetComponent<Inventory>();
        }

        return FindObjectOfType<Inventory>();
    }

    private bool CheckPlayerHasKey()
    {
        Inventory inventory = GetPlayerInventory();
        if (inventory == null) return false;

        foreach (ItemData item in inventory.Items)
        {
            if (item == null) continue;

            // เช็กว่าเป็น ItemType.Key
            if (item.itemType == ItemType.Key)
            {
                // ถ้าไม่ได้ระบุชื่อกุญแจเฉพาะ ถือว่าใช้กุญแจอะไรก็เปิดได้
                if (string.IsNullOrEmpty(requiredKeyName))
                {
                    return true;
                }
                // ถ้ามีระบุชื่อกุญแจเฉพาะ ต้องชื่อตรงกัน
                else if (item.itemName == requiredKeyName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ConsumeKeyFromInventory()
    {
        if (!consumeKeyOnUse) return;

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

    // ========================================================================

    public void TryOpenDoor()
    {
        if (isOpen) return;

        if (CheckPlayerHasKey())
        {
            isOpen = true;
            Debug.Log("[DoorController] Key found in Inventory! Opening door...");

            if (promptObject != null) promptObject.SetActive(false);

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                c.isTrigger = true;
            }

            // ลบกุญแจออกจากกระเป๋าเมื่อเปิดประตู
            ConsumeKeyFromInventory();

            PlayDoorOpenAudio();
            TriggerWinUI();
        }
        else
        {
            Debug.Log("[DoorController] No matching key found in Inventory.");
            StartCoroutine(FlashLockedPrompt());
        }
    }

    private void TriggerWinUI()
    {
        WinUIManager wum = WinUIManager.Instance;
        if (wum == null) wum = FindObjectOfType<WinUIManager>();

        if (wum == null)
        {
            GameObject wumGO = new GameObject("WinUIManager_Auto");
            wum = wumGO.AddComponent<WinUIManager>();
        }

        wum.TriggerWin(0.65f);
    }

    private IEnumerator FlashLockedPrompt()
    {
        if (promptObject == null) yield break;
        promptObject.SetActive(true);

        if (promptText != null)
        {
            promptText.text = "⚠️ ประตูล็อคแน่นหนา! ต้องใช้กุญแจ 🔑";
            promptText.color = Color.red;
        }

        yield return new WaitForSeconds(1.5f);

        if (!isOpen && playerInRange)
        {
            if (promptText != null)
            {
                promptText.text = "🔒 ประตูล็อคอยู่! (ต้องหากุญแจก่อน)";
                promptText.color = new Color(1f, 0.6f, 0.4f);
            }
        }
    }

    private void PlayDoorOpenAudio()
    {
        if (audioSource == null) return;

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
        float duration = 0.8f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float click = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-50f * t);
            float groan = Mathf.Sin(2f * Mathf.PI * (110f + t * 40f) * t) * Mathf.Exp(-2.5f * t) * 0.5f;
            samples[i] = (click * 0.4f + groan * 0.35f);
        }

        AudioClip clip = AudioClip.Create("ProceduralDoorUnlock", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponent<PlayerMovement>() != null) return true;
        if (other.GetComponentInParent<PlayerMovement>() != null) return true;
        if (other.GetComponentInChildren<PlayerMovement>() != null) return true;

        string objName = other.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("capsule") || objName.Contains("finalmc")) return true;
        if (other.transform.root != null && other.transform.root.name.ToLower().Contains("player")) return true;

        return false;
    }

    public void HandlePlayerContact(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInTrigger = true;
        playerInRange = true;

        if (autoUnlockOnContact && CheckPlayerHasKey() && !isOpen)
        {
            TryOpenDoor();
        }
    }

    private void OnTriggerEnter(Collider other) => HandlePlayerContact(other);
    private void OnTriggerStay(Collider other) => HandlePlayerContact(other);
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other)) playerInTrigger = false;
    }

    private void OnCollisionEnter(Collision collision) => HandlePlayerContact(collision.collider);
    private void OnCollisionStay(Collision collision) => HandlePlayerContact(collision.collider);
    private void OnCollisionExit(Collision collision)
    {
        if (IsPlayer(collision.collider)) playerInTrigger = false;
    }
}

public class DoorPanelCollisionForwarder : MonoBehaviour
{
    public DoorController parentDoor;

    private void OnCollisionEnter(Collision collision)
    {
        if (parentDoor != null) parentDoor.HandlePlayerContact(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (parentDoor != null) parentDoor.HandlePlayerContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (parentDoor != null) parentDoor.HandlePlayerContact(other);
    }
}