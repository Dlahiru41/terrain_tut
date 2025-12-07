using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to quickly set up visual enhancements on terrain and game objects
/// </summary>
public class VisualEnhancementSetup : EditorWindow
{
    private Terrain selectedTerrain;
    private GameObject selectedPlayer;
    
    [MenuItem("Tools/Setup Visual Enhancements")]
    public static void ShowWindow()
    {
        GetWindow<VisualEnhancementSetup>("Visual Enhancement Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Terrain Visual Enhancement Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool helps you quickly set up visual enhancements for your terrain game. " +
            "It will add texturing, improve artifact visuals, and enhance player appearance.",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Terrain Setup Section
        GUILayout.Label("Terrain Setup", EditorStyles.boldLabel);
        selectedTerrain = (Terrain)EditorGUILayout.ObjectField("Terrain", selectedTerrain, typeof(Terrain), true);
        
        if (GUILayout.Button("Auto-Find Terrain"))
        {
            selectedTerrain = FindObjectOfType<Terrain>();
            if (selectedTerrain != null)
            {
                Debug.Log($"Found terrain: {selectedTerrain.name}");
            }
            else
            {
                Debug.LogWarning("No terrain found in scene");
            }
        }
        
        if (GUILayout.Button("Setup Terrain Texturing") && selectedTerrain != null)
        {
            SetupTerrainTexturing(selectedTerrain);
        }
        
        EditorGUILayout.Space();
        
        // Player Setup Section
        GUILayout.Label("Player Setup", EditorStyles.boldLabel);
        selectedPlayer = (GameObject)EditorGUILayout.ObjectField("Player", selectedPlayer, typeof(GameObject), true);
        
        if (GUILayout.Button("Auto-Find Player"))
        {
            selectedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (selectedPlayer == null)
            {
                selectedPlayer = GameObject.Find("Player");
            }
            
            if (selectedPlayer != null)
            {
                Debug.Log($"Found player: {selectedPlayer.name}");
            }
            else
            {
                Debug.LogWarning("No player found (searched for tag 'Player' and name 'Player')");
            }
        }
        
        if (GUILayout.Button("Setup Player Visuals") && selectedPlayer != null)
        {
            SetupPlayerVisuals(selectedPlayer);
        }
        
        EditorGUILayout.Space();
        
        // Artifacts Setup Section
        GUILayout.Label("Artifacts Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("Find and Update Artifact Spawner"))
        {
            UpdateArtifactSpawner();
        }
        
        EditorGUILayout.Space();
        
        // Complete Setup Section
        GUILayout.Label("Complete Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("Setup Everything (Auto-detect)"))
        {
            SetupEverything();
        }
    }
    
    void SetupTerrainTexturing(Terrain terrain)
    {
        // Check if ImprovedTerrainGenerator exists
        ImprovedTerrainGenerator terrainGen = terrain.GetComponent<ImprovedTerrainGenerator>();
        if (terrainGen == null)
        {
            Debug.LogWarning("ImprovedTerrainGenerator not found on terrain. Please add it first.");
            return;
        }
        
        // Add TerrainTextureGenerator if not present
        TerrainTextureGenerator textureGen = terrain.GetComponent<TerrainTextureGenerator>();
        if (textureGen == null)
        {
            textureGen = terrain.gameObject.AddComponent<TerrainTextureGenerator>();
            Debug.Log("Added TerrainTextureGenerator component");
        }
        
        // Enable auto-texture application
        terrainGen.applyTexturesAfterGeneration = true;
        
        // Apply textures now
        textureGen.ApplyTerrainTextures();
        
        EditorUtility.SetDirty(terrain.gameObject);
        Debug.Log("Terrain texturing setup complete! Textures have been applied.");
    }
    
    void SetupPlayerVisuals(GameObject player)
    {
        // Add PlayerVisualEnhancer if not present
        PlayerVisualEnhancer visualEnhancer = player.GetComponent<PlayerVisualEnhancer>();
        if (visualEnhancer == null)
        {
            visualEnhancer = player.AddComponent<PlayerVisualEnhancer>();
            Debug.Log("Added PlayerVisualEnhancer component");
        }
        
        // Apply enhancements
        visualEnhancer.EnhancePlayerVisuals();
        visualEnhancer.AddPlayerIndicator();
        
        EditorUtility.SetDirty(player);
        Debug.Log("Player visual enhancements applied!");
    }
    
    void UpdateArtifactSpawner()
    {
        ArtifactSpawner spawner = FindObjectOfType<ArtifactSpawner>();
        if (spawner != null)
        {
            // Clear old artifacts and respawn with new enhanced visuals
            spawner.ClearSpawnedArtifacts();
            spawner.SpawnArtifacts();
            
            EditorUtility.SetDirty(spawner.gameObject);
            Debug.Log("Artifact spawner updated! Artifacts respawned with enhanced visuals.");
        }
        else
        {
            Debug.LogWarning("No ArtifactSpawner found in scene");
        }
    }
    
    void SetupEverything()
    {
        bool anySetup = false;
        
        // Setup terrain
        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            selectedTerrain = terrain;
            SetupTerrainTexturing(terrain);
            anySetup = true;
        }
        else
        {
            Debug.LogWarning("No terrain found in scene");
        }
        
        // Setup player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        
        if (player != null)
        {
            selectedPlayer = player;
            SetupPlayerVisuals(player);
            anySetup = true;
        }
        else
        {
            Debug.LogWarning("No player found in scene");
        }
        
        // Update artifacts
        UpdateArtifactSpawner();
        
        if (anySetup)
        {
            Debug.Log("=== Complete setup finished! ===");
            EditorUtility.DisplayDialog(
                "Setup Complete",
                "Visual enhancements have been applied!\n\n" +
                "• Terrain now has textures (grass, dirt, rock, snow)\n" +
                "• Player has enhanced visuals\n" +
                "• Artifacts have improved materials\n\n" +
                "Press Play to see the results!",
                "OK"
            );
        }
        else
        {
            Debug.LogWarning("Could not find terrain or player to set up");
        }
    }
}
