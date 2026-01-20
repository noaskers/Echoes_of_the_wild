using UnityEngine;

// Only require TerrainGenerator now; RiverGenerator is removed
[RequireComponent(typeof(TerrainGenerator))]
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerPrefab;
    public float playerSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float playerGravity = -9.81f;

    private TerrainGenerator terrain;
    private GameObject spawnedPlayer;

    void Awake()
    {
        terrain = GetComponent<TerrainGenerator>();
    }

    /// <summary>
    /// Spawns the player only once.
    /// </summary>
    public void SpawnPlayer()
    {
        if (!playerPrefab) return;

        // Prevent multiple spawns
        if (spawnedPlayer != null) return;

        Vector3 spawnPos = FindSafeSpawnPosition();
        spawnedPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Ensure player has CharacterController
        if (spawnedPlayer.GetComponent<CharacterController>() == null)
            spawnedPlayer.AddComponent<CharacterController>();

        // Add and configure SimplePlayer script
        if (spawnedPlayer.GetComponent<SimplePlayer>() == null)
        {
            SimplePlayer sp = spawnedPlayer.AddComponent<SimplePlayer>();
            sp.walkSpeed = playerSpeed;
            sp.sprintSpeed = playerSpeed * 1.7f;
            sp.mouseSensitivity = mouseSensitivity;
            sp.gravity = playerGravity;
        }

        // Assign camera tag
        if (spawnedPlayer.GetComponentInChildren<Camera>() is Camera cam)
            cam.tag = "MainCamera";
    }

    /// <summary>
    /// Finds a safe spawn position away from steep slopes.
    /// </summary>
    Vector3 FindSafeSpawnPosition()
    {
        for (int i = 0; i < 50; i++)
        {
            float x = Random.Range(terrain.terrainSize * 0.2f, terrain.terrainSize * 0.8f);
            float z = Random.Range(terrain.terrainSize * 0.2f, terrain.terrainSize * 0.8f);

            // Get vertex info
            int xi = Mathf.Clamp(Mathf.RoundToInt(x), 0, terrain.terrainSize);
            int zi = Mathf.Clamp(Mathf.RoundToInt(z), 0, terrain.terrainSize);
            int vi = zi * (terrain.terrainSize + 1) + xi;

            float height = terrain.vertices[vi].y;
            float slope = CalculateSlopeAtVertex(vi);

            if (slope < 30f)
                return new Vector3(x, height + 2f, z);
        }

        // Fallback to center
        return new Vector3(terrain.terrainSize / 2f, terrain.heightMultiplier + 2f, terrain.terrainSize / 2f);
    }

    /// <summary>
    /// Calculates approximate slope at a vertex using its normal.
    /// </summary>
    float CalculateSlopeAtVertex(int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= terrain.vertices.Length) return 0f;

        Vector3 normal = Vector3.up;
        if (terrain.mesh.normals.Length > vertexIndex)
            normal = terrain.mesh.normals[vertexIndex];

        return Vector3.Angle(normal, Vector3.up);
    }
}
