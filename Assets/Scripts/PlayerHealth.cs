using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor = new Color(0.8f, 0f, 0f, 0.35f);
    [SerializeField] private float hitFlashDuration = 0.15f;

    [Header("UI References")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image hitFlash;

    private int currentHealth;
    private Coroutine hitFlashRoutine;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"[PlayerHealth] Took {damage} damage! Current Health: {currentHealth}/{maxHealth}");
        UpdateHealthUI();

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        if (hitFlash != null)
        {
            hitFlashRoutine = StartCoroutine(ShowHitFlash());
        }

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    private IEnumerator ShowHitFlash()
    {
        hitFlash.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        hitFlash.color = Color.clear;
        hitFlashRoutine = null;
    }

    private void Die()
    {
        isDead = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.enabled = false;
        }

        Rigidbody playerBody = GetComponent<Rigidbody>();
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
        }
    }
}
