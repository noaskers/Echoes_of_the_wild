using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Threading;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainSize = 200;
    public float radius = 80f;
    public float noiseScale = 0.05f;
    public float heightMultiplier = 5f;
    public float mountainHeight = 15f;

    [Header("NavMesh Settings")]
    public bool autoGenerateNavMesh = true;

    [HideInInspector] public Mesh mesh;
    [HideInInspector] public Vector3[] vertices;
    [HideInInspector] public int[] triangles;

    private MeshFilter mf;
    private MeshCollider mc;
    private bool isNavMeshReady = false;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();
    }

    public void GenerateTerrain()
    {
        StartCoroutine(GenerateTerrainRoutine());
    }

    /// <summary>
    /// Generates terrain data on a background thread (called by LoadingManager)
    /// </summary>
    public void GenerateTerrainData()
    {
        vertices = new Vector3[(terrainSize + 1) * (terrainSize + 1)];
        Vector2 center = new Vector2(terrainSize / 2f, terrainSize / 2f);

        // This runs on background thread - NO Unity API calls
        for (int z = 0, i = 0; z <= terrainSize; z++)
        {
            for (int x = 0; x <= terrainSize; x++, i++)
            {
                float distance = Vector2.Distance(center, new Vector2(x, z));
                float y = Mathf.PerlinNoise(x * noiseScale, z * noiseScale) * heightMultiplier;

                if (distance > radius) y = -5f;
                else
                {
                    float edgeFactor = Mathf.Clamp01((distance - (radius * 0.7f)) / (radius * 0.3f));
                    y += Mathf.Pow(edgeFactor, 2f) * mountainHeight;
                }

                vertices[i] = new Vector3(x, Mathf.Max(y, -2f), z);
            }
        }

        // Create triangles on background thread
        triangles = new int[terrainSize * terrainSize * 6];
        for (int z = 0, vert = 0, tris = 0; z < terrainSize; z++, vert++)
        {
            for (int x = 0; x < terrainSize; x++, vert++, tris += 6)
            {
                triangles[tris + 0] = vert;
                triangles[tris + 1] = vert + terrainSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + terrainSize + 1;
                triangles[tris + 5] = vert + terrainSize + 2;
            }
        }
    }

    /// <summary>
    /// Applies the generated mesh to the GameObject (called on main thread)
    /// </summary>
    public void ApplyTerrainMesh()
    {
        if (vertices == null || triangles == null)
        {
            Debug.LogError("Terrain data not generated!");
            return;
        }

        mesh = new Mesh();
        mesh.name = "GeneratedTerrain";

        // Set vertices and triangles
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Apply mesh to components
        mf.mesh = mesh;
        mc.sharedMesh = mesh;

        Debug.Log("Terrain mesh applied!");

        // Start NavMesh setup
        if (autoGenerateNavMesh)
        {
            StartCoroutine(SetupNavMesh());
        }
        else
        {
            isNavMeshReady = true;
        }
    }

    private IEnumerator GenerateTerrainRoutine()
    {
        // Legacy support for direct calls
        GenerateTerrainData();
        yield return null;
        ApplyTerrainMesh();
    }

    private IEnumerator SetupNavMesh()
    {
        Debug.Log("Setting up NavMesh...");

        // Remove any existing NavMesh
        NavMesh.RemoveAllNavMeshData();
        yield return new WaitForEndOfFrame();

        // Ensure the terrain is on the correct layer for NavMesh baking
        gameObject.layer = 0; // Default layer

        // Wait for changes to take effect
        yield return new WaitForEndOfFrame();

        Debug.Log("NavMesh setup complete. Please bake NavMesh manually in Window > AI > Navigation");

        // For automatic spawning, we'll use a simple approach
        // Animals will check for NavMesh at spawn time
        isNavMeshReady = true;
    }

    public bool IsNavMeshReady()
    {
        return isNavMeshReady;
    }

    // Helper method to find ground position
    public Vector3 FindGroundPosition(Vector2 xzPosition)
    {
        float x = Mathf.Clamp(xzPosition.x, 0, terrainSize);
        float z = Mathf.Clamp(xzPosition.y, 0, terrainSize);

        int index = Mathf.RoundToInt(z) * (terrainSize + 1) + Mathf.RoundToInt(x);
        if (index >= 0 && index < vertices.Length)
        {
            return vertices[index];
        }
        return Vector3.zero;
    }

    // Calculate slope at a vertex
    public float CalculateSlopeAtVertex(int vertexIndex)
    {
        if (mesh == null || vertexIndex < 0 || vertexIndex >= mesh.normals.Length)
        {
            return 0f;
        }

        Vector3 normal = mesh.normals[vertexIndex];
        return Vector3.Angle(normal, Vector3.up);
    }

    // Calculate slope at world position
    public float CalculateSlopeAtPosition(Vector3 worldPosition)
    {
        if (mesh == null) return 0f;

        // Convert world position to local terrain coordinates
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);

        // Find the closest vertex
        int closestVertex = FindClosestVertex(localPos);

        return CalculateSlopeAtVertex(closestVertex);
    }

    // Find closest vertex to a position
    private int FindClosestVertex(Vector3 localPosition)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}