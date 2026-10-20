using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class TreeInteractUI : MonoBehaviour
{
    [Header("Visual Elements")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image keyIcon;
    [SerializeField] private Image sproutIcon;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float floatSpeed = 3.2f;
    [SerializeField] private float floatHeight = 0.12f;
    [SerializeField] private float pulseSpeed = 3.6f;
    [SerializeField] private float pulseScale = 0.05f;
    [SerializeField] private float wobbleAngle = 0f; // 0 = perfectly straight & level (no tilt)

    private Camera targetCamera;
    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private Vector3 keyInitialLocalPos;
    private Vector3 sproutInitialLocalPos;
    private Coroutine popCoroutine;
    private bool isVisible = false;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;

        if (keyIcon != null) keyInitialLocalPos = keyIcon.transform.localPosition;
        if (sproutIcon != null) sproutInitialLocalPos = sproutIcon.transform.localPosition;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Start hidden
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        targetCamera = GetBestCamera();
    }

    public void ConfigureElements(Image bg, Image key, Image sprout, TextMeshProUGUI text)
    {
        bgImage = bg;
        keyIcon = key;
        sproutIcon = sprout;
        promptText = text;
        if (keyIcon != null) keyInitialLocalPos = keyIcon.transform.localPosition;
        if (sproutIcon != null) sproutInitialLocalPos = sproutIcon.transform.localPosition;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = GetBestCamera();
            if (targetCamera == null) return;
        }

        // 1. Billboard facing active camera perfectly level with the screen (zero tilt)
        Quaternion camRot = targetCamera.transform.rotation;
        if (wobbleAngle > 0.001f)
        {
            float tilt = Mathf.Sin(Time.time * (floatSpeed * 0.8f)) * wobbleAngle;
            transform.rotation = camRot * Quaternion.Euler(0f, 0f, tilt);
        }
        else
        {
            transform.rotation = camRot;
        }

        // 2. Gentle cute floating hover animation
        if (isVisible)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.localPosition = initialLocalPos + new Vector3(0f, yOffset, 0f);

            // 3. Cute breathing pulse (Squash & stretch)
            float breath = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = new Vector3(initialScale.x * (1.0f + breath), initialScale.y * (1.0f - breath * 0.7f), initialScale.z);

            // 4. Kawaii Key Hop ("Press me!" playful hop)
            if (keyIcon != null)
            {
                float hopCycle = (Time.time * 2.2f) % 2.0f;
                if (hopCycle < 0.6f)
                {
                    float hopT = hopCycle / 0.6f;
                    float hopY = Mathf.Sin(hopT * Mathf.PI) * 10f;
                    keyIcon.transform.localPosition = keyInitialLocalPos + new Vector3(0f, hopY, 0f);
                    float squash = Mathf.Sin(hopT * Mathf.PI) * 0.12f;
                    keyIcon.transform.localScale = new Vector3(1f - squash, 1f + squash * 1.2f, 1f);
                }
                else
                {
                    keyIcon.transform.localPosition = keyInitialLocalPos;
                    keyIcon.transform.localScale = Vector3.one;
                }
            }

            // 5. Sprout gentle waggle
            if (sproutIcon != null)
            {
                float sproutTilt = Mathf.Sin(Time.time * 5.0f) * 6.0f;
                sproutIcon.transform.localRotation = Quaternion.Euler(0f, 0f, sproutTilt);
            }
        }
    }

    private Camera GetBestCamera()
    {
        // 1. Check Player's attached camera first (gameplay camera)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("Player Variant");
        if (player != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>(false);
            if (playerCam != null && playerCam.isActiveAndEnabled)
            {
                return playerCam;
            }
        }

        // 2. Check Camera.main
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.isActiveAndEnabled)
        {
            return mainCam;
        }

        // 3. Fallback to highest depth camera
        Camera[] cams = Camera.allCameras;
        Camera bestCam = null;
        float maxDepth = float.MinValue;
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null && cams[i].isActiveAndEnabled && cams[i].depth >= maxDepth)
            {
                maxDepth = cams[i].depth;
                bestCam = cams[i];
            }
        }
        return bestCam;
    }

    public void Show()
    {
        isVisible = true;
        gameObject.SetActive(true);

        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop(true));
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;

        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop(false));
    }

    private IEnumerator AnimatePop(bool show)
    {
        float duration = 0.32f;
        float elapsed = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 endScale = show ? initialScale : Vector3.zero;

        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Bouncy marshmallow elastic overshoot
            float curve = show ? (Mathf.Sin(t * Mathf.PI * 0.5f) + Mathf.Sin(t * Mathf.PI * 2.0f) * 0.18f * (1f - t)) : (1f - t);

            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, initialScale, curve);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        transform.localScale = endScale;
        canvasGroup.alpha = endAlpha;

        if (!show)
        {
            gameObject.SetActive(false);
        }
    }
}
