using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Settings")]
    public int ammoAmount = 20;
    public AudioClip pickupSound;

    [Header("Visual Effects")]
    public float rotationSpeed = 50f;
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.2f;

    [Header("Glowing Effect")]
    public Color glowColor = Color.cyan;
    public float lightRange = 6f;
    public float baseLightIntensity = 2.5f;

    private Vector3 startPos;
    private Light pointLight;

    void Start()
    {
        startPos = transform.position;

        // Auto-configure a Point Light so it glows in the dark/map
        pointLight = GetComponent<Light>();
        if (pointLight == null)
        {
            pointLight = gameObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
        }
        
        pointLight.color = glowColor;
        pointLight.range = lightRange;
        pointLight.intensity = baseLightIntensity;
        // Make sure it doesn't cast shadows to save performance
        pointLight.shadows = LightShadows.None; 
    }

    void Update()
    {
        // Make the pickup rotate and float nicely in the air for visual polish
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        
        float sinWave = Mathf.Sin(Time.time * floatSpeed);
        Vector3 tempPos = startPos;
        tempPos.y += sinWave * floatAmplitude;
        transform.position = tempPos;

        // Make the glowing light pulse in sync with the floating animation
        if (pointLight != null)
        {
            pointLight.intensity = baseLightIntensity + (sinWave * 1.0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Gun gun = other.GetComponentInChildren<Gun>();
            if (gun != null)
            {
                gun.AddAmmo(ammoAmount);

                if (pickupSound != null)
                {
                    if (gun.gunAudioSource != null)
                    {
                        gun.gunAudioSource.PlayOneShot(pickupSound);
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    }
                }

                Destroy(gameObject);
            }
        }
    }
}
