using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // เพิ่มตัวนี้เข้ามา

public class PressAnyKeyEffect : MonoBehaviour
{
    public float fadeSpeed = 2.0f;
    private TextMeshProUGUI textElement;
    private bool fadingOut = true;
    private bool isStarting = false; // ป้องกันการกดซ้ำ

    void Awake()
    {
        textElement = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        HandleFading();

        // ตรวจสอบการกดปุ่ม (และยังไม่ได้เริ่มโหลด)
        if (Input.anyKeyDown && !isStarting)
        {
            isStarting = true;
            StartGame();
        }
    }

    void HandleFading()
    {
        Color currentColor = textElement.color;
        float alpha = currentColor.a;

        if (fadingOut)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            if (alpha <= 0) fadingOut = false;
        }
        else
        {
            alpha += Time.deltaTime * fadeSpeed;
            if (alpha >= 1) fadingOut = true;
        }
        textElement.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }

    void StartGame()
    {
        // โหลด Scene ลำดับที่ 1 (หน้า Loading)
        SceneManager.LoadScene(1);
    }
}