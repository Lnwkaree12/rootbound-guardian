using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls Lumi the Light Spirit companion:
/// - Smoothly flies and follows the player with natural hovering bobbing.
/// - Wing flapping animation dynamically speeds up during movement.
/// - Displays cute and playful banter dialogue speech bubbles.
/// - Emits a soft warm magical aura.
/// </summary>
public class LumiCompanion : MonoBehaviour
{
    [Header("Target & Follow Settings")]
    [Tooltip("The target to follow (auto-finds Player if left empty)")]
    [SerializeField] private Transform target;
    [Tooltip("Offset relative to the player")]
    [SerializeField] private Vector3 followOffset = new Vector3(-0.75f, 1.35f, -0.4f);
    [Tooltip("Smooth follow damping time (smaller = tighter follow)")]
    [SerializeField] private float smoothTime = 0.28f;
    [Tooltip("Max flight speed when following")]
    [SerializeField] private float maxSpeed = 16f;
    [Tooltip("Distance threshold to teleport if player dashes or moves too far")]
    [SerializeField] private float maxDistanceBeforeTeleport = 25f;

    [Header("Hover Bobbing & Rotation")]
    [SerializeField] private float hoverFrequency = 2.2f;
    [SerializeField] private float hoverAmplitude = 0.12f;
    [SerializeField] private float rotationSpeed = 6.0f;
    [SerializeField] private float bankAmount = 15.0f;
    [Tooltip("Idle facing angle around Y axis (e.g. 155.4 to face camera nicely)")]
    [SerializeField] private float idleFacingAngleY = 155.4f;
    [SerializeField] private bool useFixedIdleAngle = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float idleAnimSpeed = 1.0f;
    [SerializeField] private float fastAnimSpeed = 1.8f;

    [Header("Dialogue Bubble")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private RectTransform bubbleTransform;
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float minDialogueInterval = 10f;
    [SerializeField] private float maxDialogueInterval = 20f;
    [SerializeField] private float bubbleDisplayDuration = 4.0f;

    [Header("Aura Light")]
    [SerializeField] private Light auraLight;
    [SerializeField] private float baseLightIntensity = 1.5f;
    [SerializeField] private float lightPulseAmplitude = 0.3f;
    [SerializeField] private float lightPulseFrequency = 2.0f;

    [Header("Playful Banter Lines")]
    [SerializeField] private List<string> banterLines = new List<string>()
    {
        "เดินระวังหน่อยสิเจ้ามนุษย์! เดี๋ยวก็สะดุดหินหรอก ฮิๆ~",
        "ว้าว ลาวาตรงนั้นสีสวยจัง... แต่อย่ากระโดดลงไปเล่นนะ!",
        "ปีกข้าสวยมั้ยล่ะ? บินทั้งวันยังไม่เหนื่อยเลยนะจะบอกให้~",
        "นี่ๆ เดินเร็วๆ หน่อย ข้าบินนำไปไกลแล้วนะ!",
        "แถวนี้มืดจัง ดีนะที่มีข้าคอยส่องไฟให้อ่ะ!",
        "ฮั่นแน่! แอบมองข้าอยู่ล่ะสิ มีข้าไปด้วยอุ่นใจใช่ม้า~",
        "สู้เขานะเจ้าสองขา ข้าเอาใจช่วยอยู่ข้างๆ นี่แหละ!",
        "เหนื่อยรึยัง? ข้าลอยตัวสบายจัง ไม่เมื่อยขาเลยสักนิด อิอิ",
        "ระวังหัวด้วยนะข้างหน้า ข้าตัวเล็กข้าหลบง่าย แต่เจ้าตัวโตนะ!",
        "ข้าเป็นวิญญาณแห่งแสงที่น่ารักที่สุดในป่าแล้ว ยอมรับมาซะดีๆ!"
    };

    private Vector3 currentVelocity;
    private float nextDialogueTime;
    private Coroutine dialogueCoroutine;
    private Camera mainCamera;
    private Vector3 lastTargetPos;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (dialogueCanvas != null)
            dialogueCanvas.worldCamera = Camera.main;

        if (bubbleTransform != null)
            bubbleTransform.localScale = Vector3.zero;

        if (bubbleCanvasGroup != null)
            bubbleCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        FindPlayerTarget();

        if (target != null)
        {
            transform.position = GetTargetFollowPosition();
            lastTargetPos = target.position;
        }

        transform.rotation = Quaternion.Euler(0f, idleFacingAngleY, 0f);
        ScheduleNextDialogue();
    }

    private void FindPlayerTarget()
    {
        if (target != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.Find("Player Variant");

        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null) return;
        }

