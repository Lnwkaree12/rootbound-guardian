using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SlimeV2NPC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpCooldownMin = 2f;
    [SerializeField] private float jumpCooldownMax = 5f;

    [Header("Dialogue Settings")]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "Boing!",
        "Hello Sprout Scout!",
        "Watch out for the dangerous roots!",
        "I'm just a friendly slime.",
        "Have you found the key yet?",
        "Don't stomp on me, please!",
        "The World Tree needs your help..."
    };
    [SerializeField] private float dialogueDuration = 3f;
    [SerializeField] private float dialogueInterval = 8f;

    [Header("Visuals & Squash/Stretch")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform spriteTransform; // Child object holding the SpriteRenderer for clean scaling

    private Rigidbody rb;
    private bool isGrounded = true;
    private bool wasGrounded = true;
    private float cooldownTimer;
    private float dialogueTimer;

    // Original local scale of the sprite container
    private Vector3 originalScale = Vector3.one;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Auto-assign references if not set
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteTransform == null && spriteRenderer != null) spriteTransform = spriteRenderer.transform;
        
        if (spriteTransform != null)
        {
            originalScale = spriteTransform.localScale;
        }

        // Set up dialog Canvas if available
        if (dialogueCanvas == null) dialogueCanvas = GetComponentInChildren<Canvas>();
        if (dialogueText == null && dialogueCanvas != null) dialogueText = dialogueCanvas.GetComponentInChildren<TextMeshProUGUI>();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.renderMode = RenderMode.WorldSpace;
            dialogueCanvas.gameObject.SetActive(false);
        }

        // Setup timers
        cooldownTimer = Random.Range(jumpCooldownMin, jumpCooldownMax);
        dialogueTimer = Random.Range(3f, dialogueInterval);
    }

    void Update()
    {
        // 1. Dialogue billboard: Make the canvas always face the camera
        if (dialogueCanvas != null && dialogueCanvas.gameObject.activeSelf && Camera.main != null)
        {
            dialogueCanvas.transform.LookAt(dialogueCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }

        // 2. Dialogue Timer
        dialogueTimer -= Time.deltaTime;
        if (dialogueTimer <= 0f)
        {
            StartCoroutine(ShowDialogue());
            dialogueTimer = dialogueInterval + dialogueDuration;
        }

        // 3. Jump Cooldown Timer
        if (isGrounded)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                Jump();
                cooldownTimer = Random.Range(jumpCooldownMin, jumpCooldownMax);
            }
        }

        // 4. Procedural Squash & Stretch Animation
        HandleProceduralAnimation();
    }

    void FixedUpdate()
    {
        // Simple ground check based on vertical velocity of Rigidbody
        // Also check with a short raycast down for safety
        bool velocityGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;
        bool raycastGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);
        
        isGrounded = velocityGrounded || raycastGrounded;

        // Detect landing to trigger squash
        if (isGrounded && !wasGrounded)
        {
            StartCoroutine(SquashRoutine());
        }

        wasGrounded = isGrounded;
    }

    private void Jump()
    {
        // Choose a random angle in the X-Z plane
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;

        // Apply physics forces
        rb.linearVelocity = new Vector3(direction.x * moveSpeed, jumpForce, direction.z * moveSpeed);
        isGrounded = false;

        // Flip sprite based on move direction
        if (spriteRenderer != null)
        {
            if (direction.x < 0f)
            {
                spriteRenderer.flipX = true;
            }
            else if (direction.x > 0f)
            {
                spriteRenderer.flipX = false;
            }
        }

        // Trigger stretch animation
        StartCoroutine(StretchRoutine());
    }

    private void HandleProceduralAnimation()
    {
        // If not in a routine, return scale towards original
        if (spriteTransform != null && !isSquashing && !isStretching)
        {
            spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, originalScale, Time.deltaTime * 5f);
        }
    }

    private bool isSquashing = false;
    private bool isStretching = false;

    IEnumerator StretchRoutine()
    {
        isStretching = true;
        float elapsed = 0f;
        float duration = 0.2f;

        Vector3 targetScale = new Vector3(originalScale.x * 0.75f, originalScale.y * 1.35f, originalScale.z * 0.75f);

        while (elapsed < duration && !isGrounded)
        {
            elapsed += Time.deltaTime;
            if (spriteTransform != null)
            {
                spriteTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            }
            yield return null;
        }

        // Smoothly interpolate back to normal while falling
        elapsed = 0f;
        duration = 0.3f;
        Vector3 currentScale = spriteTransform != null ? spriteTransform.localScale : originalScale;

        while (elapsed < duration && !isGrounded)
        {
            elapsed += Time.deltaTime;
            if (spriteTransform != null)
            {
                spriteTransform.localScale = Vector3.Lerp(currentScale, originalScale, elapsed / duration);
            }
            yield return null;
        }

        isStretching = false;
    }

    IEnumerator SquashRoutine()
    {
        // Cancel stretching if active
        isStretching = false;
        isSquashing = true;
        float elapsed = 0f;
        float duration = 0.15f;

        Vector3 targetScale = new Vector3(originalScale.x * 1.35f, originalScale.y * 0.65f, originalScale.z * 1.35f);
        Vector3 startScale = spriteTransform != null ? spriteTransform.localScale : originalScale;

        // Squash down on impact
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteTransform != null)
            {
                spriteTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            }
            yield return null;
        }

        // Return back to normal
        elapsed = 0f;
        duration = 0.25f;
        startScale = spriteTransform != null ? spriteTransform.localScale : targetScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteTransform != null)
            {
                spriteTransform.localScale = Vector3.Lerp(startScale, originalScale, elapsed / duration);
            }
            yield return null;
        }

        isSquashing = false;
    }

    IEnumerator ShowDialogue()
    {
        if (dialogueCanvas == null || dialogueText == null || dialogueLines.Length == 0)
            yield break;

        // Pick random line
        int index = Random.Range(0, dialogueLines.Length);
        dialogueText.text = dialogueLines[index];
        dialogueCanvas.gameObject.SetActive(true);

        // Simple fade-in effect on TMPro color alpha
        Color originalColor = dialogueText.color;
        float elapsed = 0f;
        float fadeTime = 0.3f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            dialogueText.color = new Color(originalColor.r, originalColor.g, originalColor.b, elapsed / fadeTime);
            yield return null;
        }
        dialogueText.color = originalColor;

        // Display for the duration
        yield return new WaitForSeconds(dialogueDuration);

        // Fade-out effect
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            dialogueText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - (elapsed / fadeTime));
            yield return null;
        }

        dialogueCanvas.gameObject.SetActive(false);
        dialogueText.color = originalColor;
    }
}
