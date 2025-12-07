# Terrain Scaling - Usage Examples

This document provides practical examples of how to use the terrain scaling system in different scenarios.

## Example 1: Quick Setup for New Project

### Step-by-Step
1. Open your scene in Unity
2. Select your Terrain GameObject
3. Go to **Tools → Setup Terrain Scaling**
4. Click **"Medium (200x50x200)"** button
5. Click **"Apply All & Setup Scene"**
6. Press Play to test

### Result
- Terrain: 200 x 50 x 200 units
- Player: Default scale (1.0)
- Good balance for most games

## Example 2: Converting Oversized Terrain

### Problem
Your existing terrain is 1000x1000 units and the player takes 5+ minutes to walk across it.

### Solution
1. Tools → Setup Terrain Scaling
2. Current size shows: 1000 x 600 x 1000
3. Change to: Small (100x30x100) or Medium (200x50x200)
4. Click "Apply Terrain Scale"
5. Right-click ImprovedTerrainGenerator → "Generate Terrain Now"
6. Right-click ArtifactSpawner → "Spawn Artifacts Now"

### Result
- Terrain is now 5-10x smaller
- Player can traverse in 30-60 seconds
- Artifacts are properly distributed

## Example 3: Manual Component Configuration

### Adding TerrainScaleManager Component

```
1. Select Terrain GameObject
2. Add Component → TerrainScaleManager
3. Configure settings:
   - Terrain Width: 150
   - Terrain Length: 150
   - Terrain Height: 40
   - Auto Apply On Start: ✓
   - Show Debug Info: ✓
4. Right-click component → "Apply Terrain Scale"
```

### Configuring Player Scale

```
1. Select Player GameObject  
2. Find or Add Component → PlayerVisualEnhancer
3. Configure settings:
   - Player Scale: 1.2
   - Apply Scale On Start: ✓
   - Auto Enhance On Start: ✓
4. Right-click component → "Apply Player Scale"
```

## Example 4: Runtime Scale Adjustment

### Using Context Menu in Play Mode

While the game is running:

```
1. Select Terrain GameObject
2. Find TerrainScaleManager component
3. Adjust sliders in Inspector
4. Right-click component → "Apply Terrain Scale"
5. See changes immediately (may need to regenerate terrain)
```

### Using Keyboard Shortcuts

Add this to your VisualEnhancementDemo or custom script:

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        terrainScaleManager.SetSmallScale();
    }
    if (Input.GetKeyDown(KeyCode.Alpha2))
    {
        terrainScaleManager.ResetToDefaultGameScale();
    }
    if (Input.GetKeyDown(KeyCode.Alpha3))
    {
        terrainScaleManager.SetLargeScale();
    }
}
```

## Example 5: Different Game Types

### Arcade Action Game
```
Terrain Scale Manager Settings:
- Width: 80
- Height: 25  
- Length: 80
- Show Debug Info: Yes

Player Visual Enhancer Settings:
- Player Scale: 2.0
- Apply Scale On Start: Yes

Result: Fast-paced, visible player, quick traversal
```

### RPG Exploration Game
```
Terrain Scale Manager Settings:
- Width: 300
- Height: 60
- Length: 300
- Show Debug Info: No

Player Visual Enhancer Settings:
- Player Scale: 1.0
- Apply Scale On Start: Yes

Result: Medium-sized world, good for quests and exploration
```

### Survival/Open World Game
```
Terrain Scale Manager Settings:
- Width: 600
- Height: 100
- Length: 600
- Show Debug Info: No

Player Visual Enhancer Settings:
- Player Scale: 1.0
- Apply Scale On Start: Yes

Artifact Spawner Settings:
- Min Spacing: 8.0 (increase for larger terrain)

Result: Large world, emphasis on exploration
```

## Example 6: Troubleshooting Common Issues

### Issue: Changed scale but terrain looks the same

**Solution:**
```
1. Check TerrainData asset in Project window
2. Verify size changed in Inspector
3. If not, manually call Apply Terrain Scale again
4. Regenerate terrain height data (press R in play mode)
```

### Issue: Artifacts spawning outside terrain

**Solution:**
```
1. Select Terrain GameObject
2. Find ArtifactSpawner component
3. Right-click → "Clear Spawned Artifacts"
4. Ensure terrain scale has been applied
5. Right-click → "Spawn Artifacts Now"
```

### Issue: Player falling through terrain

**Solution:**
```
1. Select Player GameObject
2. Find SnapPlayerToTerrain component
3. Right-click → "Snap This Player To Terrain"
4. Or adjust player Y position manually to be on terrain surface
```

## Example 7: Scripting Custom Scales

### Creating a Custom Scale Preset

Create a new script:

```csharp
using UnityEngine;

