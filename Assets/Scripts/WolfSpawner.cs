using UnityEngine;

public class WolfSpawner : MonoBehaviour
{
    public static WolfSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject wolfBossPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] wolfSpawnPoints;
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Spawning Stats")]
    [SerializeField] private int maxConcurrentWolves = 5;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int totalWolvesToKillForBoss = 10;

    private int activeWolvesCount = 0;
    private int killedWolvesCount = 0;
    private bool bossSpawned = false;
    private float nextSpawnTime;

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

    private void Update()
    {
        // Don't spawn any more regular wolves if we have already spawned the boss or reached the target kills
        if (bossSpawned || killedWolvesCount >= totalWolvesToKillForBoss) return;

        if (activeWolvesCount < maxConcurrentWolves && Time.time >= nextSpawnTime)
        {
            SpawnWolf();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnWolf()
    {
        if (wolfSpawnPoints == null || wolfSpawnPoints.Length == 0 || wolfPrefab == null) return;

        Transform spawnPoint = wolfSpawnPoints[Random.Range(0, wolfSpawnPoints.Length)];
        if (spawnPoint != null)
        {
            Instantiate(wolfPrefab, spawnPoint.position, spawnPoint.rotation);
            activeWolvesCount++;
        }
    }

    public void OnWolfKilled()
    {
        activeWolvesCount = Mathf.Max(activeWolvesCount - 1, 0);
        killedWolvesCount++;
        Debug.Log($"[WolfSpawner] Wolf killed: {killedWolvesCount}/{totalWolvesToKillForBoss}");

        if (killedWolvesCount >= totalWolvesToKillForBoss && !bossSpawned)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        bossSpawned = true;
        if (wolfBossPrefab != null && bossSpawnPoint != null)
        {
            Instantiate(wolfBossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            Debug.Log("[WolfSpawner] 10 Wolves killed! Spawning WOLF BOSS!");
        }
        else
        {
            Debug.LogError("[WolfSpawner] Boss Prefab or Boss Spawn Point is not assigned!");
        }
    }
}
