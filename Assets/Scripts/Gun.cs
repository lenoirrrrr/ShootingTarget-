using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
  public float reloadTime = 1f;
    public float fireRate = 0.15f;
    public int magSize = 30;

    public GameObject bullet;
    public Transform bulletSpawnPoint;

    public GameObject weaponFlash;

    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;

    [Header("Ammo System")]
    public int totalAmmo = 60;
    public int maxCarriedAmmo = 120;

    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    private Quaternion initalRotation;
    private Vector3 initalPosition;
    private Vector3 reloadRotationOffset = new Vector3(66, 50, 50);

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;

    void Start()
    {
        currentAmmo = magSize;
        initalRotation = transform.localRotation;
        initalPosition = transform.localPosition;
    }

    public void Shoot()
    {
        if (isReloading) return;
        if (Time.time < nextTimeToFire) return;

        if (currentAmmo <= 0)
        {
            if (totalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            return;
        }

        nextTimeToFire = Time.time + fireRate;
        currentAmmo--;

        if (gunAudioSource != null && fireSound != null)
        {
            gunAudioSource.PlayOneShot(fireSound);
        }

        // Tembakkan ray dari tengah layar kamera (crosshair)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        // Jika mengenai sesuatu, arahkan ke titik itu. Jika tidak, arahkan ke titik 100m di depan
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        // Hitung arah dari moncong senjata ke target
        Vector3 shootDirection = (targetPoint - bulletSpawnPoint.position).normalized;

        // Karena Bullet.cs menggunakan -transform.right untuk bergerak maju, 
        // kita putar rotasi awalnya agar sejajar dengan arah tembakan (LookRotation + offset 90 derajat Y)
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection) * Quaternion.Euler(0, 90, 0);

        Instantiate(
            bullet,
            bulletSpawnPoint.position,
            bulletRotation
        );
        
        Instantiate(
            weaponFlash,
            bulletSpawnPoint.position,
            bulletSpawnPoint.rotation
        );

        StopCoroutine(nameof(Recoil));
        StartCoroutine(nameof(Recoil));
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (gunAudioSource != null && reloadSound != null)
        {
            gunAudioSource.PlayOneShot(reloadSound);
        }

        Quaternion targetRotation =
            Quaternion.Euler(initalRotation.eulerAngles + reloadRotationOffset);

        float halfReload = reloadTime / 2f;
        float t = 0f;

        while (t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initalRotation, targetRotation, t / halfReload);
            yield return null;
        }

        t = 0f;

        while (t < halfReload)
        {
            t += Time.deltaTime;

            transform.localRotation =
                Quaternion.Slerp(
                    targetRotation,
                    initalRotation,
                    t / halfReload
                );

            yield return null;
        }

        int ammoNeeded = magSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, totalAmmo);
        totalAmmo -= ammoToLoad;
        currentAmmo += ammoToLoad;
        isReloading = false;
    }

    public void TryReload()
    {
        if (isReloading) return;
        if (currentAmmo == magSize) return;
        if (totalAmmo <= 0) return;

        StartCoroutine(Reload());
    }

    public void AddAmmo(int amount)
    {
        totalAmmo = Mathf.Min(totalAmmo + amount, maxCarriedAmmo);
    }

    public int CurrentAmmo => currentAmmo;

    private IEnumerator Recoil()
    {
        Vector3 recoilTarget = initalPosition + new Vector3 (recoilDistance, 0, 0);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed; 
            transform.localPosition = Vector3.Lerp(initalPosition, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(recoilTarget, initalPosition, t);
            yield return null;
        }

        transform.localPosition = initalPosition;
    }
} 