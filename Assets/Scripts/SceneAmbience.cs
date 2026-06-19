using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneAmbience : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip ambienceClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    public bool playOnStart = true;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Ensure correct AudioSource settings for 2D background ambience
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D Sound (covers the entire screen/stereo)
    }

    void Start()
    {
        if (playOnStart && ambienceClip != null)
        {
            PlayAmbience();
        }
    }

    public void PlayAmbience()
    {
        if (audioSource != null && ambienceClip != null)
        {
            audioSource.clip = ambienceClip;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }

    public void StopAmbience()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // Allow changing volume dynamically if needed (e.g. from options menu)
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}
