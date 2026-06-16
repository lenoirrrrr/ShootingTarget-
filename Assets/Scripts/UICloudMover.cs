using UnityEngine;

public class UICloudMover : MonoBehaviour
{
    [Header("Cloud Settings")]
    [SerializeField] private float speed = 15f; // Kecepatan bergerak (pixel per detik)
    [SerializeField] private bool moveRight = true; // Arah pergerakan

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private float cloudWidth;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
        
        // Ambil lebar awan untuk perhitungan batas layar
        cloudWidth = rectTransform.rect.width * rectTransform.localScale.x;
    }

    void Update()
    {
        if (rectTransform == null || canvasRect == null) return;

        // Gerakkan awan
        float direction = moveRight ? 1f : -1f;
        rectTransform.anchoredPosition += new Vector2(direction * speed * Time.deltaTime, 0f);

        float canvasHalfWidth = canvasRect.rect.width / 2f;
        float boundary = canvasHalfWidth + (cloudWidth / 2f);

        // Jika awan keluar dari batas layar kanan/kiri, kembalikan ke sisi sebaliknya
        if (moveRight && rectTransform.anchoredPosition.x > boundary)
        {
            rectTransform.anchoredPosition = new Vector2(-boundary, rectTransform.anchoredPosition.y);
        }
        else if (!moveRight && rectTransform.anchoredPosition.x < -boundary)
        {
            rectTransform.anchoredPosition = new Vector2(boundary, rectTransform.anchoredPosition.y);
        }
    }
}
