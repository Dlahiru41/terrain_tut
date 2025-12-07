using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool for setting up proper terrain and object scaling
/// </summary>
public class TerrainScaleSetupWindow : EditorWindow
{
    private Terrain terrain;
    private GameObject player;
    private ArtifactSpawner artifactSpawner;
    private TerrainScaleManager scaleManager;

    private float terrainWidth = 200f;
    private float terrainLength = 200f;
    private float terrainHeight = 50f;
    private float playerScale = 1.0f;

    [MenuItem("Tools/Setup Terrain Scaling")]
    public static void ShowWindow()
    {
        TerrainScaleSetupWindow window = GetWindow<TerrainScaleSetupWindow>("Terrain Scale Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    void OnEnable()
    {
        AutoDetectComponents();
    }

    void AutoDetectComponents()
    {
        // Try to find terrain
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
        }

        // Try to find player
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
        }

        // Try to find artifact spawner
        if (artifactSpawner == null && terrain != null)
        {
            artifactSpawner = terrain.GetComponent<ArtifactSpawner>();
        }

        // Try to find scale manager
        if (scaleManager == null && terrain != null)
        {
            scaleManager = terrain.GetComponent<TerrainScaleManager>();
        }

        // Load current terrain size if available
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 size = terrain.terrainData.size;
            terrainWidth = size.x;
            terrainLength = size.z;
            terrainHeight = size.y;
        }

