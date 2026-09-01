using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    [Header("Quest UI References")]
    public GameObject questPanel;
    public Text questTitleText;
    public Text questDescText;

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
            questPanel = GameObject.Find("QuestPanel");
        }

        if (questTitleText == null)
        {
            GameObject titleObj = GameObject.Find("QuestPanel/TitleText");
            if (titleObj != null) questTitleText = titleObj.GetComponent<Text>();
        }

        if (questDescText == null)
        {
            GameObject descObj = GameObject.Find("QuestPanel/DescText");
            if (descObj != null) questDescText = descObj.GetComponent<Text>();
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
}
