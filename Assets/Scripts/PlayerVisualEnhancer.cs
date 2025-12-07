using UnityEngine;

[ExecuteAlways]
public class PlayerVisualEnhancer : MonoBehaviour
{
    [Header("Player Visual Settings")]
    [Tooltip("Automatically enhance player visuals on start")]
    public bool autoEnhanceOnStart = true;
    
    [Tooltip("Player body color")]
    public Color bodyColor = new Color(0.2f, 0.4f, 0.8f); // Blue
    
    [Tooltip("Add emission glow to player")]
    public bool addGlow = true;
    
    [Tooltip("Glow intensity")]
    [Range(0f, 1f)]
    public float glowIntensity = 0.3f;
    
    [Header("Player Scale Settings")]
    [Tooltip("Player scale relative to terrain. 1.0 = normal human size (~2 units tall)")]
    [Range(0.5f, 3.0f)]
    public float playerScale = 1.0f;
    
    [Tooltip("Apply scale on start")]
    public bool applyScaleOnStart = false;
    
    void Start()
    {
        if (applyScaleOnStart)
        {
            ApplyPlayerScale();
        }
        
        if (autoEnhanceOnStart)
        {
            EnhancePlayerVisuals();
        }
    }
    
    [ContextMenu("Enhance Player Visuals")]
    public void EnhancePlayerVisuals()
    {
        // Get or create player visual components
        Renderer rend = GetComponent<Renderer>();
        
        if (rend == null)
        {
            // Check if there's already a visual child
            Transform visualChild = transform.Find("PlayerVisual");
            if (visualChild != null)
            {
                rend = visualChild.GetComponent<Renderer>();
            }
            
            // If still no renderer, create a visual representation
            if (rend == null)
            {
                GameObject visual = CreatePlayerVisual();
                rend = visual.GetComponent<Renderer>();
            }
        }
        
        if (rend != null)
        {
            ApplyEnhancedMaterial(rend);
            Debug.Log($"PlayerVisualEnhancer: Enhanced visuals for '{gameObject.name}'");
        }
    }
    
    [ContextMenu("Apply Player Scale")]
    public void ApplyPlayerScale()
    {
        transform.localScale = Vector3.one * playerScale;
        Debug.Log($"PlayerVisualEnhancer: Applied scale {playerScale} to '{gameObject.name}'");
    }
    
    // Helper method to safely destroy objects in both edit and play mode
    void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
    
    GameObject CreatePlayerVisual()
    {
        // Create a capsule to represent the player if no visual exists
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "PlayerVisual";
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        
        // Remove the collider from the visual (player should have its own collider)
        Collider col = visual.GetComponent<Collider>();
        SafeDestroy(col);
        
        return visual;
    }
    
    void ApplyEnhancedMaterial(Renderer rend)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = bodyColor;
        
        // Make it slightly shiny
        mat.SetFloat("_Glossiness", 0.5f);
        mat.SetFloat("_Metallic", 0.2f);
        
        // Add emission glow if enabled
        if (addGlow)
        {
            mat.EnableKeyword("_EMISSION");
            Color emissionColor = bodyColor * glowIntensity;
            mat.SetColor("_EmissionColor", emissionColor);
        }
        
        rend.sharedMaterial = mat;
    }
    
    // Helper method to make player stand out more
    [ContextMenu("Add Player Indicator")]
    public void AddPlayerIndicator()
    {
        // Add a small floating indicator above the player
        Transform indicator = transform.Find("PlayerIndicator");
        
        if (indicator == null)
        {
            GameObject indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicatorObj.name = "PlayerIndicator";
            indicatorObj.transform.SetParent(transform);
            indicatorObj.transform.localPosition = new Vector3(0, 2f, 0);
            indicatorObj.transform.localScale = Vector3.one * 0.3f;
            
            // Remove collider
            Collider col = indicatorObj.GetComponent<Collider>();
            SafeDestroy(col);
            
            // Make it glow
            Renderer rend = indicatorObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 0.8f);
                mat.SetFloat("_Glossiness", 0.9f);
                rend.sharedMaterial = mat;
            }
            
            // Add a simple floating animation component
            FloatingIndicator floater = indicatorObj.AddComponent<FloatingIndicator>();
            
            Debug.Log("PlayerVisualEnhancer: Added player indicator");
        }
    }
}

// Simple component to make the indicator float up and down
public class FloatingIndicator : MonoBehaviour
{
    public float amplitude = 0.3f;
    public float frequency = 1f;
    
    private Vector3 _startPosition;
    
    void Start()
    {
        _startPosition = transform.localPosition;
    }
    
    void Update()
    {
        if (!Application.isPlaying) return;
        
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = _startPosition + new Vector3(0, offset, 0);
    }
}
