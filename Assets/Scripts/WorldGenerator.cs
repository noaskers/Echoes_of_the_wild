using UnityEngine;
using System.Collections;

public class WorldGenerator : MonoBehaviour
{
    // NOTE: The LoadingManager component should be added to this same GameObject
    // It will handle all world generation with async loading

    void Start()
    {
        // LoadingManager will handle all generation
        // This is here for backwards compatibility
        LoadingManager loadingManager = GetComponent<LoadingManager>();
        if (loadingManager == null)
        {
            // Fallback to old system if LoadingManager not present
            StartCoroutine(GenerateWorldLegacy());
        }
    }

    private IEnumerator GenerateWorldLegacy()
    {
        Debug.Log("Starting world generation (legacy mode)...");

        // Generate terrain first
        TerrainGenerator terrain = GetComponent<TerrainGenerator>();
        terrain.GenerateTerrain();

        // Wait for terrain to be fully ready
        yield return new WaitUntil(() => terrain.IsNavMeshReady());
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Applying materials...");
        GetComponent<MaterialApplier>().ApplyMaterials();
        yield return new WaitForEndOfFrame();

        Debug.Log("Placing props...");
        GetComponent<PropsPlacer>().PlaceProps();
        yield return new WaitForEndOfFrame();

        Debug.Log("Spawning animals...");
        GetComponent<AnimalSpawner>().SpawnAnimals();

        Debug.Log("World generation complete!");
    }
}
