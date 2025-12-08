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
        
        [Header("Spawn Constraints")]
        [Tooltip("Minimum terrain height (0-1) for this artifact type. -1 = no minimum.")]
        [Range(-1f, 1f)]
        public float minHeight = -1f;
        [Tooltip("Maximum terrain height (0-1) for this artifact type. -1 = no maximum.")]
        [Range(-1f, 1f)]
        public float maxHeight = -1f;
        [Tooltip("Maximum slope angle (degrees) for this artifact type. -1 = use global setting.")]
        [Range(-1f, 90f)]
        public float maxSlope = -1f;
        
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

    [Header("Scaling")]
    [Tooltip("Artifact scale relative to the terrain horizontal size. For example, 0.01 = 1% of terrain size.")]
    [Min(0.00001f)]
    public float scaleFactor = 0.01f; // 1% of terrain size

    [Header("NavMesh / Accessibility")]
    [Tooltip("If enabled, the spawner will only place artifacts that are reachable by the player via the NavMesh.")]
    public bool requireNavMeshAccess;
    [Tooltip("Optional: Assign a Player Transform to test reachability from. If empty, the spawner will try to find a GameObject tagged 'Player'.")]
    public Transform playerTransform;

    [Header("Path Visualization")]
    [Tooltip("Show a LineRenderer path from player to each placed artifact when placement succeeds.")]
    public bool showPathVisualization;
    public Material pathMaterial;
    public Color successPathColor = Color.green;
    public Color failedPathColor = Color.red;
    public float pathWidth = 0.12f;
    [Tooltip("How far to search for a NavMesh position around a candidate artifact position (meters)")]
    public float navMeshSampleDistance = 2.0f;
    [Tooltip("Keyboard key to toggle path visualization on/off")]
    public KeyCode togglePathVisualizationKey = KeyCode.P;
    [Tooltip("Distance between interpolated points on the path (smaller = smoother path, larger = better performance)")]
    [Range(0.1f, 2.0f)]
    public float pathSegmentLength = 0.5f;
    [Tooltip("Height offset above terrain for path visualization (prevents z-fighting with terrain)")]
    [Range(0.05f, 1.0f)]
    public float pathHeightOffset = 0.2f;

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

    void Update()
    {
        if (!Application.isPlaying) return;
        
        // Toggle path visualization with keyboard shortcut
        if (Input.GetKeyDown(togglePathVisualizationKey))
        {
            showPathVisualization = !showPathVisualization;
            RefreshPathVisualizations();
            Debug.Log($"ArtifactSpawner: Path visualization toggled {(showPathVisualization ? "ON" : "OFF")}");
        }
    }

    void OnValidate()
    {
        // Keep references up-to-date in editor
        CacheTerrain();
        EnsureDefaults();
    }

    public void CacheTerrain()
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
                    case 0: // Health Potion - gentle slopes, mid-low elevation
                        a.typeName = "Health Potion"; 
                        a.primitive = PrimitiveType.Sphere; 
                        a.color = new Color(1f, 0.1f, 0.1f); 
                        a.scale = Vector3.one * 0.4f;
                        a.minHeight = 0.1f;
                        a.maxHeight = 0.6f;
                        a.maxSlope = 30f; // gentle slopes only
                        break;
                    case 1: // Coin - flat terrain, low-mid elevation
                        a.typeName = "Coin"; 
                        a.primitive = PrimitiveType.Cylinder; 
                        a.color = new Color(1f, 0.84f, 0f); 
                        a.scale = new Vector3(0.2f, 0.02f, 0.2f);
                        a.minHeight = 0.05f;
                        a.maxHeight = 0.5f;
                        a.maxSlope = 20f; // very flat areas
                        break;
                    case 2: // Weapon - any elevation, moderate slopes
                        a.typeName = "Weapon"; 
                        a.primitive = PrimitiveType.Capsule; 
                        a.color = new Color(0.7f, 0.7f, 0.8f); 
                        a.scale = new Vector3(0.2f, 0.8f, 0.2f);
                        a.minHeight = -1f; // no minimum
                        a.maxHeight = -1f; // no maximum
                        a.maxSlope = 35f;
                        break;
                    case 3: // Magic Pickup - mid-high elevation, gentle slopes
                        a.typeName = "Magic Pickup"; 
                        a.primitive = PrimitiveType.Sphere; 
                        a.color = new Color(0.7f, 0.2f, 1f); 
                        a.scale = Vector3.one * 0.5f;
                        a.minHeight = 0.4f;
                        a.maxHeight = 0.9f;
                        a.maxSlope = 25f;
                        break;
                    case 4: // Shield - low-mid elevation, flat areas
                        a.typeName = "Shield"; 
                        a.primitive = PrimitiveType.Cylinder; 
                        a.color = new Color(0.4f, 0.6f, 1f); 
                        a.scale = new Vector3(0.6f, 0.05f, 0.6f);
                        a.minHeight = 0.1f;
                        a.maxHeight = 0.5f;
                        a.maxSlope = 15f; // very flat
                        break;
                    case 5: // Trap - any location, any slope (dangerous)
                        a.typeName = "Trap"; 
                        a.primitive = PrimitiveType.Cube; 
                        a.color = new Color(0.4f, 0.2f, 0.1f); 
                        a.scale = new Vector3(0.6f, 0.2f, 0.6f);
                        a.minHeight = -1f;
                        a.maxHeight = -1f;
                        a.maxSlope = -1f; // use global setting
                        break;
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

                // Check height constraints first (cheapest check)
                float worldHeight = _terrainData.GetInterpolatedHeight(nx, nz);
                float normalizedHeight = worldHeight / _terrainSize.y; // Normalize to 0-1 range
                if (a.minHeight >= 0 && normalizedHeight < a.minHeight)
                    continue; // below minimum height
                if (a.maxHeight >= 0 && normalizedHeight > a.maxHeight)
                    continue; // above maximum height

                // Check slope constraint
                float steepness = _terrainData.GetSteepness(nx, nz);
                float maxSlopeForType = (a.maxSlope >= 0) ? a.maxSlope : allowedMaxSteepness;
                if (steepness > maxSlopeForType)
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

                // Compute scale relative to terrain horizontal size.
                // Use the larger of X/Z so artifacts remain proportionate on rectangular terrains.
                float terrainHorizontalSize = Mathf.Max(0.00001f, Mathf.Max(_terrainSize.x, _terrainSize.z));
                Vector3 finalScale = a.scale * terrainHorizontalSize * scaleFactor;

                // Apply scale first, then position the artifact so its pivot sits on the terrain correctly
                go.transform.localScale = finalScale;
                // position pivot so object sits on terrain properly (approximate using bounds)
                // move up by half the object's local Y size to avoid clipping
                float yOffset = finalScale.y * 0.5f;
                go.transform.position = worldPos + new Vector3(0f, yOffset, 0f);

                float rotY = (prng != null) ? (float)(prng.NextDouble() * 360.0) : Random.Range(0f, 360f);
                go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                // Color material (create enhanced material instance with better visuals)
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = a.color;
                    
                    // Enhance materials based on artifact type
                   switch (typeIndex)
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
                        CreatePathVisualization(go, vizPath, true);
                    }
                }

                placedPositions.Add(go.transform.position);
                a.spawned.Add(go);
                placed++;
                
