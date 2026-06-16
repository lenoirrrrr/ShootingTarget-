using UnityEngine;

public class MenuBackgroundEffects : MonoBehaviour
{
    [Header("Floating (Drift) Effect")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatSpeedX = 0.5f;
    [SerializeField] private float floatSpeedY = 0.3f;
    [SerializeField] private float floatAmountX = 15f;
    [SerializeField] private float floatAmountY = 10f;

    [Header("Breathing (Scale) Effect")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breatheSpeed = 0.4f;
    [SerializeField] private float breatheAmount = 0.03f;

    [Header("Mouse Parallax Effect")]
    [SerializeField] private bool enableMouseParallax = true;
    [SerializeField] private float parallaxStrength = 20f;
    [SerializeField] private float smoothTime = 0.2f;

    private RectTransform rectTransform;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialPosition = rectTransform.localPosition;
            initialScale = rectTransform.localScale;
            
            // Sedikit memperbesar background agar pinggirannya tidak terlihat berlubang saat bergerak
            rectTransform.localScale = initialScale * 1.1f;
            initialScale = rectTransform.localScale;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        Vector3 nextPosition = initialPosition;

        // 1. Efek Mengambang (Floating/Drift) dengan Sine Wave
        if (enableFloating)
        {
            float offsetX = Mathf.Sin(Time.time * floatSpeedX) * floatAmountX;
            float offsetY = Mathf.Cos(Time.time * floatSpeedY) * floatAmountY;
            nextPosition += new Vector3(offsetX, offsetY, 0f);
        }

        // 2. Efek Parallax Mengikuti Kursor Mouse
        if (enableMouseParallax)
        {
            // Ambil posisi mouse relatif terhadap ukuran layar (-0.5 sampai 0.5)
            float mouseX = (Input.mousePosition.x / Screen.width) - 0.5f;
            float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;

            // Target posisi berlawanan dengan arah mouse untuk efek kedalaman
            Vector3 parallaxOffset = new Vector3(-mouseX * parallaxStrength, -mouseY * parallaxStrength, 0f);
            targetPosition = nextPosition + parallaxOffset;

            // Haluskan pergerakan
            rectTransform.localPosition = Vector3.SmoothDamp(
                rectTransform.localPosition, 
                targetPosition, 
                ref currentVelocity, 
                smoothTime
            );
        }
        else
        {
            rectTransform.localPosition = nextPosition;
        }

        // 3. Efek Bernafas (Breathing Scale)
        if (enableBreathing)
        {
            float scaleOffset = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            rectTransform.localScale = initialScale + new Vector3(scaleOffset, scaleOffset, 0f);
        }
    }
}
