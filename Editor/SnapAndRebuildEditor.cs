#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SnapAndRebuildEditor
{
    [MenuItem("Tools/Snap Player to Terrain and Rebuild NavMesh")]
    static void SnapSelectedAndRebuild()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player");

        if (target == null)
        {
            Debug.LogWarning("[SnapAndRebuildEditor] No GameObject selected and no GameObject with tag 'Player' found.");
            return;
        }

        var snapComp = target.GetComponent<SnapToTerrain>();
        if (snapComp == null)
        {
            // Temporarily add component to use its logic
            snapComp = target.AddComponent<SnapToTerrain>();
            // Ensure we don't rebuild twice here; let this menu option control the rebuild.
            snapComp.rebuildNavMeshOnStart = false;
            snapComp.warpAgent = false;
            snapComp.groundOffset = 0f;
            snapComp.Snap();
            Object.DestroyImmediate(snapComp);
        }
        else
        {
            snapComp.Snap();
        }

        // Rebuild all NavMeshSurface components
        var surfaces = UnityEngine.Object.FindObjectsOfType<UnityEngine.AI.NavMeshSurface>();
        if (surfaces == null || surfaces.Length == 0)
        {
            Debug.Log("[SnapAndRebuildEditor] No NavMeshSurface components found in scene.");
            return;
        }

        foreach (var s in surfaces)
        {
            s.BuildNavMesh();
        }

        Debug.Log("[SnapAndRebuildEditor] Snapped '" + target.name + "' to terrain and rebuilt " + surfaces.Length + " NavMeshSurface(s).");
    }
}
#endif

