
using UnityEngine;

public class ProceduralGeneration : MonoBehaviour
{

    public Terrain terrain;
    private TerrainData _terraindata;
    
    [Range(0, 1f)] public float NoiseScaler;
    [Range(0, 1f)] public float HeightScaler;
    
    // Start is called before the first frame update
    void Start()
    {
        _terraindata = terrain.terrainData;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            EditTerrain();
        }
    }

    void EditTerrain()
    {
        //get terrain resolution
        int heightMapWidth = _terraindata.heightmapResolution;
        int heightMapHeight = _terraindata.heightmapResolution;
        
        float[,] heights = _terraindata.GetHeights(0, 0, heightMapWidth, heightMapHeight);
        
        for (int x = 0; x < heightMapWidth; x++)
        {
            for (int y = 0; y < heightMapHeight; y++)
            {
                heights[x, y] = HeightCalculator(x, y);
            }
        }
        _terraindata.SetHeights(0, 0, heights);
    }

    float HeightCalculator(int x, int y)
    {
        Vector2 pos = new Vector2(x * NoiseScaler, y * NoiseScaler);
        float calculator = Mathf.PerlinNoise(pos.x, pos.y) * HeightScaler;
        return calculator;
    }
}
