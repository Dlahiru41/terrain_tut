using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("NavMesh / Accessibility")]
    [Tooltip("If enabled, the spawner will only place artifacts that are reachable by the player via the NavMesh.")]
    public bool requireNavMeshAccess;
    [Tooltip("Optional: Assign a Player Transform to test reachability from. If empty, the spawner will try to find a GameObject tagged 'Player'.")]
    public Transform playerTransform;

    [Header("Path Visualization")]
    [Tooltip("Show a LineRenderer path from player to each placed artifact when placement succeeds.")]
    public bool showPathVisualization;
    public Material pathMaterial;
    public Color pathColor = Color.green;
    public float pathWidth = 0.12f;
    [Tooltip("How far to search for a NavMesh position around a candidate artifact position (meters)")]
    public float navMeshSampleDistance = 2.0f;

    private TerrainData _terrainData;
    private Vector3 _terrainPos;
    private Vector3 _terrainSize;
    // Cache property ID for shader to avoid string lookups
    private static readonly int GlossinessPropId = Shader.PropertyToID("_Glossiness");

    // Cached nav info to allow toggling / refreshing of path visualizations after spawn
    private bool _playerNavAvailableCached;
    private Vector3 _playerNavPositionCached;

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
                    case 0: a.typeName = "Health Potion"; a.primitive = PrimitiveType.Sphere; a.color = new Color(1f, 0.1f, 0.1f); a.scale = Vector3.one * 0.4f; break;
                    case 1: a.typeName = "Coin"; a.primitive = PrimitiveType.Cylinder; a.color = new Color(1f, 0.84f, 0f); a.scale = new Vector3(0.2f, 0.02f, 0.2f); break;
                    case 2: a.typeName = "Weapon"; a.primitive = PrimitiveType.Capsule; a.color = new Color(0.7f, 0.7f, 0.8f); a.scale = new Vector3(0.2f, 0.8f, 0.2f); break;
                    case 3: a.typeName = "Magic Pickup"; a.primitive = PrimitiveType.Sphere; a.color = new Color(0.7f, 0.2f, 1f); a.scale = Vector3.one * 0.5f; break;
                    case 4: a.typeName = "Shield"; a.primitive = PrimitiveType.Cylinder; a.color = new Color(0.4f, 0.6f, 1f); a.scale = new Vector3(0.6f, 0.05f, 0.6f); break;
                    case 5: a.typeName = "Trap"; a.primitive = PrimitiveType.Cube; a.color = new Color(0.4f, 0.2f, 0.1f); a.scale = new Vector3(0.6f, 0.2f, 0.6f); break;
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

        // Determine player / nav start if required
        bool playerNavAvailable = false;
        Vector3 playerNavPosition = Vector3.zero;
        GameObject playerGo;
        if (requireNavMeshAccess)
        {
            if (playerTransform != null)
                playerGo = playerTransform.gameObject;
            else
            {
                playerGo = GameObject.FindWithTag("Player");
                if (playerGo == null)
                    playerGo = GameObject.Find("Player");
            }

            if (playerGo != null)
            {
                NavMeshHit playerHit;
                if (NavMesh.SamplePosition(playerGo.transform.position, out playerHit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    playerNavAvailable = true;
                    playerNavPosition = playerHit.position;
                    // cache for later visualization toggles
                    _playerNavAvailableCached = true;
                    _playerNavPositionCached = playerHit.position;
                }
                else
                {
                    Debug.LogWarning("ArtifactSpawner: Player is not positioned on the NavMesh. Nav-based reachability checks will be skipped.");
                    _playerNavAvailableCached = false;
                }
            }
            else
            {
                Debug.LogWarning("ArtifactSpawner: No Player Transform found (tagged 'Player' or named 'Player'); Nav-based reachability checks will be skipped.");
                _playerNavAvailableCached = false;
            }
        }
        else
        {
            _playerNavAvailableCached = false;
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

                // Enforce spacing against all placed positions
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

                // If required, verify the candidate is on the NavMesh and reachable from the player
                if (requireNavMeshAccess && playerNavAvailable)
                {
                    NavMeshHit targetHit;
                    if (!NavMesh.SamplePosition(worldPos, out targetHit, navMeshSampleDistance, NavMesh.AllAreas))
                    {
                        // No NavMesh nearby, skip
                        continue;
                    }

                    NavMeshPath path = new NavMeshPath();
                    bool pathFound = NavMesh.CalculatePath(playerNavPosition, targetHit.position, NavMesh.AllAreas, path);
                    if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
                    {
                        // Not reachable, skip this candidate
                        continue;
                    }

                    // Use the sampled target position (snapped to NavMesh) as the placement anchor
                    worldPos = targetHit.position;
                }

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

                // Color material (create enhanced material instance with better visuals)
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = a.color;
                    
                    // Enhance materials based on artifact type
                    switch (i)
                    {
                        case 0: // Health Potion - glowing red
                            mat.SetFloat(GlossinessPropId, 0.9f);
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", a.color * 0.3f);
                            break;
                        case 1: // Coin - shiny metallic gold
                            mat.SetFloat(GlossinessPropId, 0.95f);
                            mat.SetFloat("_Metallic", 0.8f);
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", a.color * 0.2f);
                            break;
                        case 2: // Weapon - metallic
                            mat.SetFloat(GlossinessPropId, 0.7f);
                            mat.SetFloat("_Metallic", 0.9f);
                            break;
                        case 3: // Magic Pickup - glowing purple
                            mat.SetFloat(GlossinessPropId, 0.85f);
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", a.color * 0.5f);
                            break;
                        case 4: // Shield - semi-metallic
                            mat.SetFloat(GlossinessPropId, 0.7f);
                            mat.SetFloat("_Metallic", 0.6f);
                            break;
                        case 5: // Trap - dull/rough
                            mat.SetFloat(GlossinessPropId, 0.2f);
                            mat.SetFloat("_Metallic", 0.1f);
                            break;
                        default:
                            mat.SetFloat(GlossinessPropId, 0.6f);
                            break;
                    }
                    
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

                // Optionally create a LineRenderer visualization for the path (only if we validated the NavMesh path)
                if (showPathVisualization && requireNavMeshAccess && playerNavAvailable)
                {
                    // Calculate path again for visualization
                    NavMeshPath vizPath = new NavMeshPath();
                    if (NavMesh.CalculatePath(playerNavPosition, go.transform.position, NavMesh.AllAreas, vizPath) && vizPath.status == NavMeshPathStatus.PathComplete)
                    {
                        CreatePathVisualization(go, vizPath);
                    }
                }

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

    [ContextMenu("Refresh Path Visualizations")]
    public void RefreshPathVisualizations()
    {
        // Determine player nav position if needed
        if (requireNavMeshAccess)
        {
            if (playerTransform != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(playerTransform.position, out hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    _playerNavAvailableCached = true;
                    _playerNavPositionCached = hit.position;
                }
                else
                {
                    _playerNavAvailableCached = false;
                }
            }
            else
            {
                var player = GameObject.FindWithTag("Player");
                if (player == null) player = GameObject.Find("Player");
                if (player != null)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(player.transform.position, out hit, navMeshSampleDistance, NavMesh.AllAreas))
                    {
                        _playerNavAvailableCached = true;
                        _playerNavPositionCached = hit.position;
                    }
                    else _playerNavAvailableCached = false;
                }
                else _playerNavAvailableCached = false;
            }
        }
        else
        {
            _playerNavAvailableCached = false;
        }

        // Iterate spawned artifacts and add/remove/update visualization depending on showPathVisualization
        foreach (var a in artifactTypes)
        {
            if (a == null || a.spawned == null) continue;
            foreach (var go in a.spawned)
            {
                if (go == null) continue;
                // find existing PathViz child
                Transform existing = go.transform.Find("PathViz");
                if (showPathVisualization && _playerNavAvailableCached)
                {
                    // compute path and either create or update
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(_playerNavPositionCached, go.transform.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        // remove old
                        if (existing != null) Object.DestroyImmediate(existing.gameObject);
                        CreatePathVisualization(go, path);
                    }
                    else
                    {
                        // remove any existing visualization if path no longer valid
                        if (existing != null) Object.DestroyImmediate(existing.gameObject);
                    }
                }
                else
                {
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);
                }
            }
        }
    }

    [ContextMenu("Clear Path Visualizations")] 
    public void ClearPathVisualizations()
    {
        foreach (var a in artifactTypes)
        {
            if (a == null || a.spawned == null) continue;
            foreach (var go in a.spawned)
            {
                if (go == null) continue;
                Transform existing = go.transform.Find("PathViz");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }
        }
    }

    // Create a LineRenderer on the artifact to visualize the nav path (childed to artifact)
    void CreatePathVisualization(GameObject artifact, NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length == 0) return;

        // Ensure any existing visualization is removed first
        var existing = artifact.transform.Find("PathViz");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject lrObj = new GameObject("PathViz");
        lrObj.transform.SetParent(artifact.transform, false);
        var lr = lrObj.AddComponent<LineRenderer>();
        lr.positionCount = path.corners.Length;
        lr.SetPositions(path.corners);
        lr.widthMultiplier = Mathf.Max(0.01f, pathWidth);
        lr.material = pathMaterial != null ? pathMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = pathColor;
        lr.endColor = pathColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 2;
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