public class CustomTerrainPresets : MonoBehaviour
{
    public TerrainScaleManager scaleManager;
    
    [ContextMenu("Apply Tiny Arena Scale")]
    public void ApplyTinyArena()
    {
        scaleManager.terrainWidth = 50f;
        scaleManager.terrainLength = 50f;
        scaleManager.terrainHeight = 15f;
        scaleManager.ApplyTerrainScale();
    }
    
    [ContextMenu("Apply Medium Exploration Scale")]
    public void ApplyMediumExploration()
    {
        scaleManager.terrainWidth = 250f;
        scaleManager.terrainLength = 250f;
        scaleManager.terrainHeight = 60f;
        scaleManager.ApplyTerrainScale();
    }
    
    [ContextMenu("Apply Massive World Scale")]
    public void ApplyMassiveWorld()
    {
        scaleManager.terrainWidth = 800f;
        scaleManager.terrainLength = 800f;
        scaleManager.terrainHeight = 150f;
        scaleManager.ApplyTerrainScale();
    }
}
```

### Dynamically Adjusting Based on Player Count

```csharp
public class DynamicTerrainScaler : MonoBehaviour
{
    public TerrainScaleManager scaleManager;
    public float unitsPerPlayer = 50f;
    
    public void AdjustForPlayerCount(int playerCount)
    {
        float targetSize = Mathf.Sqrt(playerCount * unitsPerPlayer * unitsPerPlayer);
        targetSize = Mathf.Clamp(targetSize, 100f, 1000f);
        
        scaleManager.terrainWidth = targetSize;
        scaleManager.terrainLength = targetSize;
        scaleManager.terrainHeight = targetSize * 0.25f;
        scaleManager.ApplyTerrainScale();
        
        Debug.Log($"Terrain scaled for {playerCount} players: {targetSize}x{targetSize}");
    }
}
```

## Example 8: Best Workflow

### Recommended Setup Order

```
1. Create/Open Unity Scene
2. Add Terrain GameObject if needed
3. Add ImprovedTerrainGenerator component
4. Add TerrainScaleManager component
5. Use Tools → Setup Terrain Scaling
6. Select desired preset (start with Medium)
7. Apply terrain scale
8. Generate terrain
9. Add Player GameObject
10. Add PlayerVisualEnhancer component
11. Apply player scale
12. Add ArtifactSpawner component
13. Spawn artifacts
14. Test in Play mode
15. Iterate and adjust as needed
```

## Example 9: Comparing Scales Visually

### Small Scale Test
```
Terrain: 100 x 30 x 100
Time to walk corner to corner: ~20-30 seconds
Visibility: Can see across most of terrain from center
Best for: Quick matches, testing, tutorials
```

### Medium Scale Test
```
Terrain: 200 x 50 x 200
Time to walk corner to corner: ~45-60 seconds
Visibility: Can see about 40% of terrain from center
Best for: Most game types, balanced gameplay
```

### Large Scale Test
```
Terrain: 500 x 100 x 500
Time to walk corner to corner: ~2-3 minutes
Visibility: Can see about 20% of terrain from center
Best for: Exploration, survival, open world
```

## Tips and Tricks

1. **Start Small**: Always begin with smaller terrain (100-200 units) and scale up
2. **Test Early**: Apply scaling early in development
3. **Use Presets**: The built-in presets cover most use cases
4. **Visual Gizmos**: Enable "Show Debug Info" to see terrain bounds in scene view
5. **Regenerate**: Always regenerate terrain and respawn artifacts after scaling
6. **Save Presets**: Use context menus to quickly switch between scales during testing
7. **Document**: Note your chosen scale in design docs for consistency

## Quick Reference

| Scale Type | Width x Height x Length | Walk Time | Best For |
|------------|------------------------|-----------|----------|
| Tiny | 50 x 15 x 50 | 10-15s | Prototypes, arenas |
| Small | 100 x 30 x 100 | 20-30s | Action games |
| Medium | 200 x 50 x 200 | 45-60s | Most games |
| Large | 500 x 100 x 500 | 2-3 min | Exploration |
| Massive | 1000 x 200 x 1000 | 5-7 min | Open world |

## Summary

The terrain scaling system provides flexible, easy-to-use controls for adjusting your game world to match your gameplay vision. Start with the editor tool, use presets for quick results, and fine-tune as needed during development.
