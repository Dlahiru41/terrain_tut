#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor.SceneManagement;

namespace Editor
{
    public static class PlayerSnapToTerrainEditor
    {
        // Snap selected GameObjects to Terrain surface (Ctrl+Shift+T)
        [MenuItem("Tools/Snap Selected to Terrain %#t")]
        public static void SnapSelectedToTerrainMenu()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Snap To Terrain", "No GameObject selected.", "OK");
                return;
            }

            foreach (var go in Selection.gameObjects)
            {
                SnapGameObjectToTerrain(go, useNavMesh: false);
            }
        }

        // Snap selected GameObjects to nearest NavMesh position if available (Ctrl+Shift+N)
        [MenuItem("Tools/Snap Selected to NavMesh %#n")]
        public static void SnapSelectedToNavMeshMenu()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Snap To NavMesh", "No GameObject selected.", "OK");
                return;
            }

            foreach (var go in Selection.gameObjects)
            {
                SnapGameObjectToTerrain(go, useNavMesh: true);
            }
        }

        // Also add as a GameObject menu option when right-clicking hierarchy
        [MenuItem("GameObject/Snap To Terrain", false, 0)]
        public static void SnapSelectedToTerrainGameObjectMenu()
        {
            if (Selection.activeGameObject != null)
                SnapGameObjectToTerrain(Selection.activeGameObject, useNavMesh: false);
        }

        static void SnapGameObjectToTerrain(GameObject go, bool useNavMesh)
        {
            if (go == null) return;

            // Find a Terrain in the scene
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                terrain = Object.FindObjectOfType<Terrain>();

            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Snap To Terrain", "No Terrain found in the scene.", "OK");
                return;
            }

            // Record undo for transform change
            Undo.RecordObject(go.transform, "Snap To Terrain");

            Vector3 worldPos = go.transform.position;

            // First try NavMesh snapping if requested
            if (useNavMesh)
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(worldPos, out navHit, 5f, NavMesh.AllAreas))
                {
                    worldPos = navHit.position;
                    // Apply vertical offset so object sits on surface
                    float yOffset = ComputeVerticalOffset(go, worldPos);
                    worldPos.y += yOffset;

                    go.transform.position = worldPos;
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    Debug.Log($"Snapped '{go.name}' to NavMesh at {worldPos}");
                    return;
                }
                else
                {
                    Debug.LogWarning($"No NavMesh sample found near '{go.name}'; falling back to terrain height.");
                }
            }

            // Fall back to terrain sample height
            float terrainHeight = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
            worldPos.y = terrainHeight;

            // Add offset so the object sits on the surface instead of intersecting
            float offset = ComputeVerticalOffset(go, worldPos);
            worldPos.y += offset;

            go.transform.position = worldPos;
            EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log($"Snapped '{go.name}' to terrain at {worldPos}");
        }

        // Compute a vertical offset based on renderer/collider bounds so object sits above the terrain
        static float ComputeVerticalOffset(GameObject go, Vector3 sampledPosition)
        {
            // Use renderer bounds if available
            Renderer rend = go.GetComponentInChildren<Renderer>();
            Collider col = go.GetComponentInChildren<Collider>();

            Bounds bounds = new Bounds(sampledPosition, Vector3.zero);
            if (rend != null)
                bounds = rend.bounds;
            else if (col != null)
                bounds = col.bounds;

            if (bounds.size.y > 0.001f)
                return bounds.extents.y;

            return 0f;
        }
    }
}
#endif
