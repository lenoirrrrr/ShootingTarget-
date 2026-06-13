using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
  public float reloadTime = 1f;
    public float fireRate = 0.15f;
    public int magSize = 20;

    public GameObject bullet;
    public Transform bulletSpawnPoint;

    public GameObject weaponFlash;

    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;

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
            StartCoroutine(Reload());
            return;
        }

        nextTimeToFire = Time.time + fireRate;
        currentAmmo--;

        if (gunAudioSource != null && fireSound != null)
        {
            gunAudioSource.PlayOneShot(fireSound);
        }

        Instantiate(
            bullet,
            bulletSpawnPoint.position,
            bulletSpawnPoint.rotation
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

        currentAmmo = magSize;
        isReloading = false;
    }

    public void TryReload()
    {
        if (isReloading) return;
        if (currentAmmo == magSize) return;

        StartCoroutine(Reload());
    }

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