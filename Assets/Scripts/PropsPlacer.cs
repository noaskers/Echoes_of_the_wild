using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class PropsPlacer : MonoBehaviour
{
    [System.Serializable]
    public class PropGroup
    {
        public string groupName = "New Group";
        public GameObject[] prefabs;

        [Header("Cluster Settings")]
        public int minPropsInCluster = 3;
        public int maxPropsInCluster = 10;
        public float clusterRadius = 8f;
    }

    [System.Serializable]
    public class PropData
    {
        public Vector3 position;
        public float rotation;
        public float scale;
        public int prefabIndex;
        public int groupIndex;
    }

    [Header("Prop Groups")]
    public PropGroup[] propGroups;

    [Header("Spawn Area")]
    public float spawnRadius = 80f;

    [Header("Global Rules")]
    public float maxSlopeAngle = 30f;
    public float minDistanceBetweenProps = 1.4f;

    [Header("Batch Settings")]
    public int propsPerFrame = 10; // How many props to instantiate per frame

    private TerrainGenerator terrain;
    private List<Vector3> placedPositions = new List<Vector3>();
    private List<PropData> propsToInstantiate = new List<PropData>();
    private GameObject propsContainer;
    private DayNightCycle dayNightCycle;
    private List<GameObject> allProps = new List<GameObject>();

    void Awake()
    {
        terrain = GetComponent<TerrainGenerator>();
        dayNightCycle = GetComponent<DayNightCycle>();
    }

    void Update()
    {
        // Disable shadows on props during night to prevent floating effect
        if (dayNightCycle != null && allProps.Count > 0)
        {
            float currentAngle = dayNightCycle.transform.eulerAngles.x;
            bool isNight = currentAngle > 120f && currentAngle < 240f; // Night time window

            foreach (var prop in allProps)
            {
                if (prop != null)
                {
                    Renderer renderer = prop.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.shadowCastingMode = isNight ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                }
            }
        }
    }

    public void PlaceProps()
    {
        if (propGroups.Length == 0)
        {
            Debug.LogWarning("No prop groups defined!");
            return;
        }

        placedPositions.Clear();
        propsToInstantiate.Clear();
        allProps.Clear();

        // Start batched prop placement
        StartCoroutine(PlacePropsAsync());
    }

    private IEnumerator PlacePropsAsync()
    {
        foreach (var group in propGroups)
        {
            yield return StartCoroutine(PlacePropGroupAsync(group));
        }

        Debug.Log($"Prop placement complete. Total props: {allProps.Count}");
    }

    private IEnumerator PlacePropGroupAsync(PropGroup group)
    {
        int clusterCount = Mathf.RoundToInt(Random.Range(5, 12));

        for (int i = 0; i < clusterCount; i++)
        {
            TryPlaceCluster(group);

            // Yield every few clusters to prevent frame stutters
            if (i % 3 == 0)
                yield return null;
        }
    }

    /// <summary>
    /// Generates prop data on background thread (called by LoadingManager)
    /// </summary>
    public void GeneratePropsDataAsync()
    {
        if (propGroups.Length == 0)
        {
            Debug.LogWarning("No prop groups defined!");
            return;
        }

        propsToInstantiate.Clear();
        placedPositions.Clear();

        // Use seeded random for consistency
        System.Random seedRandom = new System.Random(12345);

        for (int groupIdx = 0; groupIdx < propGroups.Length; groupIdx++)
        {
            PropGroup group = propGroups[groupIdx];
            int clusterCount = seedRandom.Next(5, 12);

            for (int i = 0; i < clusterCount; i++)
            {
                GenerateClusterData(group, groupIdx, seedRandom);
            }
        }

        Debug.Log($"Generated {propsToInstantiate.Count} prop placement data points");
    }

    private void GenerateClusterData(PropGroup group, int groupIndex, System.Random seedRandom)
    {
        Vector2 center2D = new Vector2(terrain.terrainSize / 2f, terrain.terrainSize / 2f);

        // Generate random direction
        float angle = (float)(seedRandom.NextDouble() * 2 * Mathf.PI);
        float distance = (float)(seedRandom.NextDouble() * spawnRadius);
        Vector2 randomDir = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
        Vector2 spawnXZ = center2D + randomDir;

        Vector3 clusterCenter = terrain.FindGroundPosition(spawnXZ);
        if (clusterCenter == Vector3.zero)
            return;

        // Check slope
        if (terrain.CalculateSlopeAtPosition(clusterCenter) > maxSlopeAngle)
            return;

        int count = seedRandom.Next(group.minPropsInCluster, group.maxPropsInCluster + 1);

        for (int i = 0; i < count; i++)
        {
            GenerateSinglePropData(group, groupIndex, clusterCenter, seedRandom);
        }
    }

    private void GenerateSinglePropData(PropGroup group, int groupIndex, Vector3 clusterCenter, System.Random seedRandom)
    {
        float angle = (float)(seedRandom.NextDouble() * 2 * Mathf.PI);
        float distance = (float)(seedRandom.NextDouble() * group.clusterRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

        Vector3 candidatePos = new Vector3(clusterCenter.x + offset.x, clusterCenter.y, clusterCenter.z + offset.y);

        Vector3 groundPos = terrain.FindGroundPosition(new Vector2(candidatePos.x, candidatePos.z));
        if (groundPos == Vector3.zero)
            return;

        // Validate position
        if (groundPos.y < 0f)
            return;

        if (terrain.CalculateSlopeAtPosition(groundPos) > maxSlopeAngle)
            return;

        // Check distance to other props
        bool tooClose = false;
        foreach (var p in placedPositions)
        {
            if (Vector3.Distance(p, groundPos) < minDistanceBetweenProps)
            {
                tooClose = true;
                break;
            }
        }

        if (tooClose)
            return;

        // Create prop data
        PropData propData = new PropData
        {
            position = groundPos,
            rotation = (float)(seedRandom.NextDouble() * 360f),
            scale = (float)(0.8f + seedRandom.NextDouble() * 0.4f),
            prefabIndex = seedRandom.Next(0, group.prefabs.Length),
            groupIndex = groupIndex
        };

        propsToInstantiate.Add(propData);
        placedPositions.Add(groundPos);
    }

    /// <summary>
    /// Instantiate props in batches on main thread (called by LoadingManager)
    /// </summary>
    public IEnumerator InstantiatePropsInBatches()
    {
        if (propsContainer == null)
        {
            propsContainer = new GameObject("Props");
            propsContainer.transform.SetParent(transform);
        }

        int batchesProcessed = 0;
        int propsProcessed = 0;

        for (int i = 0; i < propsToInstantiate.Count; i += propsPerFrame)
        {
            int batchEnd = Mathf.Min(i + propsPerFrame, propsToInstantiate.Count);

            for (int j = i; j < batchEnd; j++)
            {
                InstantiateProp(propsToInstantiate[j]);
                propsProcessed++;
            }

            batchesProcessed++;
            yield return null; // Wait one frame between batches
        }

        Debug.Log($"Instantiated {propsProcessed} props in {batchesProcessed} batches");
    }

    private void InstantiateProp(PropData propData)
    {
        PropGroup group = propGroups[propData.groupIndex];
        GameObject prefab = group.prefabs[propData.prefabIndex];

        GameObject prop = Instantiate(prefab, propData.position, Quaternion.identity);
        prop.transform.SetParent(propsContainer.transform);
        prop.transform.rotation = Quaternion.Euler(0, propData.rotation, 0);
        prop.transform.localScale = Vector3.one * propData.scale;

        EnsureCollider(prop);
    }

    private void TryPlaceCluster(PropGroup group)
    {
        Vector2 center2D = new Vector2(terrain.terrainSize / 2f, terrain.terrainSize / 2f);
        Vector2 randomDir = Random.insideUnitCircle * spawnRadius;
        Vector2 spawnXZ = center2D + randomDir;

        Vector3 clusterCenter = terrain.FindGroundPosition(spawnXZ);
        if (clusterCenter == Vector3.zero)
            return;

        if (terrain.CalculateSlopeAtPosition(clusterCenter) > maxSlopeAngle)
            return;

        int count = Random.Range(group.minPropsInCluster, group.maxPropsInCluster);

        for (int i = 0; i < count; i++)
        {
            PlaceSinglePropInCluster(group, clusterCenter);
        }
    }

    private void PlaceSinglePropInCluster(PropGroup group, Vector3 clusterCenter)
    {
        Vector2 offset = Random.insideUnitCircle * group.clusterRadius;
        Vector3 candidatePos = new Vector3(clusterCenter.x + offset.x, clusterCenter.y, clusterCenter.z + offset.y);

        Vector3 groundPos = terrain.FindGroundPosition(new Vector2(candidatePos.x, candidatePos.z));
        if (groundPos == Vector3.zero)
            return;

        if (!IsValidPropPosition(groundPos))
            return;

        GameObject prefab = group.prefabs[Random.Range(0, group.prefabs.Length)];

        GameObject prop = Instantiate(prefab, groundPos, Quaternion.identity);
        prop.transform.parent = transform;

        prop.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        float scale = Random.Range(0.8f, 1.2f);
        prop.transform.localScale = Vector3.one * scale;

        EnsureCollider(prop);
        placedPositions.Add(groundPos);
        allProps.Add(prop);
    }

    private bool IsValidPropPosition(Vector3 pos)
    {
        if (pos.y < 0f) return false;

        float slope = terrain.CalculateSlopeAtPosition(pos);
        if (slope > maxSlopeAngle) return false;

        foreach (var p in placedPositions)
        {
            if (Vector3.Distance(p, pos) < minDistanceBetweenProps)
                return false;
        }

        return true;
    }

    private void EnsureCollider(GameObject prop)
    {
        Collider col = prop.GetComponentInChildren<Collider>();
        if (col == null)
        {
            MeshFilter mf = prop.GetComponentInChildren<MeshFilter>();
            if (mf != null)
            {
                MeshCollider meshCol = prop.AddComponent<MeshCollider>();
                meshCol.sharedMesh = mf.sharedMesh;
                meshCol.convex = true;
                meshCol.isTrigger = false;
            }
        }
        else col.isTrigger = false;
    }
}
