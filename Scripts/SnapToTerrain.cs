using UnityEngine;
using UnityEngine.AI;

public class SnapToTerrain : MonoBehaviour
{
    // ...existing code...
    [Tooltip("Vertical offset above the sampled terrain height.")]
    public float groundOffset = 0.0f;

    [Tooltip("If true, will find all NavMeshSurface components in the scene and rebuild them on Start.")]
    public bool rebuildNavMeshOnStart = false;

    [Tooltip("If true and a NavMeshAgent is attached, the agent will be warped to the snap position to avoid path issues.")]
    public bool warpAgent = true;

    void Awake()
    {
        Snap();
    }

    void Start()
    {
        // ...existing code...
        if (rebuildNavMeshOnStart)
        {
            RebuildAllNavMeshSurfaces();
        }
    }

    void Snap()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("[SnapToTerrain] No active Terrain found in scene.");
            return;
        }

        Vector3 pos = transform.position;
        float terrainHeight = terrain.SampleHeight(pos) + terrain.transform.position.y;
        Vector3 target = new Vector3(pos.x, terrainHeight + groundOffset, pos.z);
        transform.position = target;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && warpAgent)
        {
            // Use Warp to avoid navmesh agent pathing glitches
            if (agent.isOnNavMesh)
                agent.Warp(target);
        }
    }

    void RebuildAllNavMeshSurfaces()
    {
        // This method uses NavMeshSurface from the NavMeshComponents package.
        // If you use a different navmesh workflow, call your bake method here.
        var surfaces = FindObjectsOfType<UnityEngine.AI.NavMeshSurface>();
        if (surfaces == null || surfaces.Length == 0)
        {
            Debug.Log("[SnapToTerrain] No NavMeshSurface components found to rebuild.");
            return;
        }

        foreach (var s in surfaces)
        {
            s.BuildNavMesh();
        }

        Debug.Log("[SnapToTerrain] Rebuilt " + surfaces.Length + " NavMeshSurface(s).");
    }
}

