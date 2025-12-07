using UnityEngine;

[ExecuteAlways] // Allows generation from the editor via context menu
public class ImprovedTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Reference")]
    public Terrain terrain;

    private TerrainData _terrainData;

    [Header("Noise Settings")]
    [Min(0.0001f)]
    public float noiseScale = 0.01f;

    [Range(0f, 1f)]
    public float heightScale = 0.2f;

    public int seed;
    public Vector2 offset = Vector2.zero;

    [Header("Fractal Noise (Octaves)")]
    [Min(1)]
    public int octaves = 4;

    [Range(0f, 1f)]
    public float persistence = 0.5f; // amplitude multiplier per octave

    [Min(1f)]
    public float lacunarity = 2f;    // frequency multiplier per octave

    [Header("Generation Triggers")]
    public bool autoGenerateOnStart = true;
    public KeyCode regenerateKey = KeyCode.R;
    
    [Header("Texture Application")]
    [Tooltip("Automatically apply textures after generating terrain")]
    public bool applyTexturesAfterGeneration = true;

    void Awake()
    {
        CacheTerrainData();
    }

    void Start()
    {
        CacheTerrainData();

        if (autoGenerateOnStart && _terrainData != null)
        {
            GenerateTerrain();
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        // In edit mode, Update also runs due to [ExecuteAlways], so guard by application state
        if (!Application.isPlaying) return;
#endif

        if (Input.GetKeyDown(regenerateKey))
        {
            GenerateTerrain();
        }
    }

    void CacheTerrainData()
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
            Debug.LogWarning($"{nameof(ImprovedTerrainGenerator)}: No Terrain assigned or found on this GameObject.");
        }
    }

    [ContextMenu("Generate Terrain Now")]
    public void GenerateTerrainContextMenu()
    {
        CacheTerrainData();
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (_terrainData == null)
        {
            Debug.LogWarning($"{nameof(ImprovedTerrainGenerator)}: TerrainData is null, cannot generate terrain.");
            return;
        }

        int width = _terrainData.heightmapResolution;
        int height = _terrainData.heightmapResolution;

        float[,] heights = new float[width, height];

        // Seed the pseudo-random so different seeds give different offsets
        System.Random prng = new System.Random(seed);
        float seedOffsetX = prng.Next(-100000, 100000);
        float seedOffsetY = prng.Next(-100000, 100000);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = EvaluateHeight(x, y, width, height, seedOffsetX, seedOffsetY);
            }
        }

        _terrainData.SetHeights(0, 0, heights);
        
        // Automatically apply textures if enabled
        if (applyTexturesAfterGeneration)
        {
            TerrainTextureGenerator textureGen = GetComponent<TerrainTextureGenerator>();
            if (textureGen != null)
            {
                textureGen.ApplyTerrainTextures();
            }
        }
    }

    float EvaluateHeight(int x, int y, int mapWidth, int mapHeight, float seedOffsetX, float seedOffsetY)
    {
        // Normalize coordinates to [0,1] across terrain
        float xNorm = (float)x / mapWidth;
        float yNorm = (float)y / mapHeight;

        // Base coordinates for noise
        float sampleX = xNorm / Mathf.Max(noiseScale, 0.0001f) + offset.x + seedOffsetX;
        float sampleY = yNorm / Mathf.Max(noiseScale, 0.0001f) + offset.y + seedOffsetY;

        float amplitude = 1f;
        float frequency = 1f;
        float heightValue = 0f;

        // Multi-octave (fractal) Perlin noise
        for (int i = 0; i < octaves; i++)
        {
            float perlinX = sampleX * frequency;
            float perlinY = sampleY * frequency;

            float perlinValue = Mathf.PerlinNoise(perlinX, perlinY); // 0..1
            heightValue += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize height by total possible amplitude so sliders are more predictable
        float maxPossibleAmplitude = (1f - Mathf.Pow(persistence, octaves)) / (1f - persistence);
        if (maxPossibleAmplitude <= 0f)
            maxPossibleAmplitude = 1f;

        heightValue /= maxPossibleAmplitude;

        // Finally scale to terrain height scale
        return Mathf.Clamp01(heightValue * heightScale);
    }
}
