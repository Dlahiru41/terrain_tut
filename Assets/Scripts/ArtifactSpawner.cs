using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ArtifactSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ArtifactType
    {
        public string typeName = "Artifact";
        public PrimitiveType primitive = PrimitiveType.Sphere;
        public Color color = Color.white;
        public Vector3 scale = Vector3.one * 0.5f;
        public int minCount = 3;      // required minimum to spawn
        public int maxCount = 6;      // optional maximum
        public float minSpacing = 1f; // minimum distance between artifacts of any type
        [HideInInspector] public List<GameObject> spawned = new List<GameObject>();
    }

    [Header("Terrain Reference")]
    public Terrain terrain;

    [Header("Global Placement")]
    public int maxPlacementAttemptsPerItem = 200;
    public float allowedMaxSteepness = 30f; // degrees - avoid steep slopes

    [Header("Artifact Definitions (6 types required)")]
    public ArtifactType[] artifactTypes = new ArtifactType[6];

    [Header("Runtime Options")]
    public bool clearPreviousOnSpawn = true;

    [Header("Deterministic Options")]
    [Tooltip("When enabled, artifact placement will be deterministic using the provided seed.")]
    public bool useSeed;
    [Tooltip("Seed used for deterministic placement when Use Seed is enabled.")]
    public int seed;

    private TerrainData _terrainData;
    private Vector3 _terrainPos;
    private Vector3 _terrainSize;
    // Cache property ID for shader to avoid string lookups
    private static readonly int _glossinessPropId = Shader.PropertyToID("_Glossiness");

    void Awake()
    {
        CacheTerrain();
        EnsureDefaults();
    }

    void OnValidate()
    {
        // Keep references up-to-date in editor
        CacheTerrain();
        EnsureDefaults();
    }

    void CacheTerrain()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (terrain != null)
        {
            _terrainData = terrain.terrainData;
            _terrainPos = terrain.transform.position;
            _terrainSize = _terrainData.size;
        }
    }

    // Ensure there are 6 distinct default artifact types (useful when first added to a GameObject)
    void EnsureDefaults()
    {
        if (artifactTypes == null || artifactTypes.Length != 6)
        {
            artifactTypes = new ArtifactType[6];
        }

        // Provide sensible defaults for each slot if empty
        for (int i = 0; i < artifactTypes.Length; i++)
        {
            if (artifactTypes[i] == null)
                artifactTypes[i] = new ArtifactType();

            var a = artifactTypes[i];
            if (string.IsNullOrEmpty(a.typeName))
            {
                switch (i)
                {
                    case 0: a.typeName = "Health Potion"; a.primitive = PrimitiveType.Sphere; a.color = Color.red; a.scale = Vector3.one * 0.4f; break;
                    case 1: a.typeName = "Coin"; a.primitive = PrimitiveType.Cylinder; a.color = Color.yellow; a.scale = new Vector3(0.2f, 0.02f, 0.2f); break;
                    case 2: a.typeName = "Weapon"; a.primitive = PrimitiveType.Capsule; a.color = Color.gray; a.scale = new Vector3(0.2f, 0.8f, 0.2f); break;
                    case 3: a.typeName = "Magic Pickup"; a.primitive = PrimitiveType.Sphere; a.color = new Color(0.5f, 0f, 1f); a.scale = Vector3.one * 0.5f; break;
                    case 4: a.typeName = "Shield"; a.primitive = PrimitiveType.Cylinder; a.color = new Color(0.6f, 0.6f, 1f); a.scale = new Vector3(0.6f, 0.05f, 0.6f); break;
                    case 5: a.typeName = "Trap"; a.primitive = PrimitiveType.Cube; a.color = new Color(0.3f, 0.15f, 0.05f); a.scale = new Vector3(0.6f, 0.2f, 0.6f); break;
                }
            }

            // Clamp sensible values
            a.minCount = Mathf.Max(0, a.minCount);
            a.maxCount = Mathf.Max(a.minCount, a.maxCount);
            a.minSpacing = Mathf.Max(0.1f, a.minSpacing);
        }
    }

    [ContextMenu("Spawn Artifacts Now")]
    public void SpawnArtifactsContextMenu()
    {
        CacheTerrain();
        SpawnArtifacts();
    }

    public void SpawnArtifacts()
    {
        if (_terrainData == null)
        {
            Debug.LogWarning("ArtifactSpawner: No TerrainData found. Assign a Terrain in the inspector.");
            return;
        }

        // Optionally clear previous artifacts created by this spawner
        if (clearPreviousOnSpawn)
        {
            ClearSpawnedArtifacts();
        }

        List<Vector3> placedPositions = new List<Vector3>();

        for (int typeIndex = 0; typeIndex < artifactTypes.Length; typeIndex++)
        {
            var a = artifactTypes[typeIndex];
            int toPlace;
            if (useSeed)
            {
                // create a deterministic System.Random for deciding count per type using a combined seed
                var countPrng = new System.Random(seed + typeIndex * 101);
                toPlace = Mathf.Max(a.minCount, countPrng.Next(a.minCount, a.maxCount + 1));
            }
            else
            {
                toPlace = Mathf.Max(a.minCount, Random.Range(a.minCount, a.maxCount + 1));
            }
            a.spawned = new List<GameObject>();

            int placed = 0;
            int attempts = 0;
            System.Random prng = null;
            if (useSeed)
            {
                // Create a seeded PRNG for placement and rotation; combine base seed with type index so different types vary
                prng = new System.Random(seed + typeIndex * 1000 + 17);
            }
            while (placed < toPlace && attempts < toPlace * maxPlacementAttemptsPerItem)
            {
                attempts++;
                // Choose a random normalized position across terrain
                float nx = (prng != null) ? (float)prng.NextDouble() : Random.value;
                float nz = (prng != null) ? (float)prng.NextDouble() : Random.value;

                float steepness = _terrainData.GetSteepness(nx, nz);
                if (steepness > allowedMaxSteepness)
                    continue; // skip steep areas

                Vector3 worldPos = new Vector3(_terrainPos.x + nx * _terrainSize.x, 0f, _terrainPos.z + nz * _terrainSize.z);
                float height = terrain.SampleHeight(worldPos) + _terrainPos.y;
                worldPos.y = height;

                // enforce spacing against all placed positions
                bool tooClose = false;
                foreach (var p in placedPositions)
                {
                    if (Vector3.Distance(p, worldPos) < a.minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Create primitive visual object
                GameObject go = GameObject.CreatePrimitive(a.primitive);
                go.name = $"Artifact_{a.typeName}_{placed + 1}";
                go.transform.position = worldPos;

                // position pivot so object sits on terrain properly (approximate using bounds)
                // move up by half the object's local Y size to avoid clipping
                float yOffset = (a.scale.y * 0.5f);
                go.transform.position += new Vector3(0f, yOffset, 0f);

                go.transform.localScale = a.scale;
                float rotY = (prng != null) ? (float)(prng.NextDouble() * 360.0) : Random.Range(0f, 360f);
                go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                // Color material (create simple material instance)
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = a.color;
                    // make coins a bit shiny
                    mat.SetFloat(_glossinessPropId, 0.6f);
                    rend.sharedMaterial = mat;
                }

                // Ensure collider exists and is enabled (CreatePrimitive already attaches a collider)
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = false; // non-interactable physical collider
                }

                // Parent under spawner for organization
                go.transform.SetParent(this.transform, true);

                placedPositions.Add(go.transform.position);
                a.spawned.Add(go);
                placed++;
            }

            if (a.spawned.Count < a.minCount)
            {
                Debug.LogWarning($"ArtifactSpawner: Could only place {a.spawned.Count}/{a.minCount} of {a.typeName} after {attempts} attempts.");
            }
        }

        // Completed placing all artifact types
        Debug.Log($"ArtifactSpawner: Spawned artifacts. Total types: {artifactTypes.Length}.");
    }

    // Removes any spawned artifact GameObjects created by this spawner
    public void ClearSpawnedArtifacts()
    {
        for (int i = 0; i < artifactTypes.Length; i++)
        {
            var a = artifactTypes[i];
            if (a == null || a.spawned == null) continue;

            for (int j = a.spawned.Count - 1; j >= 0; j--)
            {
                var go = a.spawned[j];
                if (go == null) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(go);
                }
                else
                {
                    Object.Destroy(go);
                }
#else
                Object.Destroy(go);
#endif
            }

            a.spawned.Clear();
        }

        // Also destroy any direct children that match our naming convention (defensive)
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Artifact_"))
                    Object.DestroyImmediate(child.gameObject);
            }
        }
#endif
    }
}
