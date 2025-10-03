using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void playGame()
    {
        // Add logic to start the game
        SceneManager.LoadScene("GameScene");
    }

    public void quitGame()
    {
        Application.Quit();
    }
}
