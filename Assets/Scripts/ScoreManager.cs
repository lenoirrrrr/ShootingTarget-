using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    public int targetScore = 500;
    private int currentScore = 0;
    private bool isGameOver = false;

    [Header("UI References")]
    public Text legacyScoreText;
    public TextMeshProUGUI tmproScoreText;
    public GameObject victoryPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void AddScore(int points)
    {
        if (isGameOver) return;

        currentScore += points;
        UpdateScoreUI();

        if (currentScore >= targetScore)
        {
            WinGame();
        }
    }

    private void UpdateScoreUI()
    {
        string scoreString = $"Score: {currentScore} / {targetScore}";

        if (tmproScoreText != null)
        {
            tmproScoreText.text = scoreString;
        }

        if (legacyScoreText != null)
        {
            legacyScoreText.text = scoreString;
        }
    }

    private void WinGame()
    {
        isGameOver = true;
        Debug.Log("[ScoreManager] Target score reached! Victory!");

        // Find and disable player movement, shooting, and look
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            PlayerShooting shooting = player.GetComponent<PlayerShooting>();
            if (shooting != null) shooting.enabled = false;

            PlayerLook look = player.GetComponent<PlayerLook>();
            if (look == null) look = player.GetComponentInChildren<PlayerLook>();
            if (look != null) look.enabled = false;

            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null) playerBody.linearVelocity = Vector3.zero;
        }

        // Unlock cursor so user can click menu buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show victory panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }
}
