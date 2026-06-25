using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject pauseMenuPanel;

    private bool isPaused = false;
    private MonoBehaviour playerMovement;
    private MonoBehaviour playerShooting;

    private void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Find player scripts
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent("PlayerMovement") as MonoBehaviour;
            playerShooting = player.GetComponent("PlayerShooting") as MonoBehaviour;
        }
    }

    private void Update()
    {
        bool escPressed = false;

#if UNITY_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
#endif

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }

        if (escPressed)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f; // Resume game time

        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player controls
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;

        isPaused = false;
    }

    public void Pause()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Freeze game time

        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player controls to prevent shooting/moving while paused
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;

        isPaused = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Reset time scale before loading scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Reset time scale before loading scene
        SceneManager.LoadScene(0); // Load Main Menu
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}
