using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("UI Components (TextMeshPro)")]
    [Tooltip("TextMeshPro text displaying progress percentage (e.g., '100%').")]
    public TextMeshProUGUI progressText;
    
    [Tooltip("TextMeshPro text displaying loading steps (e.g., 'Loading assets...').")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("TextMeshPro text displaying rotating tips or lore.")]
    public TextMeshProUGUI tipText;

    [Header("UI Components (Legacy Text - Alternates)")]
    [Tooltip("Legacy UI Text displaying progress percentage. Use this if you are not using TextMeshPro.")]
    public Text progressTextLegacy;
    
    [Tooltip("Legacy UI Text displaying loading steps. Use this if you are not using TextMeshPro.")]
    public Text statusTextLegacy;
    
    [Tooltip("Legacy UI Text displaying rotating tips. Use this if you are not using TextMeshPro.")]
    public Text tipTextLegacy;

    [Header("General UI Components")]
    [Tooltip("The slider representing the progress bar.")]
    public Slider progressBar;
    
    [Tooltip("A CanvasGroup covering the screen to handle smooth fade in/out transition.")]
    public CanvasGroup fadeCanvasGroup;
    
    [Tooltip("Prompt GameObject shown when loading is done (e.g., 'Press Any Key to Start').")]
    public GameObject pressKeyPrompt;

    [Header("Loading Configuration")]
    [Tooltip("The name of the scene to load.")]
    public string sceneToLoad = "SafeZone";
    
    [Tooltip("Minimum time the loading screen must be visible (for smooth visualization).")]
    public float minimumLoadTime = 4f;
    
    [Tooltip("If true, the game will wait for player input after loading is complete.")]
    public bool waitForInputToStart = true;
    
    [Header("Gameplay Tips")]
    [Tooltip("Gameplay tips or lore text that will rotate during loading.")]
    [TextArea(2, 5)]
    public string[] gameTips = new string[]
    {
        "Tip: Sprout Scouts must avoid dangerous roots!",
        "Tip: Collect water to stay hydrated and keep moving.",
        "Tip: Look out for glowing runes to gain temporary speed boosts.",
        "Tip: Sprout Scout backpacks can carry up to 5 resource items."
    };
    
    [Tooltip("How long each tip is displayed before rotating to the next.")]
    public float tipRotationInterval = 4f;

    // Internal animation state
    private float targetProgress = 0f;
    private float currentProgress = 0f;
    
    void Start()
    {
        // Initial setup
        if (pressKeyPrompt != null) 
            pressKeyPrompt.SetActive(false);
            
        if (fadeCanvasGroup != null) 
            fadeCanvasGroup.alpha = 1f; // Start screen fully black
        
        StartCoroutine(LoadingSequence());
    }

    IEnumerator LoadingSequence()
    {
        // 1. Smooth Fade In: Transition from black overlay to showing the loading screen
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = 1f - (elapsed / 1f);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }

        // Start rotating gameplay tips in background
        Coroutine tipsCoroutine = StartCoroutine(RotateTips());

        // 2. Start Async Loading
        float startTime = Time.time;
        AsyncOperation operation = null;
        
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LoadingManager] Direct scene load threw an exception: {e.Message}");
        }

        if (operation == null)
        {
            Debug.LogError($"[LoadingManager] Scene '{sceneToLoad}' could not be loaded async. " +
                           "Please make sure the scene name is correct and it is added to 'Build Settings' (File -> Build Settings).\n" +
                           "Simulating loading screen for testing purposes...");
        }

        while (currentProgress < 1f)
        {
            float elapsed = Time.time - startTime;
            
            // Calculate progress based on actual Unity loading (0 to 0.9) and minimum time
            float actualProgress = 1f;
            if (operation != null)
            {
                actualProgress = Mathf.Clamp01(operation.progress / 0.9f);
            }
            
            float timeProgress = Mathf.Clamp01(elapsed / minimumLoadTime);
            
            targetProgress = Mathf.Min(actualProgress, timeProgress);

            // Smoothly move the progress bar towards the target value to avoid jumps
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 0.5f);

            if (progressBar != null)
                progressBar.value = currentProgress;

            // Update Progress Text (TextMeshPro)
            if (progressText != null)
                progressText.text = $"{(currentProgress * 100f):0}%";
                
            // Update Progress Text (Legacy UI Text)
            if (progressTextLegacy != null)
                progressTextLegacy.text = $"{(currentProgress * 100f):0}%";

            // Update status messages dynamically
            string statusStr = "Loading...";
            if (currentProgress < 0.3f) statusStr = "Loading game assets...";
            else if (currentProgress < 0.6f) statusStr = "Preparing environments...";
            else if (currentProgress < 0.9f) statusStr = "Initializing world...";
            else statusStr = "Almost ready...";

            if (statusText != null) statusText.text = statusStr;
            if (statusTextLegacy != null) statusTextLegacy.text = statusStr;

            yield return null;
        }

        if (operation != null)
        {
            // Ensure background loading is fully finished
            while (operation.progress < 0.9f)
            {
                yield return null;
            }
        }

        // Stop tips rotation when done
        StopCoroutine(tipsCoroutine);

        // 3. Wait for User Input
        if (waitForInputToStart)
        {
            if (pressKeyPrompt != null) 
                pressKeyPrompt.SetActive(true);
                
            string readyStr = "Load complete. Click or press any key to enter!";
            if (statusText != null) statusText.text = readyStr;
            if (statusTextLegacy != null) statusTextLegacy.text = readyStr;
            
            while (!Input.anyKeyDown)
            {
                yield return null;
            }
        }

        // 4. Smooth Fade Out: Transition back to black overlay before entering the game
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = elapsed / 1f;
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // Activate the scene
        if (operation != null)
        {
            operation.allowSceneActivation = true;
        }
        else
        {
            Debug.LogWarning($"[LoadingManager] Simulated load finished! Unable to switch scene because '{sceneToLoad}' is missing or not in Build Settings.");
        }
    }

    IEnumerator RotateTips()
    {
        if (gameTips == null || gameTips.Length == 0) 
            yield break;

        int index = Random.Range(0, gameTips.Length);
        
        // Initial setup
        if (tipText != null) tipText.text = gameTips[index];
        if (tipTextLegacy != null) tipTextLegacy.text = gameTips[index];

        while (true)
        {
            yield return new WaitForSeconds(tipRotationInterval);
            
            index = (index + 1) % gameTips.Length;
            
            // Smoothly fade out the current tip
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / 0.4f);
                if (tipText != null) tipText.color = new Color(tipText.color.r, tipText.color.g, tipText.color.b, alpha);
                if (tipTextLegacy != null) tipTextLegacy.color = new Color(tipTextLegacy.color.r, tipTextLegacy.color.g, tipTextLegacy.color.b, alpha);
                yield return null;
            }
            
            // Swap Text
            if (tipText != null) tipText.text = gameTips[index];
            if (tipTextLegacy != null) tipTextLegacy.text = gameTips[index];
            
            // Smoothly fade in the new tip
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / 0.4f;
                if (tipText != null) tipText.color = new Color(tipText.color.r, tipText.color.g, tipText.color.b, alpha);
                if (tipTextLegacy != null) tipTextLegacy.color = new Color(tipTextLegacy.color.r, tipTextLegacy.color.g, tipTextLegacy.color.b, alpha);
                yield return null;
            }
            
            if (tipText != null) tipText.color = new Color(tipText.color.r, tipText.color.g, tipText.color.b, 1f);
            if (tipTextLegacy != null) tipTextLegacy.color = new Color(tipTextLegacy.color.r, tipTextLegacy.color.g, tipTextLegacy.color.b, 1f);
        }
    }
}