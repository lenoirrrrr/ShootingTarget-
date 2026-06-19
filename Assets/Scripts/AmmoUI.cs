using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [Header("Dependencies")]
    public Gun playerGun;

    [Header("UI Text Options (Assign One)")]
    public Text legacyAmmoText;
    public TextMeshProUGUI tmproAmmoText;

    void Update()
    {
        if (playerGun == null)
        {
            // Auto-locate player gun if not assigned
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerGun = player.GetComponentInChildren<Gun>();
            }
        }

        if (playerGun != null)
        {
            string ammoString = $"{playerGun.CurrentAmmo} / {playerGun.totalAmmo}";

            if (tmproAmmoText != null)
            {
                tmproAmmoText.text = ammoString;
            }

            if (legacyAmmoText != null)
            {
                legacyAmmoText.text = ammoString;
            }
        }
    }
}
