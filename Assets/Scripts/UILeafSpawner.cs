using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILeafSpawner : MonoBehaviour
{
    [Header("Leaf Settings")]
    [SerializeField] private Sprite leafSprite; // Sprite gambar daun (PNG Transparan)
    [SerializeField] private int maxLeaves = 25; // Batas jumlah daun maksimal di layar
    [SerializeField] private float spawnRate = 0.8f; // Jeda waktu antar pemunculan daun (detik)
    
    [Header("Movement Range")]
    [SerializeField] private float minFallSpeed = 50f; // Kecepatan jatuh minimal (pixel/detik)
    [SerializeField] private float maxFallSpeed = 150f; // Kecepatan jatuh maksimal (pixel/detik)
    [SerializeField] private float minScale = 0.2f; // Skala ukuran daun paling kecil
    [SerializeField] private float maxScale = 0.6f; // Skala ukuran daun paling besar

    [Header("Wind Effect (Sway)")]
    [SerializeField] private float windStrengthX = 30f; // Dorongan angin konstan ke kanan/kiri
    [SerializeField] private float minSwaySpeed = 1f; // Kecepatan goyangan daun kiri-kanan
    [SerializeField] private float maxSwaySpeed = 3f;
    [SerializeField] private float minSwayWidth = 15f; // Lebar goyangan daun kiri-kanan
    [SerializeField] private float maxSwayWidth = 40f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float minRotateSpeed = 30f; // Kecepatan putar daun
    [SerializeField] private float maxRotateSpeed = 120f;

    private float spawnTimer;
    private RectTransform canvasRect;
    
    private class LeafInstance
    {
        public GameObject obj;
        public RectTransform rectTransform;
        public float fallSpeed;
        public float swaySpeed;
        public float swayWidth;
        public float rotateSpeed;
        public float startX;
        public float birthTime;
    }

    private List<LeafInstance> activeLeaves = new List<LeafInstance>();

    void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        if (leafSprite == null)
        {
            Debug.LogWarning("[UILeafSpawner] Harap pasang gambar daun (Leaf Sprite) di Inspector!");
        }
    }

    void Update()
    {
        if (canvasRect == null || leafSprite == null) return;

        // 1. Pembuatan Daun Baru secara berkala
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnRate && activeLeaves.Count < maxLeaves)
        {
            SpawnLeaf();
            spawnTimer = 0f;
        }

        // 2. Pergerakan dan update daun aktif
        float canvasHeight = canvasRect.rect.height;
        float canvasWidth = canvasRect.rect.width;
        float bottomBoundary = -canvasHeight / 2f - 50f; // Batas bawah layar + offset

        for (int i = activeLeaves.Count - 1; i >= 0; i--)
        {
            LeafInstance leaf = activeLeaves[i];

            if (leaf.obj == null)
            {
                activeLeaves.RemoveAt(i);
                continue;
            }

            // Durasi hidup daun sejak dibuat
            float age = Time.time - leaf.birthTime;

            // Hitung posisi Y (jatuh ke bawah)
            float currentY = leaf.rectTransform.anchoredPosition.y - (leaf.fallSpeed * Time.deltaTime);

            // Hitung posisi X (goyangan daun tertiup angin + dorongan angin konstan)
            float sway = Mathf.Sin(age * leaf.swaySpeed) * leaf.swayWidth;
            float constantWind = age * windStrengthX;
            float currentX = leaf.startX + sway + constantWind;

            // Terapkan posisi baru
            leaf.rectTransform.anchoredPosition = new Vector2(currentX, currentY);

            // Terapkan rotasi
            leaf.rectTransform.Rotate(0f, 0f, leaf.rotateSpeed * Time.deltaTime);

            // Jika daun sudah melewati bagian bawah layar, hapus daun
            if (currentY < bottomBoundary)
            {
                Destroy(leaf.obj);
                activeLeaves.RemoveAt(i);
            }
        }
    }

    private void SpawnLeaf()
    {
        // Membuat GameObject baru untuk satu helai daun
        GameObject leafObj = new GameObject("UI_Leaf", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        
        // Atur agar menjadi anak (child) dari Canvas ini
        leafObj.transform.SetParent(transform, false);

        // Pasang sprite daun
        Image imageComponent = leafObj.GetComponent<Image>();
        imageComponent.sprite = leafSprite;
        imageComponent.raycastTarget = false; // Agar daun tidak menghalangi klik mouse pada tombol menu

        RectTransform leafRect = leafObj.GetComponent<RectTransform>();
        
        // Atur posisi acak di atas layar
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        
        // Memulai dari atas layar (ditambah sedikit offset agar tidak tiba-tiba muncul)
        float startY = canvasHeight / 2f + 50f;
        // Posisi X diacak sepanjang lebar canvas (dikompensasi angin agar tidak menumpuk di satu sisi)
        float startX = Random.Range(-canvasWidth / 2f - 100f, canvasWidth / 2f);

        leafRect.anchoredPosition = new Vector2(startX, startY);

        // Atur skala ukuran daun secara acak
        float scale = Random.Range(minScale, maxScale);
        leafRect.localScale = new Vector3(scale, scale, 1f);

        // Buat data instansi daun
        LeafInstance newLeaf = new LeafInstance
        {
            obj = leafObj,
            rectTransform = leafRect,
            fallSpeed = Random.Range(minFallSpeed, maxFallSpeed),
            swaySpeed = Random.Range(minSwaySpeed, maxSwaySpeed),
            swayWidth = Random.Range(minSwayWidth, maxSwayWidth),
            rotateSpeed = Random.Range(minRotateSpeed, maxRotateSpeed) * (Random.value > 0.5f ? 1f : -1f),
            startX = startX,
            birthTime = Time.time
        };

        activeLeaves.Add(newLeaf);
    }
}
