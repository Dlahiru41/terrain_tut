#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArtifactSpawner))]
public class ArtifactSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArtifactSpawner spawner = (ArtifactSpawner)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn Artifacts Now"))
        {
            // This will call the public context menu wrapper
            spawner.SpawnArtifactsContextMenu();
            // Mark scene dirty so changes persist
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            }
        }

        if (GUILayout.Button("Clear Spawned Artifacts"))
        {
            spawner.ClearSpawnedArtifacts();
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Select Spawned Artifacts"))
        {
            // Select all spawned children for quick inspection
            var gos = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < spawner.transform.childCount; i++)
            {
                var child = spawner.transform.GetChild(i);
                if (child.name.StartsWith("Artifact_"))
                    gos.Add(child.gameObject);
            }
            Selection.objects = gos.ToArray();
        }
    }
}
#endif

