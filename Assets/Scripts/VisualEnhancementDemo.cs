using UnityEngine;

/// <summary>
/// Component that can be added to demonstrate and test the visual enhancements.
/// Provides runtime controls and visual feedback.
/// </summary>
public class VisualEnhancementDemo : MonoBehaviour
{
    [Header("References")]
    public ImprovedTerrainGenerator terrainGenerator;
    public ArtifactSpawner artifactSpawner;
    public GameObject player;
    
    [Header("Demo Controls")]
    [Tooltip("Show on-screen instructions")]
    public bool showInstructions = true;
    
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;
    
    private bool _initialized = false;
    
    void Start()
    {
        AutoFindComponents();
        LogSetupStatus();
    }
    
    void AutoFindComponents()
    {
        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<ImprovedTerrainGenerator>();
        }
        
        if (artifactSpawner == null)
        {
            artifactSpawner = FindObjectOfType<ArtifactSpawner>();
        }
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
        }
        
        _initialized = (terrainGenerator != null || artifactSpawner != null);
    }
    
    void LogSetupStatus()
    {
        if (!debugMode) return;
        
        Debug.Log("=== Visual Enhancement Demo Status ===");
        Debug.Log($"Terrain Generator: {(terrainGenerator != null ? "Found" : "Not Found")}");
        Debug.Log($"Artifact Spawner: {(artifactSpawner != null ? "Found" : "Not Found")}");
        Debug.Log($"Player: {(player != null ? "Found" : "Not Found")}");
        
        if (terrainGenerator != null)
        {
            TerrainTextureGenerator textureGen = terrainGenerator.GetComponent<TerrainTextureGenerator>();
            Debug.Log($"Terrain Texturing: {(textureGen != null ? "Enabled" : "Not Setup")}");
        }
        
        if (player != null)
        {
            PlayerVisualEnhancer visualEnhancer = player.GetComponent<PlayerVisualEnhancer>();
            Debug.Log($"Player Enhancement: {(visualEnhancer != null ? "Enabled" : "Not Setup")}");
        }
        
        Debug.Log("======================================");
    }
    
    void Update()
    {
        // Additional controls beyond the default 'R' key
        if (Input.GetKeyDown(KeyCode.T))
        {
            ApplyTerrainTextures();
        }
        
        if (Input.GetKeyDown(KeyCode.A))
        {
            RespawnArtifacts();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            EnhancePlayerVisuals();
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            showInstructions = !showInstructions;
        }
    }
    
    void ApplyTerrainTextures()
    {
        if (terrainGenerator != null)
        {
            TerrainTextureGenerator textureGen = terrainGenerator.GetComponent<TerrainTextureGenerator>();
            if (textureGen != null)
            {
                textureGen.ApplyTerrainTextures();
                Debug.Log("Applied terrain textures manually");
            }
            else
            {
                Debug.LogWarning("TerrainTextureGenerator component not found. Add it to the terrain GameObject.");
            }
        }
    }
    
    void RespawnArtifacts()
    {
        if (artifactSpawner != null)
        {
            artifactSpawner.ClearSpawnedArtifacts();
            artifactSpawner.SpawnArtifacts();
            Debug.Log("Respawned artifacts with enhanced visuals");
        }
        else
        {
            Debug.LogWarning("ArtifactSpawner not found");
        }
    }
    
    void EnhancePlayerVisuals()
    {
        if (player != null)
        {
            PlayerVisualEnhancer enhancer = player.GetComponent<PlayerVisualEnhancer>();
            if (enhancer == null)
            {
                enhancer = player.AddComponent<PlayerVisualEnhancer>();
            }
            enhancer.EnhancePlayerVisuals();
            Debug.Log("Enhanced player visuals");
        }
        else
        {
            Debug.LogWarning("Player not found");
        }
    }
    
    void OnGUI()
    {
        if (!showInstructions) return;
        
        // Create a styled box for instructions
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        boxStyle.normal.textColor = Color.white;
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;
        
        // Position in top-left corner
        float width = 300f;
        float height = 200f;
        Rect boxRect = new Rect(10, 10, width, height);
        
        GUI.Box(boxRect, "", boxStyle);
        
        GUILayout.BeginArea(new Rect(20, 20, width - 20, height - 20));
        
        GUILayout.Label("Visual Enhancement Demo", new GUIStyle(labelStyle) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);
        
        GUILayout.Label("Controls:", new GUIStyle(labelStyle) { fontStyle = FontStyle.Bold });
        GUILayout.Label("R - Regenerate Terrain", labelStyle);
        GUILayout.Label("T - Apply Textures", labelStyle);
        GUILayout.Label("A - Respawn Artifacts", labelStyle);
        GUILayout.Label("P - Enhance Player", labelStyle);
        GUILayout.Label("H - Toggle Instructions", labelStyle);
        
        GUILayout.Space(10);
        GUILayout.Label($"Status: {(_initialized ? "Ready" : "Setup Required")}", labelStyle);
        
        GUILayout.EndArea();
    }
}
