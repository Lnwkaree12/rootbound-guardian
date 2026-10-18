using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Provides juicy hover and press scaling with living cat paw animations for UI buttons.
/// Uses Time.unscaledDeltaTime so buttons remain responsive even when Time.timeScale = 0 (game paused).
/// </summary>
[RequireComponent(typeof(Button))]
public class UIJuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Paw Indicators")]
    public GameObject leftPaw;
    public GameObject rightPaw;

    [Header("Icon Settings")]
    public bool alwaysShowIcons = true;

    [Header("Scale Settings")]
    public float hoverScale = 1.05f;
    public float pressScale = 0.95f;
    public float transitionSpeed = 16f;

    private Vector3 baseScale = Vector3.one;
    private float targetScale = 1.0f;
    private float currentScale = 1.0f;
    private bool isHovered = false;

    private Vector3 leftPawBasePos;
    private Vector3 rightPawBasePos;

    private void Awake()
    {
        baseScale = transform.localScale;

        if (leftPaw != null) leftPawBasePos = leftPaw.transform.localPosition;
        if (rightPaw != null) rightPawBasePos = rightPaw.transform.localPosition;
    }

    private void OnEnable()
    {
        targetScale = 1.0f;
        currentScale = 1.0f;
        transform.localScale = baseScale;
        isHovered = false;
        SetPaws(alwaysShowIcons);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = hoverScale;
        SetPaws(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1.0f;
        if (!alwaysShowIcons) SetPaws(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? hoverScale : 1.0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        currentScale = Mathf.Lerp(currentScale, targetScale, dt * transitionSpeed);
        transform.localScale = baseScale * currentScale;

        if (isHovered)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 5.0f) * 3f;
            float tilt = Mathf.Sin(Time.unscaledTime * 4.0f) * 6f;

            if (leftPaw != null)
            {
                leftPaw.transform.localPosition = leftPawBasePos + new Vector3(0f, bob, 0f);
                leftPaw.transform.localEulerAngles = new Vector3(0f, 0f, tilt);
            }
            if (rightPaw != null)
            {
                rightPaw.transform.localPosition = rightPawBasePos + new Vector3(0f, bob, 0f);
                rightPaw.transform.localEulerAngles = new Vector3(0f, 0f, -tilt);
            }
        }
    }

    public void SetPaws(bool active)
    {
        if (leftPaw != null) leftPaw.SetActive(active);
        if (rightPaw != null) rightPaw.SetActive(active);
    }
}
