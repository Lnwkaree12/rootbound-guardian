using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Provides juicy hover, selection, and press scaling with living cat paw / leaf animations for UI buttons.
/// Supports both Mouse Hover and Keyboard/Gamepad Navigation (Select / Submit).
/// Automatically expands raycast hitbox padding to prevent missed clicks.
/// Uses Time.unscaledDeltaTime so buttons remain responsive even when Time.timeScale = 0 (game paused).
/// </summary>
[RequireComponent(typeof(Button))]
public class UIJuicyButton : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Paw / Leaf Indicators")]
    public GameObject leftPaw;
    public GameObject rightPaw;

    [Header("Icon Settings")]
    public bool alwaysShowIcons = true;

    [Header("Scale Settings")]
    public float hoverScale = 1.08f;
    public float pressScale = 0.96f;
    public float transitionSpeed = 16f;

    [Header("Hitbox Padding")]
    [Tooltip("Extra raycast hitbox margin (Left, Bottom, Right, Top) in pixels to ensure clicks never miss.")]
    public Vector4 extraHitboxPadding = new Vector4(-18f, -14f, -18f, -14f);

    public System.Action<UIJuicyButton> onHoverOrSelect;

    private Button button;
    private Image buttonImage;
    private Vector3 baseScale = Vector3.one;
    private float targetScale = 1.0f;
    private float currentScale = 1.0f;
    private bool isHovered = false;
    private bool isSelected = false;

    private Vector3 leftPawBasePos;
    private Vector3 rightPawBasePos;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        baseScale = transform.localScale;

        if (leftPaw != null) leftPawBasePos = leftPaw.transform.localPosition;
        if (rightPaw != null) rightPawBasePos = rightPaw.transform.localPosition;

        ApplyHitboxPadding();
    }

    private void ApplyHitboxPadding()
    {
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
            buttonImage.raycastPadding = extraHitboxPadding;
        }

        // Child elements should not block raycast
        Graphic[] childGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in childGraphics)
        {
            if (g != buttonImage && g.gameObject != gameObject)
            {
                g.raycastTarget = false;
            }
        }
    }

    private void OnEnable()
    {
        targetScale = 1.0f;
        currentScale = 1.0f;
        transform.localScale = baseScale;
        isHovered = false;
        isSelected = false;
        SetPaws(alwaysShowIcons);
        ApplyHitboxPadding();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = hoverScale;
        SetPaws(true);
        onHoverOrSelect?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
        {
            targetScale = 1.0f;
            if (!alwaysShowIcons) SetPaws(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bool highlighted = isHovered || isSelected;
        targetScale = highlighted ? hoverScale : 1.0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Punch scale on click
        targetScale = hoverScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        targetScale = hoverScale;
        SetPaws(true);
        onHoverOrSelect?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        if (!isHovered)
        {
            targetScale = 1.0f;
            if (!alwaysShowIcons) SetPaws(false);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        targetScale = pressScale;
    }

    private void Update()
    {
        // Keep in sync with EventSystem selection
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            isSelected = true;
        }
        else if (!isHovered)
        {
            isSelected = false;
        }

        bool isHighlighted = isHovered || isSelected;
        float desiredScale = isHighlighted ? hoverScale : 1.0f;

        // Animate scale smoothly with unscaled time
        float dt = Time.unscaledDeltaTime;
        currentScale = Mathf.Lerp(currentScale, targetScale, dt * transitionSpeed);
        transform.localScale = baseScale * currentScale;

        // If targetScale wasn't actively held in press, restore desired scale
        if (targetScale == pressScale && !Input.GetMouseButton(0))
        {
            targetScale = desiredScale;
        }

        if (isHighlighted)
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
        else if (alwaysShowIcons)
        {
            if (leftPaw != null)
            {
                leftPaw.transform.localPosition = leftPawBasePos;
                leftPaw.transform.localEulerAngles = Vector3.zero;
            }
            if (rightPaw != null)
            {
                rightPaw.transform.localPosition = rightPawBasePos;
                rightPaw.transform.localEulerAngles = Vector3.zero;
            }
        }
    }

    public void SetPaws(bool active)
    {
        if (leftPaw != null) leftPaw.SetActive(active);
        if (rightPaw != null) rightPaw.SetActive(active);
    }

    public void TriggerVisualSelection(bool selected)
    {
        isSelected = selected;
        targetScale = selected ? hoverScale : 1.0f;
        SetPaws(selected || alwaysShowIcons);
    }
}

