using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroCinematicManager : MonoBehaviour
{
    [System.Serializable]
    public struct Slide
    {
        [Tooltip("The storyboard image for this scene.")]
        public Sprite sprite;
        
        [TextArea(3, 10)]
        [Tooltip("The storytelling text displayed underneath the image.")]
        public string narrativeText;
    }

    [Header("Cinematic Slides")]
    public Slide[] slides;

    [Header("UI References")]
    public Image slideImage;
    public Text dialogueText;
    public CanvasGroup fadeGroup;
    public GameObject pressSpacePrompt;

    [Header("Settings")]
    public float typeSpeed = 0.04f;
    public float fadeDuration = 1.0f;
    public string nextSceneName = "map"; // Target game scene to load when finished

    private int currentSlideIndex = 0;
    private bool isTextTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;
    private bool isTransitioning = false;

    void Start()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogError("[IntroCinematicManager] No slides assigned!");
            return;
        }

        if (slideImage == null || dialogueText == null || fadeGroup == null || pressSpacePrompt == null)
        {
            Debug.LogError("[IntroCinematicManager] Please assign all UI references in Inspector!");
            return;
        }

        // Initialize UI
        fadeGroup.alpha = 0f;
        pressSpacePrompt.SetActive(false);
        StartCoroutine(PlayCinematic());
    }

    void Update()
    {
        if (isTransitioning) return;

        // Allow skipping text or moving to the next slide via Spacebar or Mouse Click
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTextTyping)
            {
                // Instant skip typing text, show full text immediately
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentFullText;
                isTextTyping = false;
                pressSpacePrompt.SetActive(true);
            }
            else
            {
                // Go to the next slide
                StartCoroutine(NextSlide());
            }
        }
    }

    IEnumerator PlayCinematic()
    {
        isTransitioning = true;
        currentSlideIndex = 0;
        
        // Setup first slide
        slideImage.sprite = slides[currentSlideIndex].sprite;
        dialogueText.text = "";
        
        // Fade in the screen
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;
        
        isTransitioning = false;
        
        // Start typing narrative text
        typingCoroutine = StartCoroutine(TypeText(slides[currentSlideIndex].narrativeText));
    }

    IEnumerator TypeText(string text)
    {
        isTextTyping = true;
        currentFullText = text;
        dialogueText.text = "";
        pressSpacePrompt.SetActive(false);

        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTextTyping = false;
        pressSpacePrompt.SetActive(true);
    }

    IEnumerator NextSlide()
    {
        isTransitioning = true;
        pressSpacePrompt.SetActive(false);

        currentSlideIndex++;

        // Fade out active slide
        float elapsed = 0f;
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / (fadeDuration / 2f));
            yield return null;
        }
        fadeGroup.alpha = 0f;

        if (currentSlideIndex >= slides.Length)
        {
            // End of cinematic, load the gameplay scene!
            Debug.Log("[IntroCinematicManager] Intro completed. Loading scene: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Setup next slide
            slideImage.sprite = slides[currentSlideIndex].sprite;
            dialogueText.text = "";

            // Fade in next slide
            elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / (fadeDuration / 2f));
                yield return null;
            }
            fadeGroup.alpha = 1f;

            isTransitioning = false;
            typingCoroutine = StartCoroutine(TypeText(slides[currentSlideIndex].narrativeText));
        }
    }
}
