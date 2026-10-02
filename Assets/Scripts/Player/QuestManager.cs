using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    [Header("Quest UI References")]
    public GameObject questPanel;
    public Text questCategoryText;
    public Text questTitleText;
    public Text questDescText;
    public Text questDetailText;

    [Header("Quest Progress")]
    public bool hasKey = false;

    private static QuestManager instance;
    public static QuestManager Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this); // ONLY destroy the duplicate component, NEVER destroy the GameObject!
            return;
        }

        // Auto-locate references if null
        FindQuestUIReferences();
    }

    void Start()
    {
        UpdateQuestUI();
    }

    private void FindQuestUIReferences()
    {
        if (questPanel == null)
        {
            Transform qp = transform.Find("QuestPanel");
            questPanel = qp != null ? qp.gameObject : GameObject.Find("QuestPanel");
        }

        if (questPanel == null) return;

        if (questCategoryText == null)
        {
            Transform t = questPanel.transform.Find("CategoryText");
            if (t != null) questCategoryText = t.GetComponent<Text>();
        }

        if (questTitleText == null)
        {
            Transform t = questPanel.transform.Find("TitleText");
            if (t != null) questTitleText = t.GetComponent<Text>();
        }

        if (questDescText == null)
        {
            Transform t = questPanel.transform.Find("DescText");
            if (t == null) t = questPanel.transform.Find("ObjectiveRow/DescText");
            if (t != null) questDescText = t.GetComponent<Text>();
        }

        if (questDetailText == null)
        {
            Transform t = questPanel.transform.Find("DetailText");
            if (t != null) questDetailText = t.GetComponent<Text>();
        }
    }

    public void CollectKey()
    {
        hasKey = true;
        UpdateQuestUI();
        Debug.Log("[QuestManager] Key collected! Quest Completed.");
    }

    public void UpdateQuestUI()
    {
        // Ensure references are set
        if (questDescText == null) FindQuestUIReferences();

        if (questDescText != null)
        {
            if (hasKey)
            {
                questDescText.text = "🔑 เก็บกุญแจสำเร็จ! (1/1)";
                questDescText.color = new Color(0.3f, 0.85f, 0.3f); // Green
            }
            else
            {
                questDescText.text = "🔑 เก็บกุญแจสำคัญ (0/1)";
                questDescText.color = Color.white;
            }
        }
    }

    public void UseKeyOnDoor()
    {
        if (questDescText == null) FindQuestUIReferences();

        if (questDescText != null)
        {
            questDescText.text = "🚪 ไขประตูดันเจี้ยนสำเร็จ! (1/1)";
            questDescText.color = new Color(1f, 0.85f, 0.2f); // Gold
        }
        Debug.Log("[QuestManager] Key used on door! Quest cleared.");
    }
}