        HandleMovement();
        HandleRotation();
        HandleAnimationSpeed();
        HandleDialogueTimer();
        HandleAuraPulse();
    }

    private void LateUpdate()
    {
        // Billboard dialogue bubble towards camera (distortion-free plane alignment)
        if (dialogueCanvas != null)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                dialogueCanvas.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private Vector3 GetTargetFollowPosition()
    {
        if (target == null) return transform.position;

        // Transform follow offset based on target's facing direction
        Vector3 worldOffset = target.TransformDirection(followOffset);
        Vector3 basePos = target.position + worldOffset;

        // Add subtle sine-wave hovering bob
        float hover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        basePos.y += hover;

        return basePos;
    }

    private void HandleMovement()
    {
        Vector3 targetPos = GetTargetFollowPosition();
        float distToTarget = Vector3.Distance(transform.position, targetPos);

        // Teleport if too far
        if (distToTarget > maxDistanceBeforeTeleport)
        {
            transform.position = targetPos;
            currentVelocity = Vector3.zero;
            return;
        }

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime, maxSpeed);
    }

    private void HandleRotation()
    {
        float speed = currentVelocity.magnitude;

        if (speed > 0.5f)
        {
            // Face the direction of movement
            Vector3 moveDir = currentVelocity.normalized;
            moveDir.y *= 0.3f; // Damp vertical tilt
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                
                // Add playful banking tilt based on turn rate
                Vector3 localVel = transform.InverseTransformDirection(currentVelocity);
                float bank = -Mathf.Clamp(localVel.x * 3f, -bankAmount, bankAmount);
                targetRot *= Quaternion.Euler(0, 0, bank);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
        else
        {
            // When idle, smoothly face the designated angle (e.g. 155.4 degrees)
            Quaternion targetRot;
            if (useFixedIdleAngle)
            {
                targetRot = Quaternion.Euler(0f, idleFacingAngleY, 0f);
            }
            else if (target != null)
            {
                targetRot = Quaternion.Euler(0f, target.eulerAngles.y + idleFacingAngleY, 0f);
            }
            else
            {
                targetRot = Quaternion.Euler(0f, idleFacingAngleY, 0f);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * (rotationSpeed * 0.5f));
        }
    }

    private void HandleAnimationSpeed()
    {
        if (animator == null) return;

        float currentSpeed = currentVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / 6f);
        animator.speed = Mathf.Lerp(idleAnimSpeed, fastAnimSpeed, speedRatio);
    }

    private void HandleAuraPulse()
    {
        if (auraLight == null) return;
        float pulse = Mathf.Sin(Time.time * lightPulseFrequency) * lightPulseAmplitude;
        auraLight.intensity = baseLightIntensity + pulse;
    }

    private void HandleDialogueTimer()
    {
        if (Time.time >= nextDialogueTime)
        {
            TriggerRandomDialogue();
            ScheduleNextDialogue();
        }
    }

    private void ScheduleNextDialogue()
    {
        nextDialogueTime = Time.time + Random.Range(minDialogueInterval, maxDialogueInterval);
    }

    /// <summary>
    /// Trigger a random playful dialogue bubble.
    /// </summary>
    public void TriggerRandomDialogue()
    {
        if (banterLines == null || banterLines.Count == 0) return;
        int randomIndex = Random.Range(0, banterLines.Count);
        Speak(banterLines[randomIndex]);
    }

    /// <summary>
    /// Speak a custom dialogue text.
    /// </summary>
    public void Speak(string message)
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(ShowBubbleRoutine(message));
    }

    private IEnumerator ShowBubbleRoutine(string message)
    {
        if (dialogueText != null)
            dialogueText.text = message;

        if (bubbleTransform == null) yield break;

        // Pop in animation (0 -> 1.15 -> 1.0)
        float popTime = 0.25f;
        float elapsed = 0f;

        while (elapsed < popTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popTime;
            // Overshoot bounce curve
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.12f;
            if (t > 0.8f)
            {
                scale = Mathf.Lerp(1.12f, 1.0f, (t - 0.8f) / 0.2f);
            }
            bubbleTransform.localScale = Vector3.one * scale;
            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = Mathf.Clamp01(t * 2f);

            yield return null;
        }

        bubbleTransform.localScale = Vector3.one;
        if (bubbleCanvasGroup != null)
            bubbleCanvasGroup.alpha = 1f;

        // Hold display duration
        yield return new WaitForSeconds(bubbleDisplayDuration);

        // Pop out animation (1.0 -> 0)
        float fadeTime = 0.2f;
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            bubbleTransform.localScale = Vector3.one * (1.0f - t);
            if (bubbleCanvasGroup != null)
                bubbleCanvasGroup.alpha = 1.0f - t;
            yield return null;
        }

        bubbleTransform.localScale = Vector3.zero;
        if (bubbleCanvasGroup != null)
            bubbleCanvasGroup.alpha = 0f;
        dialogueCoroutine = null;
    }
}