        // Get player scale if available
        if (player != null)
        {
            // Use the maximum scale component to handle non-uniform scaling
            Vector3 scale = player.transform.localScale;
            playerScale = Mathf.Max(scale.x, scale.y, scale.z);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Terrain & Object Scale Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool helps you set up proper relative scaling between your terrain, player, and artifacts. " +
            "A common issue is when terrain is too large, making the player take forever to traverse it.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Component Detection Section
        GUILayout.Label("1. Detect Scene Components", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        EditorGUILayout.ObjectField("Player", player, typeof(GameObject), true);
        EditorGUILayout.ObjectField("Artifact Spawner", artifactSpawner, typeof(ArtifactSpawner), true);
        EditorGUILayout.ObjectField("Scale Manager", scaleManager, typeof(TerrainScaleManager), true);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Refresh / Auto-Detect"))
        {
            AutoDetectComponents();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Scale Configuration Section
        GUILayout.Label("2. Configure Terrain Scale", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Current Terrain Size:", EditorStyles.miniLabel);
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 currentSize = terrain.terrainData.size;
            EditorGUILayout.LabelField($"  Width: {currentSize.x}, Height: {currentSize.y}, Length: {currentSize.z}");
        }
        else
        {
            EditorGUILayout.LabelField("  No terrain detected", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        terrainWidth = EditorGUILayout.Slider("Terrain Width (X)", terrainWidth, 50f, 2000f);
        terrainLength = EditorGUILayout.Slider("Terrain Length (Z)", terrainLength, 50f, 2000f);
        terrainHeight = EditorGUILayout.Slider("Terrain Height (Y)", terrainHeight, 10f, 500f);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Small (100x30x100)"))
        {
            terrainWidth = 100f;
            terrainLength = 100f;
            terrainHeight = 30f;
        }
        if (GUILayout.Button("Medium (200x50x200)"))
        {
            terrainWidth = 200f;
            terrainLength = 200f;
            terrainHeight = 50f;
        }
        if (GUILayout.Button("Large (500x100x500)"))
        {
            terrainWidth = 500f;
            terrainLength = 500f;
            terrainHeight = 100f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Player Scale Section
        GUILayout.Label("3. Configure Player Scale", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Current Player Scale:", EditorStyles.miniLabel);
        if (player != null)
        {
            Vector3 scale = player.transform.localScale;
            EditorGUILayout.LabelField($"  Scale: X={scale.x:F2}, Y={scale.y:F2}, Z={scale.z:F2}");
        }
        else
        {
            EditorGUILayout.LabelField("  No player detected", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        playerScale = EditorGUILayout.Slider("Player Scale", playerScale, 0.5f, 3.0f);

        EditorGUILayout.HelpBox(
            "Recommended: Keep player at 1.0-2.0 scale (roughly human height). " +
            "Adjust terrain size rather than player size for best results.",
            MessageType.Info);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Apply Section
        GUILayout.Label("4. Apply Changes", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GUILayout.Button("Apply Terrain Scale", GUILayout.Height(30)))
        {
            ApplyTerrainScale();
        }

        if (GUILayout.Button("Apply Player Scale", GUILayout.Height(30)))
        {
            ApplyPlayerScale();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply All & Setup Scene", GUILayout.Height(40)))
        {
            ApplyAllAndSetup();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Recommendations
        GUILayout.Label("Recommendations", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("• For quick traversal: Use 100-200 unit terrain", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• For exploration: Use 300-500 unit terrain", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• Keep player scale at 1.0-2.0 for consistency", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• Artifact spacing adjusts automatically", EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.EndVertical();
    }

    void ApplyTerrainScale()
    {
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Error", "No terrain found. Please ensure a terrain exists in the scene.", "OK");
            return;
        }

        Undo.RecordObject(terrain.terrainData, "Change Terrain Scale");
        terrain.terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        // Ensure TerrainScaleManager component exists
        if (scaleManager == null)
        {
            scaleManager = terrain.gameObject.AddComponent<TerrainScaleManager>();
            Undo.RegisterCreatedObjectUndo(scaleManager, "Add TerrainScaleManager");
        }

        // Update scale manager values
        scaleManager.terrainWidth = terrainWidth;
        scaleManager.terrainLength = terrainLength;
        scaleManager.terrainHeight = terrainHeight;

        EditorUtility.SetDirty(terrain.terrainData);
        EditorUtility.SetDirty(scaleManager);

        Debug.Log($"Terrain scale applied: {terrainWidth} x {terrainHeight} x {terrainLength}");
        
        EditorUtility.DisplayDialog("Success", 
            $"Terrain scale updated to {terrainWidth} x {terrainHeight} x {terrainLength}.\n\n" +
            "You may want to regenerate the terrain and respawn artifacts for best results.", 
            "OK");
    }

    void ApplyPlayerScale()
    {
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", "No player found. Please ensure a Player GameObject exists and is tagged 'Player'.", "OK");
            return;
        }

        Undo.RecordObject(player.transform, "Change Player Scale");
        player.transform.localScale = Vector3.one * playerScale;

        EditorUtility.SetDirty(player);

        Debug.Log($"Player scale applied: {playerScale}");
        EditorUtility.DisplayDialog("Success", $"Player scale updated to {playerScale}", "OK");
    }

    void ApplyAllAndSetup()
    {
        bool success = true;

        // Apply terrain scale
        if (terrain != null && terrain.terrainData != null)
        {
            ApplyTerrainScale();
        }
        else
        {
            success = false;
            Debug.LogWarning("Terrain not found - skipping terrain scale");
        }

        // Apply player scale
        if (player != null)
        {
            ApplyPlayerScale();
        }
        else
        {
            Debug.LogWarning("Player not found - skipping player scale");
        }

        // Refresh artifact spawner cache
        if (artifactSpawner != null)
        {
            artifactSpawner.CacheTerrain();
            Debug.Log("Artifact spawner cache refreshed");
        }

        if (success)
        {
            EditorUtility.DisplayDialog("Setup Complete", 
                "Terrain and object scaling has been configured!\n\n" +
                "Next steps:\n" +
                "1. Regenerate terrain (press R in play mode)\n" +
                "2. Respawn artifacts (right-click ArtifactSpawner)\n" +
                "3. Test gameplay and adjust as needed", 
                "OK");
        }
    }
}
