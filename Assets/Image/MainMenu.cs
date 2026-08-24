using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenu : MonoBehaviour
{
    
    public void StartGame()
    {
        
        SceneManager.LoadScene("Level1");
    }

    
    public void ContinueGame()
    {
        
        Debug.Log("Loading last save...");
    }

    // ฟังก์ชันสำหรับออกจากเกม
    public void ExitGame()
    {
        Debug.Log("Game is exiting..."); 
        Application.Quit(); 
    }
}