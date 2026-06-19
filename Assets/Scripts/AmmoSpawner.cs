using UnityEngine;

public class AmmoSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject ammoPickupPrefab;
    public int maxAmmoPickups = 10;
    public float spawnInterval = 10f;

    [Header("Spawn Area (If not using Terrain bounds)")]
    public bool useTerrainBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Height Offset")]
    public float spawnHeightOffset = 0.5f;

    private int activePickupsCount = 0;
    private float nextSpawnTime;

    void Start()
    {
        // Initial spawn of ammo pickups to populate the map at the beginning
        for (int i = 0; i < maxAmmoPickups; i++)
        {
            SpawnAmmo();
        }
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            // Count active pickups in the scene
            activePickupsCount = Object.FindObjectsByType<AmmoPickup>(FindObjectsSortMode.None).Length;

            if (activePickupsCount < maxAmmoPickups)
            {
                SpawnAmmo();
            }
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnAmmo()
    {
        if (ammoPickupPrefab == null) return;

        float targetMinX = minX;
        float targetMaxX = maxX;
        float targetMinZ = minZ;
        float targetMaxZ = maxZ;

        Terrain terrain = Terrain.activeTerrain;
        if (useTerrainBounds && terrain != null)
        {
            Vector3 terrainPos = terrain.transform.position;
            float terrainWidth = terrain.terrainData.size.x;
            float terrainLength = terrain.terrainData.size.z;

            // Stay slightly inside the terrain bounds to avoid spawning right on the edges
            float padding = 10f;
            targetMinX = terrainPos.x + padding;
            targetMaxX = terrainPos.x + terrainWidth - padding;
            targetMinZ = terrainPos.z + padding;
            targetMaxZ = terrainPos.z + terrainLength - padding;
        }

        float randomX = Random.Range(targetMinX, targetMaxX);
        float randomZ = Random.Range(targetMinZ, targetMaxZ);
        float randomY = 0f;

        if (terrain != null)
        {
            // Get height from the terrain collider height map at these coordinates
            randomY = terrain.SampleHeight(new Vector3(randomX, 0, randomZ)) + terrain.transform.position.y;
        }
        else
        {
            // Fallback height if there is no Terrain
            randomY = transform.position.y;
        }

        Vector3 spawnPosition = new Vector3(randomX, randomY + spawnHeightOffset, randomZ);
        
        // Spawn the pickup
        GameObject spawned = Instantiate(ammoPickupPrefab, spawnPosition, Quaternion.identity);
        
        // Parent under this spawner to keep the hierarchy clean
        spawned.transform.parent = this.transform;
    }
}
