using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Alignment Direction")]
    [Tooltip("ติ๊กถูกถ้าบันไดขนานหน้าฉาก (ล็อกแกน X), เอาออกถ้าเป็นบันไดด้านข้าง (ล็อกแกน Z)")]
    [SerializeField] private bool alignToX = true;

    [Header("Side/Direction Settings")]
    [Tooltip("เปิดใช้งานเพื่อให้บันไดปีนได้เฉพาะฝั่งที่หันหน้าเข้าหาเท่านั้น")]
    [SerializeField] private bool restrictToOneSide = true;

    [Header("Climb Prompt UI")]
    [SerializeField] private bool showClimbPrompt = true;
    [SerializeField] private float promptShowRadius = 3.2f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.35f, -0.35f);
    [SerializeField] private Sprite keycapSprite;
    [SerializeField] private TMP_FontAsset promptFont;
    [SerializeField] private Material overlayUiMaterial;

    private BoxCollider ladderCollider;
    private GameObject promptCanvasObj;
    private CanvasGroup promptCanvasGroup;
    private Vector3 promptBaseWorldPos;
    private TextMeshProUGUI climbPromptText;
    private Image climbPromptBackground;

    private Transform cachedPlayerTransform;
    private PlayerMovement cachedPlayerMovement;

    private void OnEnable()
    {
        InputSystemInput.OnLastInputDeviceChanged += OnInputDeviceChanged;
    }

    private void OnDisable()
    {
        InputSystemInput.OnLastInputDeviceChanged -= OnInputDeviceChanged;
    }

    private void Start()
    {
        ladderCollider = GetComponent<BoxCollider>();
        if (showClimbPrompt)
        {
            BuildClimbPromptUI();
        }
        RefreshClimbPrompt();
    }

    private void OnInputDeviceChanged(bool isController)
    {
        RefreshClimbPrompt();
    }

    private void RefreshClimbPrompt()
    {
        if (climbPromptText == null) return;

        bool isController = InputSystemInput.IsControllerInputActive;
        climbPromptText.text = isController
            ? "<sprite name=\"analogLUp\">"
            : "W";
        if (climbPromptBackground != null)
        {
            climbPromptBackground.enabled = !isController;
        }
    }

    private void Update()
    {
        if (!showClimbPrompt || promptCanvasObj == null || promptCanvasGroup == null) return;

        if (cachedPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                cachedPlayerTransform = playerObj.transform;
                cachedPlayerMovement = playerObj.GetComponent<PlayerMovement>();
            }
        }

        bool shouldShow = false;
        if (cachedPlayerTransform != null)
        {
            Bounds b = ladderCollider != null ? ladderCollider.bounds : new Bounds(transform.position, Vector3.one);
            Vector2 playerXZ = new Vector2(cachedPlayerTransform.position.x, cachedPlayerTransform.position.z);
            Vector2 ladderXZ = new Vector2(b.center.x, b.center.z);
            float distXZ = Vector2.Distance(playerXZ, ladderXZ);

            bool withinY = cachedPlayerTransform.position.y >= b.min.y - 1.5f
                        && cachedPlayerTransform.position.y <= b.max.y + 0.5f;
            bool notClimbing = cachedPlayerMovement == null || !cachedPlayerMovement.IsClimbing;

            shouldShow = distXZ <= promptShowRadius && withinY && notClimbing;
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * 8f);

        float bob = Mathf.Sin(Time.time * 3.5f) * 0.06f;
        promptCanvasObj.transform.position = promptBaseWorldPos + new Vector3(0f, bob, 0f);

        if (Camera.main != null)
        {
            promptCanvasObj.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void BuildClimbPromptUI()
    {
        Bounds b = ladderCollider != null ? ladderCollider.bounds : new Bounds(transform.position, new Vector3(1f, 2f, 1f));
        promptBaseWorldPos = new Vector3(b.center.x, b.min.y, b.center.z) + promptOffset;

        promptCanvasObj = new GameObject("LadderClimbPrompt_W");
        promptCanvasObj.transform.SetParent(transform, true);
        promptCanvasObj.transform.position = promptBaseWorldPos;
        promptCanvasObj.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = promptCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 60;

        RectTransform canvasRect = promptCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(140f, 140f);

        promptCanvasGroup = promptCanvasObj.AddComponent<CanvasGroup>();
        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;

        GameObject bgObj = new GameObject("Keycap_W_Bg");
        bgObj.transform.SetParent(promptCanvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(130f, 130f);

        Image bgImg = bgObj.AddComponent<Image>();
        climbPromptBackground = bgImg;
        if (keycapSprite != null)
        {
            bgImg.sprite = keycapSprite;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.86f, 0.72f, 0.50f, 0.96f);
        }
        bgImg.preserveAspect = true;
        bgImg.raycastTarget = false;
        if (overlayUiMaterial != null)
        {
            bgImg.material = overlayUiMaterial;
        }
        else
        {
            Shader overlayShader = Shader.Find("UI/OverlayAlwaysOnTop");
            if (overlayShader != null) bgImg.material = new Material(overlayShader);
        }

        GameObject txtObj = new GameObject("Key_Label_W");
        txtObj.transform.SetParent(bgObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        if (promptFont != null) tmp.font = promptFont;
        tmp.text = "W";
        tmp.fontSize = 44f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.227f, 0.133f, 0.071f, 1f);
        tmp.raycastTarget = false;
        climbPromptText = tmp;

        Shader tmpOverlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (tmpOverlayShader != null && tmp.fontSharedMaterial != null)
        {
            Material mat = new Material(tmp.fontSharedMaterial);
            mat.shader = tmpOverlayShader;
            tmp.fontMaterial = mat;
        }
        RefreshClimbPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                if (restrictToOneSide)
                {
                    Vector3 dirToPlayer = (other.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, dirToPlayer);

                    Debug.Log("Ladder Dot Value: " + dot);

                    if (dot < 0f) return;
                }

                player.EnableClimbing(true, transform.position, alignToX);
            }
        }
    }
}
