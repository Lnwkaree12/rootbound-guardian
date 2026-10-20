using System.Collections;
using UnityEngine;

/// <summary>
/// Triggers Lumi's cinematic trap warning cutscene in MainGameplay:
/// When the player approaches (159.09, 9.96, -21.05) near the giant falling stone traps:
/// 1. Locks player movement.
/// 2. Glides Lumi into the exact center of the screen, facing the player/camera with Lumi's back along the +X axis.
/// 3. Lumi speaks: "นี้ระวังแผ่นหินยักบนหัวของเข้าให้ดีๆนะ คอยดู Pattern ของแผ่นหินไว้ด้วยล่ะ"
/// 4. After the dialogue finishes, Lumi smoothly returns to the player's shoulder and gameplay resumes.
/// </summary>
public class LumiTrapWarningCutscene : MonoBehaviour
{
    [Header("Trigger Location")]
    [Tooltip("Target position where cutscene triggers (Cube 8 floor slab)")]
    [SerializeField] private Vector3 triggerPosition = new Vector3(159.09f, 9.96f, -21.05f);
    [Tooltip("Distance threshold to trigger cutscene")]
    [SerializeField] private float triggerRadius = 3.8f;

    [Header("Dialogue Content")]
    [TextArea(2, 4)]
    [SerializeField] private string warningDialogueText = "นี้ระวังแผ่นหินยักบนหัวของเข้าให้ดีๆนะ\nคอยดู Pattern ของแผ่นหินไว้ด้วยล่ะ";
    [SerializeField] private float dialogueDuration = 5.0f;

    [Header("Lumi Stage & Flight")]
    [Tooltip("Distance in front of the active camera where Lumi hovers center-screen")]
    [SerializeField] private float centerScreenDistance = 3.055f; // Increased by 30% from 2.35f
    [Tooltip("Viewport position for center-screen framing (0.5 = exact center)")]
    [SerializeField] private Vector2 viewportCenter = new Vector2(0.5f, 0.35f);
    [Tooltip("Offset of Lumi's visual body center from root transform")]
    [SerializeField] private float lumiBodyYOffset = 0.78f;
    [SerializeField] private float flyToCenterDuration = 1.0f;
    [SerializeField] private float returnToShoulderDuration = 0.8f;

    [Header("State")]
    [SerializeField] private bool hasTriggered = false;

    private GameObject playerObj;
    private PlayerMovement playerMovement;
    private PlayerController playerController;
    private Rigidbody playerRb;
    private PlayerOxygen playerOxygen;
    private LumiCompanion lumi;
    private Camera activeCamera;

    private void Start()
    {
        FindSceneReferences();
    }

