using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls the cinematic ending cutscene in EndScene:
/// 1. While walking, keeps the player's original third-person camera attached.
/// 2. When player reaches target position (44.206, 4.59, 29.99):
///    - Locks player movement.
///    - Unparents camera and smoothly pans to frame Lumi dead center on the screen.
///    - Lumi flies to center stage facing the camera.
/// 3. Lumi delivers the dialogue: "นี้คือต้นไม้โลกแห่งโบราณกาลที่เจ้าได้รักษาปกป้องมันเอาไว้ ข้าขอบคุณเจ้ามากนะ".
/// 4. Camera smoothly ascends to majestic overhead bird's-eye view.
/// 5. Restores the ancient tree with magical VFX and progressive leaf-by-leaf sprouting.
/// </summary>
public class EndSceneCutsceneManager : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Target position where cutscene triggers")]
    [SerializeField] private Vector3 triggerPosition = new Vector3(44.206f, 4.59f, 29.99f);
    [Tooltip("Distance threshold for player arrival")]
    [SerializeField] private float triggerRadius = 2.5f;

    [Header("Player & Lumi Stage Settings")]
    [Tooltip("Target position where player stands during dialogue (safely on Cube 2, right of center)")]
    [SerializeField] private Vector3 playerStagePosition = new Vector3(44.75f, 5.89f, 29.99f);

    [Tooltip("Position Lumi moves to so it sits centered and completely unobstructed")]
    [SerializeField] private Vector3 lumiStagePosition = new Vector3(43.65f, 5.70f, 28.80f);
    [SerializeField] private float lumiStageFacingAngle = 180f;

    [Header("Camera Vantage Points")]
    [Tooltip("Camera position framing Lumi center-stage on screen")]
    [SerializeField] private Vector3 lumiCamPosition = new Vector3(44.05f, 5.95f, 26.20f);
    [SerializeField] private float camToLumiTransitionTime = 1.4f;

    [Tooltip("High overhead bird's-eye vantage point")]
    [SerializeField] private Vector3 overheadCamPosition = new Vector3(45.40f, 32.00f, 2.00f);
    [SerializeField] private Vector3 overheadCamRotation = new Vector3(20.85f, 0.0f, 0.0f);
    [SerializeField] private float camToOverheadTransitionTime = 2.8f;
    [SerializeField] private float overheadHoldDuration = 2.5f;
    [SerializeField] private float camReturnToPlayerTime = 2.2f;

    [Header("Dialogue Content")]
    [TextArea(3, 5)]
    [SerializeField] private string lumiDialogueText = "นี้คือต้นไม้โลกแห่งโบราณกาล\nที่เจ้าได้รักษาปกป้องมันเอาไว้...\nข้าขอบคุณเจ้ามากนะ!";
    [SerializeField] private float dialogueReadDuration = 5.0f;

    [Header("Tree Leaf Sprouting Settings")]
    [Tooltip("The ancient tree renderer (auto-finds sakura_cqz if null)")]
    [SerializeField] private MeshRenderer treeRenderer;
    [Tooltip("Index of the leaf material in treeRenderer.materials")]
    [SerializeField] private int leafMaterialIndex = 1;
    [SerializeField] private float bareLeafCutoff = 1.01f;
    [SerializeField] private float bloomedLeafCutoff = 0.23f;
    [SerializeField] private float leafSproutDuration = 4.5f;

    [Header("VFX & Audio")]
    [SerializeField] private GameObject treeRestoreVfxPrefab;
    [SerializeField] private Vector3 treeBaseVfxOffset = new Vector3(45.4f, 5.0f, 44.5f);
    [SerializeField] private Vector3 treeCanopyVfxOffset = new Vector3(45.4f, 20.0f, 44.5f);

    [Header("End Game Victory UI")]
    [Tooltip("Victory celebration UI shown after tree restoration cutscene")]
    [SerializeField] private EndGameVictoryUI victoryUI;
    [SerializeField] private float victoryUIDelay = 1.2f;

    // Runtime state
    private bool cutsceneTriggered = false;
    private GameObject playerObj;
    private PlayerMovement playerMovement;
    private PlayerController playerController;
    private Rigidbody playerRb;
    private LumiCompanion lumi;
    private Camera mainCamera;
    private Material leafMaterialInstance;

    // Original player camera default offset recorded at runtime
    private Vector3 originalCamLocalPos = new Vector3(0f, 1.17f, -2.35f);
    private Quaternion originalCamLocalRot = Quaternion.Euler(19.495f, 0f, 0f);
    private Vector3 originalCamLocalScale = Vector3.one;

    private void Awake()
    {
        FindSceneReferences();
        RecordOriginalCameraTransform();
        InitializeTreeLeafState();
    }

    private void Start()
    {
        FindSceneReferences();
        RecordOriginalCameraTransform();
        EnsureDefaultPlayerCamera();
    }

    private void RecordOriginalCameraTransform()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player Variant");
            if (playerObj == null)
                playerObj = GameObject.FindGameObjectWithTag("Player");
        }

        if (mainCamera != null && playerObj != null)
        {
            if (mainCamera.transform.parent == playerObj.transform)
            {
                originalCamLocalPos = mainCamera.transform.localPosition;
                originalCamLocalRot = mainCamera.transform.localRotation;
                originalCamLocalScale = mainCamera.transform.localScale;
            }
        }
    }

    private void EnsureDefaultPlayerCamera()
    {
        if (cutsceneTriggered) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerObj == null)
            playerObj = GameObject.Find("Player Variant");

        if (mainCamera != null && playerObj != null)
        {
            if (mainCamera.transform.parent != playerObj.transform)
            {
                mainCamera.transform.SetParent(playerObj.transform, false);
                mainCamera.transform.localPosition = originalCamLocalPos;
                mainCamera.transform.localRotation = originalCamLocalRot;
                mainCamera.transform.localScale = originalCamLocalScale;
            }
        }
    }

    private void FindSceneReferences()
    {
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player Variant");
            if (playerObj == null)
                playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<PlayerMovement>();
                playerController = playerObj.GetComponent<PlayerController>();
                playerRb = playerObj.GetComponent<Rigidbody>();
            }
        }

        if (lumi == null)
        {
            var lumiObj = GameObject.Find("Lumi_Companion");
            if (lumiObj != null)
                lumi = lumiObj.GetComponent<LumiCompanion>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (treeRenderer == null)
        {
            var treeObj = GameObject.Find("sakura_cqz");
            if (treeObj != null)
                treeRenderer = treeObj.GetComponent<MeshRenderer>();
        }

        if (victoryUI == null)
        {
            victoryUI = Object.FindFirstObjectByType<EndGameVictoryUI>();
        }
    }

    private void InitializeTreeLeafState()
    {
        if (treeRenderer != null && treeRenderer.sharedMaterials.Length > leafMaterialIndex)
        {
            var mats = treeRenderer.materials;
            if (mats.Length > leafMaterialIndex && mats[leafMaterialIndex] != null)
            {
                leafMaterialInstance = mats[leafMaterialIndex];
                leafMaterialInstance.SetFloat("_Cutoff", bareLeafCutoff);
                treeRenderer.materials = mats;
            }
        }
    }

    private void Update()
    {
        if (cutsceneTriggered) return;

        if (playerObj == null)
        {
            FindSceneReferences();
            if (playerObj == null) return;
        }

        // Distance-based trigger check
        float dist = Vector3.Distance(playerObj.transform.position, triggerPosition);
        if (dist <= triggerRadius)
        {
            StartCutscene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cutsceneTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
        {
            StartCutscene();
        }
    }

    /// <summary>
    /// Initiate the complete cinematic cutscene sequence.
    /// </summary>
    public void StartCutscene()
    {
        if (cutsceneTriggered) return;
        cutsceneTriggered = true;

        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        // -------------------------------------------------------------
        // Step 1: Lock Player Input & Secure Character Position
        // -------------------------------------------------------------
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true; // Freeze physics to prevent falling or displacement during cutscene
        }

        // Align player at safe stage position on Cube (2) facing tree/dialogue
        if (playerObj != null)
        {
            playerObj.transform.position = playerStagePosition;
            playerObj.transform.rotation = Quaternion.identity;

            // Ensure player does not suffocate or take damage during cutscene
            var oxy = playerObj.GetComponent<PlayerOxygen>();
            if (oxy != null)
            {
                oxy.ResetOxygen();
                oxy.UpdateSafeZoneState(true);
            }
        }

        // Detach Camera from Player so it can execute cinematic camera choreographies
        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null, true);
        }

        // Put Lumi into cutscene mode
        if (lumi != null)
        {
            lumi.disableAutoDialogue = true;
            lumi.HideBubble();
            lumi.SetCutsceneMode(true, lumiStageFacingAngle);
        }

        // -------------------------------------------------------------
        // Step 2: Camera & Lumi Glide Synchronously to Frame Lumi CENTER STAGE
        // -------------------------------------------------------------
        Vector3 startLumiPos = lumi != null ? lumi.transform.position : lumiStagePosition;
        Vector3 startCamPos = mainCamera != null ? mainCamera.transform.position : lumiCamPosition;
        Quaternion startCamRot = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;

        // Target camera orientation directly looks at Lumi & tree with subtle upward tilt
        Quaternion targetCamRot = Quaternion.Euler(2f, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < camToLumiTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / camToLumiTransitionTime);

            if (lumi != null)
            {
                lumi.transform.position = Vector3.Lerp(startLumiPos, lumiStagePosition, t);
                lumi.transform.rotation = Quaternion.Euler(0f, lumiStageFacingAngle, 0f);
            }

            if (mainCamera != null)
            {
                mainCamera.transform.position = Vector3.Lerp(startCamPos, lumiCamPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
            }

            yield return null;
        }

        if (lumi != null)
        {
            lumi.transform.position = lumiStagePosition;
            lumi.transform.rotation = Quaternion.Euler(0f, lumiStageFacingAngle, 0f);
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position = lumiCamPosition;
            mainCamera.transform.rotation = targetCamRot;
        }

        yield return new WaitForSeconds(0.3f);

        // -------------------------------------------------------------
        // Step 3: Lumi Speaks Dialogue (Right at Screen Center)
        // -------------------------------------------------------------
        if (lumi != null)
        {
            lumi.Speak(lumiDialogueText, dialogueReadDuration - 0.5f);
        }

        yield return new WaitForSeconds(dialogueReadDuration);

        // -------------------------------------------------------------
        // Step 4: Camera Ascends to Majestic Overhead Bird's-Eye View
        // -------------------------------------------------------------
        if (mainCamera != null)
        {
            Vector3 camCurrentPos = mainCamera.transform.position;
            Quaternion camCurrentRot = mainCamera.transform.rotation;
            Quaternion overheadRot = Quaternion.Euler(overheadCamRotation);

            elapsed = 0f;
            while (elapsed < camToOverheadTransitionTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / camToOverheadTransitionTime);
                mainCamera.transform.position = Vector3.Lerp(camCurrentPos, overheadCamPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(camCurrentRot, overheadRot, t);
                yield return null;
            }
            mainCamera.transform.position = overheadCamPosition;
            mainCamera.transform.rotation = overheadRot;
        }

        yield return new WaitForSeconds(0.3f);

        // -------------------------------------------------------------
        // Step 5: Trigger Magical VFX & Sprout Leaves Leaf-by-Leaf
        // -------------------------------------------------------------
        // Spawn Base VFX
        if (treeRestoreVfxPrefab != null)
        {
            GameObject baseVfx = Instantiate(treeRestoreVfxPrefab, treeBaseVfxOffset, Quaternion.identity);
            baseVfx.transform.localScale = Vector3.one * 5f;
            Destroy(baseVfx, 10f);

            // Spawn Canopy VFX slightly delayed
            StartCoroutine(SpawnCanopyVfxRoutine());
        }

        // Animate leaf sprouting by smoothly lowering _Cutoff
        if (leafMaterialInstance != null)
        {
            elapsed = 0f;
            while (elapsed < leafSproutDuration)
            {
                elapsed += Time.deltaTime;
                float rawT = elapsed / leafSproutDuration;
                
                // Natural organic blooming curve: start gentle, bloom quickly, settle softly
                float curvedT = Mathf.SmoothStep(0f, 1f, rawT);
                float currentCutoff = Mathf.Lerp(bareLeafCutoff, bloomedLeafCutoff, curvedT);

                leafMaterialInstance.SetFloat("_Cutoff", currentCutoff);
                yield return null;
            }
            leafMaterialInstance.SetFloat("_Cutoff", bloomedLeafCutoff);
        }

        // -------------------------------------------------------------
        // Step 6: Grand Finale - Hold Majestic View & Gentle Ambient Drift
        // -------------------------------------------------------------
        float finaleElapsed = 0f;
        Vector3 finalOverheadCamPos = overheadCamPosition;

        while (finaleElapsed < overheadHoldDuration)
        {
            finaleElapsed += Time.deltaTime;
            if (mainCamera != null)
            {
                // Subtle tranquil camera float
                float driftY = Mathf.Sin(finaleElapsed * 0.8f) * 0.4f;
                mainCamera.transform.position = finalOverheadCamPos + new Vector3(0f, driftY, 0f);
            }
            yield return null;
        }

        // -------------------------------------------------------------
        // Step 7: Smoothly Return Camera & Controls to Player
        // -------------------------------------------------------------
        if (mainCamera != null && playerObj != null)
        {
            Vector3 camCurrentPos = mainCamera.transform.position;
            Quaternion camCurrentRot = mainCamera.transform.rotation;

            elapsed = 0f;
            while (elapsed < camReturnToPlayerTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / camReturnToPlayerTime);

                Vector3 targetCamWorldPos = playerObj.transform.TransformPoint(originalCamLocalPos);
                Quaternion targetCamWorldRot = playerObj.transform.rotation * originalCamLocalRot;

                mainCamera.transform.position = Vector3.Lerp(camCurrentPos, targetCamWorldPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(camCurrentRot, targetCamWorldRot, t);
                yield return null;
            }

            // Re-parent camera to player with exact original transform
            mainCamera.transform.SetParent(playerObj.transform, false);
            mainCamera.transform.localPosition = originalCamLocalPos;
            mainCamera.transform.localRotation = originalCamLocalRot;
            mainCamera.transform.localScale = originalCamLocalScale;
        }

        // Anchor player solidly on top of Cube (2) platform
        if (playerObj != null)
        {
            playerObj.transform.position = playerStagePosition;
            playerObj.transform.rotation = Quaternion.identity;
        }

        // Return Lumi to normal companion following behavior
        if (lumi != null)
        {
            lumi.SetCutsceneMode(false);
            lumi.disableAutoDialogue = false;
        }

        // Unfreeze physics and restore Player Controls for free exploration of the restored sacred island
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerController != null) playerController.enabled = true;

        // -------------------------------------------------------------
        // Step 8: Trigger Victory / The End Celebration UI
        // -------------------------------------------------------------
        if (victoryUI == null)
        {
            victoryUI = Object.FindFirstObjectByType<EndGameVictoryUI>();
        }

        if (victoryUI != null)
        {
            yield return new WaitForSeconds(victoryUIDelay);
            victoryUI.ShowVictoryScreen();
        }
    }

    private IEnumerator SpawnCanopyVfxRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        if (treeRestoreVfxPrefab != null)
        {
            GameObject canopyVfx = Instantiate(treeRestoreVfxPrefab, treeCanopyVfxOffset, Quaternion.identity);
            canopyVfx.transform.localScale = Vector3.one * 7f;
            Destroy(canopyVfx, 10f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerStagePosition, 0.5f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lumiStagePosition, 0.4f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(lumiCamPosition, Vector3.one * 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(overheadCamPosition, Vector3.one * 1.5f);
    }
}
