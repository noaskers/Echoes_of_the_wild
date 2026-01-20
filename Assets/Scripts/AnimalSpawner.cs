using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Animal Prefabs")]
    public GameObject[] animalPrefabs;

    [Header("Spawn Settings")]
    public int numberOfAnimals = 3;
    public float spawnRadius = 50f;

    [Header("Performance Settings")]
    public int animalsPerFrame = 2; // How many animals to spawn per frame

    private TerrainGenerator terrain;
    private bool hasSpawned = false;
    private GameObject animalsContainer;

    void Start()
    {
        terrain = GetComponent<TerrainGenerator>();
    }

    public void SpawnAnimals()
    {
        if (hasSpawned) return;
        StartCoroutine(SpawnAnimalsRoutine());
    }

    /// <summary>
    /// Public routine that can be called from LoadingManager
    /// </summary>
    public IEnumerator SpawnAnimalsRoutine()
    {
        Debug.Log("Starting animal spawn...");

        // Wait for terrain and NavMesh
        yield return new WaitUntil(() => terrain != null && terrain.IsNavMeshReady());
        yield return new WaitForSeconds(0.3f);

        // Ensure container exists
        if (animalsContainer == null)
        {
            animalsContainer = GameObject.Find("Animals");
            if (animalsContainer == null)
            {
                animalsContainer = new GameObject("Animals");
                animalsContainer.transform.position = Vector3.zero;
            }
            else if (animalsContainer.transform.parent != null)
            {
                animalsContainer.transform.SetParent(null, true);
            }
        }

        int spawnedCount = 0;
        int batchCount = 0;

        // Spawn in batches
        for (int i = 0; i < numberOfAnimals; i += animalsPerFrame)
        {
            int batchEnd = Mathf.Min(i + animalsPerFrame, numberOfAnimals);

            for (int j = i; j < batchEnd; j++)
            {
                if (SpawnAnimal())
                    spawnedCount++;
            }

            batchCount++;
            yield return null; // Wait one frame between batches
        }

        Debug.Log($"Successfully spawned {spawnedCount}/{numberOfAnimals} animals in {batchCount} batches!");
        hasSpawned = true;
    }

    private bool SpawnAnimal()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition == Vector3.zero)
            return false;

        // Pick random animal
        GameObject animalPrefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];
        GameObject animal = Instantiate(animalPrefab, spawnPosition, Quaternion.identity);

        animal.transform.SetParent(animalsContainer.transform, true);
        animal.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        return true;
    }
    private Vector3 GetRandomSpawnPosition()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 center = new Vector2(terrain.terrainSize / 2f, terrain.terrainSize / 2f);
            Vector2 randomDir = Random.insideUnitCircle * spawnRadius;
            Vector2 spawnXZ = center + randomDir;

            Vector3 spawnPosition = terrain.FindGroundPosition(spawnXZ);

            if (spawnPosition != Vector3.zero && IsValidSpawnPosition(spawnPosition))
                return spawnPosition;
        }

        Debug.LogWarning("Could not find valid spawn position");
        return Vector3.zero;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Reject water
        if (position.y < 0f)
            return false;

        // Reject steep slopes
        float slope = terrain.CalculateSlopeAtPosition(position);
        if (slope > 45f)
            return false;

        // Check for props
        float checkRadius = 0.7f;
        int propLayer = LayerMask.NameToLayer("Props");

        if (propLayer >= 0)
        {
            int mask = 1 << propLayer;
            Collider[] hits = Physics.OverlapSphere(position + Vector3.up * 0.5f, checkRadius, mask);

            if (hits.Length > 0)
                return false;
        }

        return true;
    }
}