    private void FindSceneReferences()
    {
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player Variant");
            if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<PlayerMovement>();
                playerController = playerObj.GetComponent<PlayerController>();
                playerRb = playerObj.GetComponent<Rigidbody>();
                playerOxygen = playerObj.GetComponent<PlayerOxygen>();
                activeCamera = playerObj.GetComponentInChildren<Camera>(true);
            }
        }

        if (activeCamera == null)
        {
            activeCamera = Camera.main;
        }

        if (lumi == null)
        {
            var lumiObj = GameObject.Find("Lumi_Companion");
            if (lumiObj != null)
            {
                lumi = lumiObj.GetComponent<LumiCompanion>();
            }
        }
    }

    private void Update()
    {
        if (hasTriggered) return;

        if (playerObj == null || lumi == null)
        {
            FindSceneReferences();
            if (playerObj == null || lumi == null) return;
        }

        // Horizontal distance check to trigger position
        float dx = playerObj.transform.position.x - triggerPosition.x;
        float dz = playerObj.transform.position.z - triggerPosition.z;
        float horizontalDist = Mathf.Sqrt(dx * dx + dz * dz);

        if (horizontalDist <= triggerRadius)
        {
            StartWarningCutscene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || (playerObj != null && (other.gameObject == playerObj || other.transform.IsChildOf(playerObj.transform))))
        {
            StartWarningCutscene();
        }
    }

    [ContextMenu("Trigger Cutscene Now")]
    public void StartWarningCutscene()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        Debug.Log("[LumiTrapWarningCutscene] Starting Lumi Trap Warning Cutscene...");
        FindSceneReferences();

        // 1. Lock Player Movement & ensure safe state
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (playerObj != null)
        {
            if (playerOxygen != null)
            {
                playerOxygen.UpdateSafeZoneState(true);
            }
        }

        // 2. Put Lumi into cutscene mode
        if (lumi != null)
        {
            lumi.disableAutoDialogue = true;
            lumi.HideBubble();
            // Facing angle 270 degrees = faces -X (towards camera/player), back is along +X axis
            lumi.SetCutsceneMode(true, 270f);
        }

        // 3. Calculate Center-Screen Target Position
        if (activeCamera == null) activeCamera = Camera.main;

        Vector3 targetRootPos;
        Quaternion targetLumiRot;

        if (activeCamera != null)
        {
            Vector3 targetBodyPos = activeCamera.ViewportToWorldPoint(new Vector3(viewportCenter.x, viewportCenter.y, centerScreenDistance));
            targetRootPos = targetBodyPos - new Vector3(0f, lumiBodyYOffset, 0f);
            // Lumi faces camera directly with back facing along the +X axis
            Vector3 toCam = (activeCamera.transform.position - targetBodyPos).normalized;
            targetLumiRot = Quaternion.LookRotation(toCam, Vector3.up);
        }
        else
        {
            // Fallback stage position in front of player
            targetRootPos = triggerPosition + new Vector3(-1.0f, 1.4f, 0f);
            targetLumiRot = Quaternion.Euler(0f, 270f, 0f);
        }

        // 4. Smoothly Glide Lumi to Center Stage
        Vector3 startLumiPos = lumi != null ? lumi.transform.position : targetRootPos;
        Quaternion startLumiRot = lumi != null ? lumi.transform.rotation : targetLumiRot;

        float elapsed = 0f;
        while (elapsed < flyToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flyToCenterDuration);

            if (lumi != null)
            {
                // Re-evaluate center in case of minor camera drift
                if (activeCamera != null)
                {
                    Vector3 curBodyPos = activeCamera.ViewportToWorldPoint(new Vector3(viewportCenter.x, viewportCenter.y, centerScreenDistance));
                    targetRootPos = curBodyPos - new Vector3(0f, lumiBodyYOffset, 0f);
                }

                lumi.transform.position = Vector3.Lerp(startLumiPos, targetRootPos, t);
                lumi.transform.rotation = Quaternion.Slerp(startLumiRot, targetLumiRot, t);
            }

            yield return null;
        }

        if (lumi != null)
        {
            lumi.transform.position = targetRootPos;
            lumi.transform.rotation = targetLumiRot;
        }

        yield return new WaitForSeconds(0.2f);

        // 5. Lumi Delivers the Warning Dialogue!
        if (lumi != null)
        {
            Debug.Log($"[LumiTrapWarningCutscene] Lumi speaks: '{warningDialogueText}'");
            lumi.Speak(warningDialogueText, dialogueDuration - 0.4f);
        }

        // Gentle floating hover while speaking at center screen
        float speechTimer = 0f;
        while (speechTimer < dialogueDuration)
        {
            speechTimer += Time.deltaTime;
            if (lumi != null)
            {
                float bob = Mathf.Sin(speechTimer * 3.5f) * 0.04f;
                Vector3 currentRoot = activeCamera != null
                    ? (activeCamera.ViewportToWorldPoint(new Vector3(viewportCenter.x, viewportCenter.y, centerScreenDistance)) - new Vector3(0f, lumiBodyYOffset, 0f))
                    : targetRootPos;
                lumi.transform.position = currentRoot + new Vector3(0f, bob, 0f);
                lumi.transform.rotation = targetLumiRot;
            }
            yield return null;
        }

        if (lumi != null)
        {
            lumi.HideBubble();
        }

        yield return new WaitForSeconds(0.15f);

        // 6. Smoothly Return Lumi to Player's Shoulder
        if (playerObj != null && lumi != null)
        {
            Vector3 returnStartPos = lumi.transform.position;
            Quaternion returnStartRot = lumi.transform.rotation;
            Vector3 shoulderOffset = new Vector3(-0.75f, 1.35f, -0.4f);

            elapsed = 0f;
            while (elapsed < returnToShoulderDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnToShoulderDuration);

                Vector3 targetShoulderPos = playerObj.transform.position + playerObj.transform.TransformDirection(shoulderOffset);
                Quaternion targetShoulderRot = Quaternion.Euler(0f, 155.4f, 0f);

                lumi.transform.position = Vector3.Lerp(returnStartPos, targetShoulderPos, t);
                lumi.transform.rotation = Quaternion.Slerp(returnStartRot, targetShoulderRot, t);

                yield return null;
            }

            // Restore Lumi companion AI
            lumi.SetCutsceneMode(false);
            lumi.disableAutoDialogue = false;
        }

        // 7. Restore Player Movement & Controls
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (playerOxygen != null) playerOxygen.UpdateSafeZoneState(false);

        Debug.Log("[LumiTrapWarningCutscene] Cutscene finished! Gameplay controls restored.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(triggerPosition, triggerRadius);
    }
}
