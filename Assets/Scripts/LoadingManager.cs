using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class LoadingManager : MonoBehaviour
{
    [Header("Loading Settings")]
    public float minLoadingDuration = 2f; // Minimum time to show loading screen

    private WorldGenerator worldGenerator;
    private TerrainGenerator terrainGenerator;
    private PropsPlacer propsPlacer;
    private AnimalSpawner animalSpawner;

    private float generationStartTime;
    private bool isGenerating = false;

    void Awake()
    {
        // Get references
        worldGenerator = GetComponent<WorldGenerator>();
        terrainGenerator = GetComponent<TerrainGenerator>();
        propsPlacer = GetComponent<PropsPlacer>();
        animalSpawner = GetComponent<AnimalSpawner>();
    }

    void Start()
    {
        // Start world generation
        StartCoroutine(GenerateWorldAsync());
    }

    private IEnumerator GenerateWorldAsync()
    {
        isGenerating = true;
        generationStartTime = Time.time;

        // Stage 1: Generate terrain
        yield return StartCoroutine(GenerateTerrainAsync());

        // Stage 2: Apply materials
        yield return StartCoroutine(ApplyMaterialsAsync());

        // Stage 3: Place props
        yield return StartCoroutine(PlacePropsAsync());

        // Stage 4: Spawn animals
        yield return StartCoroutine(SpawnAnimalsAsync());

        isGenerating = false;
        Debug.Log("World generation complete!");
    }

    private IEnumerator GenerateTerrainAsync()
    {
        // Use task for heavy mesh generation
        Task terrainTask = null;

        terrainTask = Task.Run(() =>
        {
            terrainGenerator.GenerateTerrainData();
        });

        // Wait for terrain task to complete
        while (!terrainTask.IsCompleted)
        {
            yield return new WaitForEndOfFrame();
        }

        if (terrainTask.IsFaulted)
        {
            Debug.LogError("Terrain generation failed: " + terrainTask.Exception);
        }

        // Apply mesh on main thread
        terrainGenerator.ApplyTerrainMesh();

        // Wait for NavMesh
        yield return new WaitUntil(() => terrainGenerator.IsNavMeshReady());
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator ApplyMaterialsAsync()
    {
        yield return new WaitForEndOfFrame();
        GetComponent<MaterialApplier>().ApplyMaterials();
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator PlacePropsAsync()
    {
        yield return new WaitForEndOfFrame();
        propsPlacer.PlaceProps();
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator SpawnAnimalsAsync()
    {
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(animalSpawner.SpawnAnimalsRoutine());
    }

    public bool IsGenerating()
    {
        return isGenerating;
    }

    public float GetGenerationProgress()
    {
        return (Time.time - generationStartTime) / minLoadingDuration;
    }
}
