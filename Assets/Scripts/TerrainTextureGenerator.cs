using UnityEngine;

[RequireComponent(typeof(Terrain))]
[ExecuteAlways]
public class TerrainTextureGenerator : MonoBehaviour
{
    [Header("Terrain Reference")]
    public Terrain terrain;
    
    [Header("Height-Based Texturing")]
    [Tooltip("Height below which grass texture dominates (0-1 normalized)")]
    [Range(0f, 1f)]
    public float grassHeight = 0.3f;
    
    [Tooltip("Height above which snow texture dominates (0-1 normalized)")]
    [Range(0f, 1f)]
    public float snowHeight = 0.6f;
    
    [Header("Slope-Based Texturing")]
    [Tooltip("Slope angle (degrees) above which cliff/rock texture shows")]
    [Range(0f, 90f)]
    public float cliffSlopeThreshold = 40f;
    
    [Header("Texture Colors (Used if no terrain layers available)")]
    public Color grassColor = new Color(0.2f, 0.6f, 0.2f); // Green grass
    public Color dirtColor = new Color(0.5f, 0.35f, 0.2f); // Brown dirt
    public Color rockColor = new Color(0.4f, 0.4f, 0.4f); // Gray rock
    public Color snowColor = new Color(0.95f, 0.95f, 1f);  // White snow
    
    private TerrainData _terrainData;
    private Texture2D _splatMapTexture;
    
    void Awake()
    {
        CacheTerrain();
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
    }
    
    [ContextMenu("Apply Terrain Textures")]
    public void ApplyTerrainTextures()
    {
        if (_terrainData == null)
        {
            Debug.LogWarning("TerrainTextureGenerator: No TerrainData found.");
            return;
        }
        
        // Create terrain layers if they don't exist
        CreateTerrainLayers();
        
        // Apply textures based on height and slope
        GenerateSplatMap();
        
        Debug.Log("TerrainTextureGenerator: Applied textures to terrain.");
    }
    
    void CreateTerrainLayers()
    {
        // Create 4 terrain layers: grass, dirt, rock/cliff, snow
        TerrainLayer[] layers = new TerrainLayer[4];
        
        // Grass layer (0)
        layers[0] = new TerrainLayer();
        layers[0].diffuseTexture = CreateColorTexture(grassColor, "Grass");
        layers[0].tileSize = new Vector2(15, 15);
        layers[0].name = "Grass";
        
        // Dirt layer (1)
        layers[1] = new TerrainLayer();
        layers[1].diffuseTexture = CreateColorTexture(dirtColor, "Dirt");
        layers[1].tileSize = new Vector2(15, 15);
        layers[1].name = "Dirt";
        
        // Rock/Cliff layer (2)
        layers[2] = new TerrainLayer();
        layers[2].diffuseTexture = CreateColorTexture(rockColor, "Rock");
        layers[2].tileSize = new Vector2(10, 10);
        layers[2].name = "Rock";
        
        // Snow layer (3)
        layers[3] = new TerrainLayer();
        layers[3].diffuseTexture = CreateColorTexture(snowColor, "Snow");
        layers[3].tileSize = new Vector2(20, 20);
        layers[3].name = "Snow";
        
        _terrainData.terrainLayers = layers;
    }
    
    Texture2D CreateColorTexture(Color color, string name)
    {
        // Create a more interesting textured pattern
        int size = 128; // Larger texture for better quality
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true); // Enable mipmaps
        Color[] pixels = new Color[size * size];
        
        // Use Perlin noise to create natural-looking variation
        float scale = 0.15f; // Noise scale for texture detail
        // Use deterministic offset based on color to ensure consistency
        float offset = color.r * 1000f + color.g * 500f + color.b * 250f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                
                // Generate Perlin noise for this pixel
                float noiseValue = Mathf.PerlinNoise(
                    (x + offset) * scale,
                    (y + offset) * scale
                );
                
                // Add more complex noise by combining multiple octaves
                float detailNoise = Mathf.PerlinNoise(
                    (x + offset) * scale * 3f,
                    (y + offset) * scale * 3f
                ) * 0.3f;
                
                // Combine noise values for natural variation
                float totalVariation = (noiseValue * 0.7f + detailNoise) - 0.5f;
                totalVariation *= 0.25f; // Scale down the variation
                
                // Apply variation to the base color
                pixels[index] = new Color(
                    Mathf.Clamp01(color.r + totalVariation),
                    Mathf.Clamp01(color.g + totalVariation),
                    Mathf.Clamp01(color.b + totalVariation)
                );
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        
        return tex;
    }
    
    void GenerateSplatMap()
    {
        int alphamapWidth = _terrainData.alphamapWidth;
        int alphamapHeight = _terrainData.alphamapHeight;
        int heightmapResolution = _terrainData.heightmapResolution;
        
        // Cache the heightmap data for better performance
        float[,] heights = _terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
        float terrainHeight = _terrainData.size.y;
        
        float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, 4];
        
        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                // Normalize coordinates
                float xNorm = (float)x / (alphamapWidth - 1);
                float yNorm = (float)y / (alphamapHeight - 1);
                
                // Sample height from cached heightmap (normalized 0-1)
                int heightX = Mathf.RoundToInt(xNorm * (heightmapResolution - 1));
                int heightY = Mathf.RoundToInt(yNorm * (heightmapResolution - 1));
                float height = heights[heightY, heightX];
                
                // Get steepness at this point
                float steepness = _terrainData.GetSteepness(xNorm, yNorm);
                
                // Calculate texture weights based on height and slope
                float grassWeight = 0f;
                float dirtWeight = 0f;
                float rockWeight = 0f;
                float snowWeight = 0f;
                
                // Steep slopes get rock texture
                if (steepness > cliffSlopeThreshold)
                {
                    rockWeight = Mathf.Clamp01((steepness - cliffSlopeThreshold) / 20f);
                    dirtWeight = 1f - rockWeight;
                }
                else
                {
                    // Height-based texturing for flatter areas
                    if (height < grassHeight)
                    {
                        // Low areas: grass with some dirt
                        grassWeight = 1f - (height / grassHeight) * 0.3f;
                        dirtWeight = (height / grassHeight) * 0.3f;
                    }
                    else if (height < snowHeight)
                    {
                        // Mid areas: dirt with grass fading out
                        float midRange = (height - grassHeight) / (snowHeight - grassHeight);
                        dirtWeight = 1f;
                        grassWeight = Mathf.Max(0f, 0.3f - midRange * 0.3f);
                        rockWeight = midRange * 0.2f; // Add some rock variation
                    }
                    else
                    {
                        // High areas: snow with some rock
                        float snowRange = (height - snowHeight) / (1f - snowHeight);
                        snowWeight = Mathf.Clamp01(snowRange);
                        dirtWeight = 1f - snowWeight;
                    }
                }
                
                // Normalize weights so they sum to 1
                float totalWeight = grassWeight + dirtWeight + rockWeight + snowWeight;
                if (totalWeight > 0f)
                {
                    grassWeight /= totalWeight;
                    dirtWeight /= totalWeight;
                    rockWeight /= totalWeight;
                    snowWeight /= totalWeight;
                }
                else
                {
                    // Default to grass if something went wrong
                    grassWeight = 1f;
                }
                
                // Assign weights to splatmap
                splatmapData[x, y, 0] = grassWeight; // Grass
                splatmapData[x, y, 1] = dirtWeight;  // Dirt
                splatmapData[x, y, 2] = rockWeight;  // Rock
                splatmapData[x, y, 3] = snowWeight;  // Snow
            }
        }
        
        _terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
