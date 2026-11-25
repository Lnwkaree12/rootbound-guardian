using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จัดการจุดเซฟ ข้อมูลสำคัญของรอบการเล่น และโหมดการเล่น Tutorial เมื่อโหลดซีนใหม่
/// บันทึกข้อมูลหลักตอนเซฟ:
/// 1. ตำแหน่งผู้เล่น
/// 2. ของในกระเป๋า
/// 3. ออกซิเจนของผู้เล่น
/// 4. เลือดต้นไม้ (และสถานะต้นไม้)
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    // 1. ตำแหน่งผู้เล่นและข้อมูลจุดเซฟ
    private Vector3 lastCheckpointPosition;
    private bool hasCheckpoint = false;
    private bool lastCheckpointHealsOxygen = true;
    private TreeSavePoint lastCheckpointTree;
    private bool lastCheckpointUsesTree;
    private Vector3 lastCheckpointTreePosition;

    // 2. ของในกระเป๋า
    private PlayerDataSave currentSaveData;

    // 3. ออกซิเจนของผู้เล่น ณ ตอนที่เซฟ
    private int savedOxygen = 100;

    // 4. เลือดต้นไม้ ณ ตอนที่เซฟ
    private float savedTreeHealth = 100f;

    // รายการของและกับดักในฉาก
    private HashSet<string> currentPickedItemIDs = new HashSet<string>();
    private HashSet<string> savedPickedItemIDs = new HashSet<string>();

    private HashSet<string> currentOpenedChestIDs = new HashSet<string>();
    private HashSet<string> savedOpenedChestIDs = new HashSet<string>();

    private HashSet<string> currentFallenStalactites = new HashSet<string>();
    private HashSet<string> savedFallenStalactites = new HashSet<string>();

    // สถานะเควสต์
    private bool savedHasKey = false;
    private bool savedDoorUnlocked = false;

    // สถานะ Tutorial และโหมดเมื่อเริ่มฉากใหม่ (true = เล่น Tutorial, false = ข้าม Tutorial)
    private const string TutorialCompletedKey = "OpeningWakeupTutorialCompleted";
    private bool tutorialCompleted = false;
    private bool runTutorialOnNextLoad = true;

    // ธงสำหรับสั่งให้คืนค่า Checkpoint อัตโนมัติหลังโหลดฉากใหม่เสร็จ
    private bool restoreCheckpointOnNextSceneLoad = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// กดเล่นใหม่จากหน้า Pause: คงค่า Checkpoint ที่เซฟไว้ และตั้งค่าให้เล่น Tutorial เมื่อโหลดซีนใหม่
    /// </summary>
    public void RestartFromPauseWithSavedCheckpoint()
    {
        bool shouldSkipTutorial = hasCheckpoint;
        runTutorialOnNextLoad = !shouldSkipTutorial;
        tutorialCompleted = shouldSkipTutorial;
        restoreCheckpointOnNextSceneLoad = hasCheckpoint;
        Debug.Log($"[CheckpointManager] เล่นใหม่จาก Pause -> เล่น Tutorial = {runTutorialOnNextLoad}, คืนค่าเซฟ = {restoreCheckpointOnNextSceneLoad}");
    }

    /// <summary>
    /// กดเล่นใหม่จากหน้า Game Over: คงค่า Checkpoint ที่เซฟไว้ และข้าม Tutorial เมื่อโหลดซีนใหม่
    /// </summary>
    public void RestartFromGameOverWithSavedCheckpoint()
    {
        runTutorialOnNextLoad = false;
        tutorialCompleted = true;
        restoreCheckpointOnNextSceneLoad = hasCheckpoint;
        Debug.Log($"[CheckpointManager] เล่นใหม่จาก Game Over -> เล่น Tutorial = FALSE, คืนค่าเซฟ = {restoreCheckpointOnNextSceneLoad}");
    }

    /// <summary>
    /// เมื่อโหลดซีนใหม่เสร็จ หากมีธงคืนค่า Checkpoint ให้ดึงค่าที่เซฟไว้มาใช้กับผู้เล่นและฉากทันที
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!restoreCheckpointOnNextSceneLoad) return;
        restoreCheckpointOnNextSceneLoad = false;

        if (!hasCheckpoint) return;

        GameObject player = GameObject.Find("Player Variant") ?? GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            RespawnPlayer(player, true);
            Debug.Log("[CheckpointManager] นำค่าที่เซฟไว้กลับมาใช้หลังโหลดซีนใหม่เรียบร้อย");
        }
    }

    /// <summary>
    /// เตรียมเริ่มรอบใหม่จากหน้า Pause: ล้างรอบเก่าและตั้งค่าให้เล่น Tutorial เมื่อโหลดซีนใหม่
    /// </summary>
    public void PrepareRestartFromPause()
    {
        ResetRunState();
        runTutorialOnNextLoad = true;
        tutorialCompleted = false;
        Debug.Log("[CheckpointManager] เตรียมเริ่มใหม่จาก Pause -> เล่น Tutorial = TRUE");
    }

    /// <summary>
    /// เตรียมเริ่มรอบใหม่จากหน้า Game Over: ล้างรอบเก่าและตั้งค่าให้ข้าม Tutorial เมื่อโหลดซีนใหม่
    /// </summary>
    public void PrepareRestartFromGameOver()
    {
        ResetRunState();
        runTutorialOnNextLoad = false;
        tutorialCompleted = true;
        Debug.Log("[CheckpointManager] เตรียมเริ่มใหม่จาก Game Over -> เล่น Tutorial = FALSE");
    }

    /// <summary>
    /// ตั้งค่าว่าเมื่อโหลดซีนถัดไป ให้เล่น Tutorial (true) หรือข้าม Tutorial (false)
    /// </summary>
    public void SetForceTutorialOnNextLoad(bool runTutorial)
    {
        runTutorialOnNextLoad = runTutorial;
        tutorialCompleted = !runTutorial;
    }

    /// <summary>
    /// ตรวจสอบว่าฉากที่เพิ่งโหลดควรเล่น Tutorial หรือไม่
    /// </summary>
    public bool ShouldRunTutorialOnSceneStart()
    {
        return runTutorialOnNextLoad;
    }

    public void MarkItemAsPicked(string itemID)
    {
        if (!currentPickedItemIDs.Contains(itemID))
        {
            currentPickedItemIDs.Add(itemID);
        }
    }

    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp = null, Inventory inventoryComp = null)
    {
        SaveCheckpoint(checkpointPos, healthComp, inventoryComp, true, null);
    }

    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp, Inventory inventoryComp, bool healsOxygen)
    {
        SaveCheckpoint(checkpointPos, healthComp, inventoryComp, healsOxygen, null);
    }

    /// <summary>
    /// บันทึกจุดเซฟ ณ วินาทีที่กดเซฟ / ฟื้นฟูต้นไม้จุดเซฟ / จบ Tutorial Step 9
    /// </summary>
    public void SaveCheckpoint(Vector3 checkpointPos, PlayerHealth healthComp, Inventory inventoryComp, bool healsOxygen, TreeSavePoint sourceTree)
    {
        // 1. บันทึกตำแหน่งผู้เล่นและข้อมูลจุดเซฟ
        lastCheckpointPosition = checkpointPos;
        hasCheckpoint = true;
        lastCheckpointHealsOxygen = healsOxygen;
        lastCheckpointTree = sourceTree;
        lastCheckpointUsesTree = sourceTree != null;
        if (sourceTree != null)
        {
            lastCheckpointTreePosition = sourceTree.transform.position;
        }

        // บันทึกสถานะ Quest ปัจจุบัน
        if (QuestManager.Instance != null)
        {
            savedHasKey = QuestManager.Instance.hasKey;
            QuestManager.Instance.OnCheckpointSaved();
        }

        // 2. บันทึกของในกระเป๋า
        currentSaveData = new PlayerDataSave
        {
            savedItems = inventoryComp != null ? new List<ItemData>(inventoryComp.GetItems()) : new List<ItemData>()
        };

        // 3. บันทึกออกซิเจนของผู้เล่น ณ ตอนเซฟ
        PlayerOxygen oxygenComp = healthComp != null
            ? healthComp.GetComponent<PlayerOxygen>()
            : FindFirstObjectByType<PlayerOxygen>();
        if (oxygenComp != null)
        {
            savedOxygen = oxygenComp.CurrentOxygen;
        }

        // 4. บันทึกเลือดต้นไม้ ณ ตอนเซฟ
        if (sourceTree != null)
        {
            savedTreeHealth = sourceTree.CurrentTreeHealth;
        }

        // บันทึกสถานะไอเทม หีบ และหินงอกที่เกิดขึ้นจนถึงจุดเซฟนี้
        savedOpenedChestIDs = new HashSet<string>(currentOpenedChestIDs);
        savedFallenStalactites = new HashSet<string>(currentFallenStalactites);
        savedPickedItemIDs = new HashSet<string>(currentPickedItemIDs);

        Debug.Log($"[CheckpointManager] เซฟสำเร็จ -> Pos:{checkpointPos}, O2:{savedOxygen}, TreeHP:{savedTreeHealth}, Items:{currentSaveData.savedItems.Count}");
    }

    /// <summary>
    /// ลบจุดเซฟทิ้งหากต้นไม้จุดเซฟเลือดหมดและตำแหน่งตรงกัน
    /// </summary>
    public void InvalidateCheckpointIfMatch(Vector3 treePos)
    {
        if (hasCheckpoint && Vector3.Distance(lastCheckpointPosition, treePos) < 3f)
        {
            hasCheckpoint = false;
            currentSaveData = null;
            lastCheckpointTree = null;
            lastCheckpointUsesTree = false;
            Debug.Log("[CheckpointManager] ⚠️ จุดเซฟปัจจุบันถูกทำลายเพราะต้นไม้หมดอายุ! ลบเซฟทิ้งแล้ว");
        }
    }

    public bool HasValidCheckpoint()
    {
        return hasCheckpoint;
    }

    /// <summary>
    /// คืนค่าทุกอย่างกลับไปตามจุดเซฟที่บันทึกไว้
    /// </summary>
    public void RespawnPlayer(GameObject player, bool restoreTreeFromCheckpoint = true)
    {
        if (!hasCheckpoint || player == null)
        {
            Debug.LogWarning("[CheckpointManager] ❌ ไม่มีจุดเซฟเหลืออยู่! ทำการเรียกหน้า Game Over");
            TriggerGameOverDirectly();
            return;
        }

        // โหลดสถานะ Quest กลับคืนมา
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadQuestState(savedHasKey, savedDoorUnlocked);
        }

        // 1. คืนค่าตำแหน่งผู้เล่น
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = lastCheckpointPosition;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            player.transform.position = lastCheckpointPosition;
        }
        Physics.SyncTransforms();

        // คืนต้นไม้ที่ผูกกับ checkpoint เท่านั้น ไม่ใช้ต้นไม้ Tutorial เป็น fallback
        if (restoreTreeFromCheckpoint && lastCheckpointUsesTree && lastCheckpointTree == null)
        {
            TreeSavePoint[] trees = FindObjectsByType<TreeSavePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            float closestDistanceSqr = 1f;
            foreach (TreeSavePoint tree in trees)
            {
                float distanceSqr = (tree.transform.position - lastCheckpointTreePosition).sqrMagnitude;
                if (distanceSqr <= closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    lastCheckpointTree = tree;
                }
            }
        }
        if (restoreTreeFromCheckpoint && lastCheckpointUsesTree && lastCheckpointTree != null)
        {
            lastCheckpointTree.RestoreFromSavedState(savedTreeHealth);
        }

        bool shouldHealOxygen = lastCheckpointHealsOxygen &&
            (!lastCheckpointUsesTree || (lastCheckpointTree != null && lastCheckpointTree.IsRestored));

        // 4. คืนค่าออกซิเจนของผู้เล่นตามที่เซฟไว้
        PlayerOxygen oxygen = player.GetComponent<PlayerOxygen>();
        if (oxygen != null)
        {
            oxygen.SetOxygenRegenSuppressedUntilSafeZoneExit(!shouldHealOxygen);
            oxygen.SetOxygen(savedOxygen);
        }

        // 3. คืนค่าเลือดผู้เล่นตามที่เซฟไว้
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetHealth(shouldHealOxygen);
            health.ResetHealthAfterRespawn();
        }

        // 2. คืนค่าของในกระเป๋าตามที่เซฟไว้
        Inventory inventory = player.GetComponentInChildren<Inventory>(true);
        if (inventory != null && currentSaveData != null)
        {
            inventory.LoadSavedItems(currentSaveData.savedItems);
        }

        // รีเซ็ตสถานะการเคลื่อนที่ของ PlayerController ให้พร้อมเล่นต่อ
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.ResetAfterRespawn();
        }

        // รีเซ็ตไอเทม หีบ และหินงอกให้ตรงกับตอนที่เซฟ
        currentPickedItemIDs = new HashSet<string>(savedPickedItemIDs);
        ItemObject[] allItems = FindObjectsByType<ItemObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ItemObject item in allItems)
        {
            bool isAlreadyPickedBeforeSave = savedPickedItemIDs.Contains(item.ItemID);
            item.ResetItemState(!isAlreadyPickedBeforeSave);
        }

        currentOpenedChestIDs = new HashSet<string>(savedOpenedChestIDs);
        ChestController[] allChests = FindObjectsByType<ChestController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ChestController chest in allChests)
        {
            bool isOpenedBeforeSave = savedOpenedChestIDs.Contains(chest.ChestID);
            chest.ResetChestState(isOpenedBeforeSave);
        }

        currentFallenStalactites = new HashSet<string>(savedFallenStalactites);
        StalactiteTrap[] allStalactites = FindObjectsByType<StalactiteTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StalactiteTrap trap in allStalactites)
        {
            bool fallenBeforeSave = savedFallenStalactites.Contains(trap.StalactiteID);
            trap.ResetStalactiteState(fallenBeforeSave);
        }
    }

    private void TriggerGameOverDirectly()
    {
        GameOverUIManager gameOverUI = GameOverUIManager.Instance;
        if (gameOverUI == null)
        {
            gameOverUI = FindFirstObjectByType<GameOverUIManager>(FindObjectsInactive.Include);
        }
        if (gameOverUI != null)
        {
            if (!gameOverUI.gameObject.activeSelf)
            {
                gameOverUI.gameObject.SetActive(true);
            }
            gameOverUI.TriggerGameOver("ไม่มีจุดเซฟเหลือรอด! เกมโอเวอร์");
        }
        else
        {
            Debug.LogError("[CheckpointManager] ไม่พบ GameOverUIManager ในฉากเพื่อแสดงหน้าจอ Game Over");
        }
    }

    public void MarkChestAsOpened(string chestID)
    {
        if (!currentOpenedChestIDs.Contains(chestID))
        {
            currentOpenedChestIDs.Add(chestID);
        }
    }

    public bool IsChestOpened(string chestID)
    {
        return savedOpenedChestIDs.Contains(chestID) || currentOpenedChestIDs.Contains(chestID);
    }

    public void MarkStalactiteAsFallen(string stalactiteID)
    {
        if (!currentFallenStalactites.Contains(stalactiteID))
        {
            currentFallenStalactites.Add(stalactiteID);
        }
    }

    public bool IsTutorialCompleted()
    {
        return tutorialCompleted;
    }

    public void MarkTutorialCompleted()
    {
        tutorialCompleted = true;
        runTutorialOnNextLoad = false;
    }

    /// <summary>
    /// ล้างข้อมูลรอบการเล่นปัจจุบัน
    /// </summary>
    public void ResetRunState()
    {
        hasCheckpoint = false;
        restoreCheckpointOnNextSceneLoad = false;
        currentSaveData = null;
        lastCheckpointTree = null;
        lastCheckpointUsesTree = false;
        lastCheckpointTreePosition = Vector3.zero;
        lastCheckpointHealsOxygen = true;

        savedOxygen = 100;
        savedTreeHealth = 100f;

        currentPickedItemIDs.Clear();
        savedPickedItemIDs.Clear();
        currentOpenedChestIDs.Clear();
        savedOpenedChestIDs.Clear();
        currentFallenStalactites.Clear();
        savedFallenStalactites.Clear();

        savedHasKey = false;
        savedDoorUnlocked = false;

        tutorialCompleted = false;
        runTutorialOnNextLoad = true;
        PlayerPrefs.DeleteKey(TutorialCompletedKey);

        Debug.Log("[CheckpointManager] ล้างข้อมูลรอบเก่าทั้งหมดเรียบร้อย");
    }
}