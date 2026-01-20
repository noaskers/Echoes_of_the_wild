using UnityEngine;

public class MaterialApplier : MonoBehaviour
{
    [Header("Materials")]
    public Material groundMaterial;
    public Material mountainMaterial;
    public Material waterMaterial;

    [Header("Height Settings")]
    public float waterLevel = 0f;
    public float mountainStartHeight = 3f;

    private TerrainGenerator terrain;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        terrain = GetComponent<TerrainGenerator>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void ApplyMaterials()
    {
        if (terrain == null || terrain.mesh == null)
        {
            Debug.LogError("Terrain or mesh not found!");
            return;
        }

        // Create mesh with submeshes for different materials
        CreateMultiMaterialMesh();

        Debug.Log("Materials applied successfully!");
    }

    private void CreateMultiMaterialMesh()
    {
        Mesh originalMesh = terrain.mesh;
        Vector3[] vertices = originalMesh.vertices;

        // We need to separate triangles by height/type
        System.Collections.Generic.List<int> groundTriangles = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> mountainTriangles = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> waterTriangles = new System.Collections.Generic.List<int>();

        // Get the triangles from the original mesh
        int[] triangles = originalMesh.triangles;

        // Separate triangles based on vertex heights
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            float avgHeight = (vertices[v1].y + vertices[v2].y + vertices[v3].y) / 3f;

            if (avgHeight <= waterLevel)
            {
                // Water area (below water level)
                waterTriangles.Add(v1);
                waterTriangles.Add(v2);
                waterTriangles.Add(v3);
            }
            else if (avgHeight >= mountainStartHeight)
            {
                // Mountain area
                mountainTriangles.Add(v1);
                mountainTriangles.Add(v2);
                mountainTriangles.Add(v3);
            }
            else
            {
                // Ground area
                groundTriangles.Add(v1);
                groundTriangles.Add(v2);
                groundTriangles.Add(v3);
            }
        }

        // Create new mesh with submeshes
        Mesh newMesh = new Mesh();
        newMesh.vertices = vertices;
        newMesh.normals = originalMesh.normals;
        newMesh.uv = originalMesh.uv;

        // Set submeshes
        newMesh.subMeshCount = 3;
        newMesh.SetTriangles(groundTriangles.ToArray(), 0);
        newMesh.SetTriangles(mountainTriangles.ToArray(), 1);
        newMesh.SetTriangles(waterTriangles.ToArray(), 2);

        // Apply the new mesh
        GetComponent<MeshFilter>().mesh = newMesh;

        // Create materials array
        Material[] materials = new Material[3];
        materials[0] = groundMaterial;
        materials[1] = mountainMaterial;
        materials[2] = waterMaterial;

        meshRenderer.materials = materials;
    }

    // Alternative simpler approach if you don't need multiple materials
    public void ApplySingleMaterial()
    {
        if (meshRenderer != null && groundMaterial != null)
        {
            meshRenderer.material = groundMaterial;
            Debug.Log("Single material applied!");
        }
    }

    // Method to update materials based on vertex colors (alternative approach)
    public void ApplyVertexColors()
    {
        if (terrain == null || terrain.mesh == null) return;

        Color[] colors = new Color[terrain.vertices.Length];

        for (int i = 0; i < terrain.vertices.Length; i++)
        {
            float height = terrain.vertices[i].y;

            if (height <= waterLevel)
            {
                colors[i] = Color.blue; // Water
            }
            else if (height >= mountainStartHeight)
            {
                colors[i] = Color.gray; // Mountain
            }
            else
            {
                colors[i] = Color.green; // Ground
            }
        }

        terrain.mesh.colors = colors;
        terrain.mesh.RecalculateNormals();

        Debug.Log("Vertex colors applied!");
    }
}