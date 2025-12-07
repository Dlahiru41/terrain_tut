using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class SnapPlayerToTerrain : MonoBehaviour
{
    public Terrain terrain;
    public bool preferNavMesh = true;
    public float navMeshSampleDistance = 5f;

    // Snap this GameObject to the terrain or NavMesh
    [ContextMenu("Snap This Player To Terrain")]
    public void SnapThisToTerrain()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
            if (terrain == null)
                terrain = FindObjectOfType<Terrain>();
        }

        if (terrain == null)
        {
            Debug.LogWarning("SnapPlayerToTerrain: No Terrain found in scene.");
            return;
        }

        Vector3 pos = transform.position;

        if (preferNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                pos = hit.position;
                float offset = ComputeVerticalOffset();
                pos.y += offset;
                transform.position = pos;
                Debug.Log($"Snapped '{gameObject.name}' to NavMesh at {pos}");
                return;
            }
        }

        float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
        pos.y = terrainY + ComputeVerticalOffset();
        transform.position = pos;
        Debug.Log($"Snapped '{gameObject.name}' to terrain at {pos}");
    }

    float ComputeVerticalOffset()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        Collider col = GetComponentInChildren<Collider>();

        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        if (rend != null) bounds = rend.bounds;
        else if (col != null) bounds = col.bounds;

        if (bounds.size.y > 0.001f) return bounds.extents.y;
        return 0f;
    }
}