#if UNITY_EDITOR || DEBUG
                // Log successful placement with details (only in debug builds)
                string accessibilityInfo = (requireNavMeshAccess && playerNavAvailable) ? "Accessible" : "Not validated";
                Debug.Log($"ArtifactSpawner: {a.typeName} #{placed} placed at ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1}) - {accessibilityInfo}");
#endif
            }

            if (a.spawned.Count < a.minCount)
            {
                Debug.LogWarning($"ArtifactSpawner: Could only place {a.spawned.Count}/{a.minCount} of {a.typeName} after {attempts} attempts.");
            }
#if UNITY_EDITOR || DEBUG
            else
            {
                Debug.Log($"ArtifactSpawner: Successfully placed {a.spawned.Count} {a.typeName}(s) (min: {a.minCount})");
            }
#endif
        }

        // Completed placing all artifact types
        int totalPlaced = 0;
        foreach (var a in artifactTypes)
        {
            if (a != null && a.spawned != null)
                totalPlaced += a.spawned.Count;
        }
#if UNITY_EDITOR || DEBUG
        Debug.Log($"ArtifactSpawner: Artifact spawning complete. Total artifacts placed: {totalPlaced} across {artifactTypes.Length} types.");
#endif
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
                        CreatePathVisualization(go, path, true);
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
    void CreatePathVisualization(GameObject artifact, NavMeshPath path, bool isSuccessPath = true)
    {
        if (path == null || path.corners == null || path.corners.Length == 0) return;

        // Ensure any existing visualization is removed first
        var existing = artifact.transform.Find("PathViz");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        // Terrain is required for proper path following
        if (terrain == null)
        {
            Debug.LogWarning("ArtifactSpawner: Terrain reference is null. Cannot create terrain-following path visualization.");
            return;
        }

        // Interpolate points along the path to follow terrain contours
        List<Vector3> finalPoints = new List<Vector3>();
        const float minDistanceThreshold = 0.01f; // Minimum distance to consider points as duplicates
        const float minDistanceSqr = minDistanceThreshold * minDistanceThreshold; // Use squared distance for performance
        
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            float distance = Vector3.Distance(start, end);
            int numSegments = Mathf.Max(1, Mathf.CeilToInt(distance / pathSegmentLength));
            
            // Add interpolated points along this segment (excluding the endpoint to avoid duplicates)
            for (int j = 0; j < numSegments; j++)
            {
                float t = j / (float)numSegments;
                Vector3 interpolatedPos = Vector3.Lerp(start, end, t);
                
                // Sample terrain height at this position
                float terrainHeight = terrain.SampleHeight(interpolatedPos) + _terrainPos.y;
                interpolatedPos.y = terrainHeight + pathHeightOffset;
                
                // Add point only if it's not too close to the previous point (optimization)
                if (finalPoints.Count == 0 || (interpolatedPos - finalPoints[finalPoints.Count - 1]).sqrMagnitude > minDistanceSqr)
                {
                    finalPoints.Add(interpolatedPos);
                }
            }
        }
        
        // Add the final corner point
        if (path.corners.Length > 0)
        {
            Vector3 lastCorner = path.corners[path.corners.Length - 1];
            float terrainHeight = terrain.SampleHeight(lastCorner) + _terrainPos.y;
            lastCorner.y = terrainHeight + pathHeightOffset;
            
            // Add only if not too close to the last added point
            if (finalPoints.Count == 0 || (lastCorner - finalPoints[finalPoints.Count - 1]).sqrMagnitude > minDistanceSqr)
            {
                finalPoints.Add(lastCorner);
            }
        }

        GameObject lrObj = new GameObject("PathViz");
        lrObj.transform.SetParent(artifact.transform, false);
        var lr = lrObj.AddComponent<LineRenderer>();
        lr.positionCount = finalPoints.Count;
        lr.SetPositions(finalPoints.ToArray());
        lr.widthMultiplier = Mathf.Max(0.01f, pathWidth);
        lr.material = pathMaterial != null ? pathMaterial : new Material(Shader.Find("Sprites/Default"));
        Color pathColorToUse = isSuccessPath ? successPathColor : failedPathColor;
        lr.startColor = pathColorToUse;
        lr.endColor = pathColorToUse;
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
