using UnityEngine;

/// <summary>
/// Manages the scale relationship between terrain, player, and artifacts to ensure
/// proper relative sizing. Addresses the issue where terrain can be too large for gameplay.
/// </summary>
[ExecuteAlways]
public class TerrainScaleManager : MonoBehaviour
{
    [Header("Terrain Reference")]
    [Tooltip("The terrain to manage. If null, will attempt to find terrain on this GameObject.")]
    public Terrain terrain;

    [Header("Terrain Size Configuration")]
    [Tooltip("Desired terrain width in world units (X axis). Smaller values make terrain more traversable.")]
    [Range(50f, 2000f)]
    public float terrainWidth = 200f;

    [Tooltip("Desired terrain length in world units (Z axis). Smaller values make terrain more traversable.")]
    [Range(50f, 2000f)]
    public float terrainLength = 200f;

    [Tooltip("Maximum terrain height in world units (Y axis). Affects how tall mountains can be.")]
    [Range(10f, 500f)]
    public float terrainHeight = 50f;

    [Header("Auto-Apply Settings")]
    [Tooltip("Automatically apply terrain size on Start")]
    public bool autoApplyOnStart = true;

    [Tooltip("Show terrain dimensions in scene view")]
    public bool showDebugInfo = true;

    private TerrainData _terrainData;

    void Awake()
    {
        CacheTerrain();
    }

    void Start()
    {
        CacheTerrain();
        if (autoApplyOnStart && _terrainData != null)
        {
            ApplyTerrainScale();
        }
    }

    void OnValidate()
    {
        CacheTerrain();
    }

    void CacheTerrain()
    {
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }

        if (terrain != null)
        {
            _terrainData = terrain.terrainData;
        }
        else
        {
            Debug.LogWarning($"{nameof(TerrainScaleManager)}: No Terrain assigned or found on this GameObject.");
        }
    }

    [ContextMenu("Apply Terrain Scale")]
    public void ApplyTerrainScale()
    {
        if (_terrainData == null)
        {
            Debug.LogWarning($"{nameof(TerrainScaleManager)}: TerrainData is null, cannot apply scale.");
            return;
        }

        // Store the current size for comparison
        Vector3 oldSize = _terrainData.size;

        // Apply the new size
        _terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        Debug.Log($"{nameof(TerrainScaleManager)}: Terrain size changed from {oldSize} to {_terrainData.size}");

        // Notify other components that depend on terrain size
        NotifyTerrainSizeChanged();
    }

    void NotifyTerrainSizeChanged()
    {
        // Notify the terrain generator if it exists
        ImprovedTerrainGenerator generator = GetComponent<ImprovedTerrainGenerator>();
        if (generator != null)
        {
            Debug.Log($"{nameof(TerrainScaleManager)}: Notifying ImprovedTerrainGenerator of size change. You may want to regenerate terrain.");
        }

        // Notify artifact spawner if it exists
        ArtifactSpawner spawner = GetComponent<ArtifactSpawner>();
        if (spawner != null)
        {
            spawner.CacheTerrain();
            Debug.Log($"{nameof(TerrainScaleManager)}: Notified ArtifactSpawner of size change. You may want to respawn artifacts.");
        }

        // Notify texture generator if it exists
        TerrainTextureGenerator textureGen = GetComponent<TerrainTextureGenerator>();
        if (textureGen != null)
        {
            Debug.Log($"{nameof(TerrainScaleManager)}: Notified TerrainTextureGenerator of size change.");
        }
    }

    [ContextMenu("Get Recommended Player Scale")]
    public void GetRecommendedPlayerScale()
    {
        if (_terrainData == null)
        {
            Debug.LogWarning($"{nameof(TerrainScaleManager)}: TerrainData is null.");
            return;
        }

        // Recommend player to be about 1.8 units tall (roughly human height)
        float recommendedPlayerHeight = 1.8f;

        Debug.Log($"{nameof(TerrainScaleManager)}: Recommended player height: {recommendedPlayerHeight} units");
        Debug.Log($"{nameof(TerrainScaleManager)}: Terrain width: {terrainWidth}, Length: {terrainLength}, Height: {terrainHeight}");
        Debug.Log($"{nameof(TerrainScaleManager)}: For best results, ensure player and artifacts are scaled proportionally to terrain size.");
    }

    [ContextMenu("Reset To Default Game Scale")]
    public void ResetToDefaultGameScale()
    {
        // Set to a reasonable game-friendly scale
        terrainWidth = 200f;
        terrainLength = 200f;
        terrainHeight = 50f;
        ApplyTerrainScale();
        Debug.Log($"{nameof(TerrainScaleManager)}: Reset to default game scale (200x50x200)");
    }

    [ContextMenu("Set Small/Compact Scale")]
    public void SetSmallScale()
    {
        terrainWidth = 100f;
        terrainLength = 100f;
        terrainHeight = 30f;
        ApplyTerrainScale();
        Debug.Log($"{nameof(TerrainScaleManager)}: Set to small scale (100x30x100) for quick traversal");
    }

    [ContextMenu("Set Large/Exploration Scale")]
    public void SetLargeScale()
    {
        terrainWidth = 500f;
        terrainLength = 500f;
        terrainHeight = 100f;
        ApplyTerrainScale();
        Debug.Log($"{nameof(TerrainScaleManager)}: Set to large scale (500x100x500) for exploration gameplay");
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || _terrainData == null || terrain == null)
            return;

        // Draw a wireframe box showing the terrain dimensions
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = _terrainData.size;

        Gizmos.color = Color.yellow;
        
        // Draw the terrain bounds
        Vector3 center = terrainPos + new Vector3(terrainSize.x * 0.5f, terrainSize.y * 0.5f, terrainSize.z * 0.5f);
        Gizmos.DrawWireCube(center, terrainSize);

        // Draw corner markers
        Gizmos.color = Color.green;
        float markerSize = Mathf.Min(terrainSize.x, terrainSize.z) * 0.02f;
        
        // Bottom corners
        Gizmos.DrawSphere(terrainPos, markerSize);
        Gizmos.DrawSphere(terrainPos + new Vector3(terrainSize.x, 0, 0), markerSize);
        Gizmos.DrawSphere(terrainPos + new Vector3(0, 0, terrainSize.z), markerSize);
        Gizmos.DrawSphere(terrainPos + new Vector3(terrainSize.x, 0, terrainSize.z), markerSize);
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying || _terrainData == null)
            return;

        // Show terrain info in play mode
        string info = $"Terrain Size: {_terrainData.size.x:F0} x {_terrainData.size.y:F0} x {_terrainData.size.z:F0}\n";
        info += $"Resolution: {_terrainData.heightmapResolution}x{_terrainData.heightmapResolution}";

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 12;
        style.normal.textColor = Color.yellow;

        GUI.Box(new Rect(10, Screen.height - 60, 300, 50), info, style);
    }
#endif
}
