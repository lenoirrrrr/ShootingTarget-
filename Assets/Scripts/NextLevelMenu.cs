using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelMenu : MonoBehaviour
{
    public void NextLevel()
    {
        // Memuat scene berikutnya dalam antrean Build Settings (level selanjutnya)
        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextBuildIndex);
        }
        else
        {
            // Jika tidak ada level berikutnya, kembali ke Main Menu (index 0)
            SceneManager.LoadScene(0);
        }
    }

    public void GoToMainMenu()
    {
        // Memuat scene Main Menu (index 0 di Build Settings)
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}
