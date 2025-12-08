# Artifact System Guide

## Overview
The Artifact System provides comprehensive procedural placement of collectible items across your terrain with full NavMesh integration and accessibility validation.

## Features

### Phase 3: Artifact Generation & Placement

#### Step 3.1: 6 Unique Artifact Types ✓
The system includes 6 distinct artifact types, each with unique visual properties:

1. **Health Potion** - Red glowing sphere
2. **Coin** - Gold metallic cylinder
3. **Weapon** - Gray metallic capsule
4. **Magic Pickup** - Purple glowing sphere
5. **Shield** - Blue semi-metallic cylinder
6. **Trap** - Brown rough cube

Each artifact has:
- Unique color and material properties
- Custom primitive shape
- Configurable scale relative to terrain size
- Sphere collider for interaction

#### Step 3.2: Artifact Generation Rules ✓
Each artifact type can define:

- **Height Range**: Min/max terrain height (0-1) where artifact spawns
  - Example: Coins spawn at 0.05-0.5 (low-mid elevation)
  - Use -1 for no constraint

- **Slope Constraint**: Maximum slope angle in degrees
  - Example: Shields spawn only on very flat terrain (≤15°)
  - Use -1 to use global `allowedMaxSteepness` setting

- **Spawn Density**: Min/max count per artifact type
  - Configurable `minCount` and `maxCount`
  - System ensures at least `minCount` are placed

- **Spacing**: Minimum distance between any artifacts
  - Configurable `minSpacing` per artifact type
  - Prevents clustering

Default constraints per artifact:
```
Health Potion: height 0.1-0.6, slope ≤30°
Coin:          height 0.05-0.5, slope ≤20° (flat areas)
Weapon:        any height, slope ≤35°
Magic Pickup:  height 0.4-0.9, slope ≤25°
Shield:        height 0.1-0.5, slope ≤15° (very flat)
Trap:          any height/slope (dangerous anywhere!)
```

#### Step 3.3: Artifact Placement Algorithm ✓
The placement algorithm:
1. Iterates across random terrain positions
2. Validates height constraints for the artifact type
3. Validates slope constraints for the artifact type
4. Checks minimum spacing against all placed artifacts
5. Validates NavMesh accessibility (if enabled)
6. Places artifact if all checks pass
7. Logs placement confirmation with coordinates

Algorithm features:
- Maximum attempts per item configurable
- Deterministic placement with optional seed
- Efficient spatial validation
- Proper artifact scaling relative to terrain

#### Step 3.4: Terrain Slope Calculation ✓
Slopes are calculated using:
- `TerrainData.GetSteepness(nx, nz)` for accurate slope angles
- Results in degrees (0-90°)
- Used to constrain artifact placement per type

### Phase 4: NavMesh Integration & Accessibility

#### Step 4.1: NavMesh System Setup ✓
The system integrates with Unity's NavMesh:
- Uses built-in NavMesh components
- Configurable NavMesh sample distance
- Supports all NavMesh areas
- Compatible with NavMeshSurface components

To use:
1. Add NavMeshSurface to terrain GameObject
2. Configure agent parameters (radius, height, max slope)
3. Bake NavMesh
4. Enable `requireNavMeshAccess` in ArtifactSpawner

#### Step 4.2: Accessibility Pre-Validation ✓
Before placing each artifact:
- Samples NavMesh position at candidate location
- Uses `NavMesh.SamplePosition()` with configurable search radius
- Skips locations not on NavMesh (water, cliffs, etc.)

#### Step 4.3: Pathfinding Validation ✓
For each artifact placement:
- Finds player spawn location (tag "Player" or manual assignment)
- Creates NavMeshPath from player to artifact
- Uses `NavMesh.CalculatePath()` to compute path
- Only places artifacts with complete, valid paths
- Rejects partial or failed paths

#### Step 4.4: Path Visualization ✓
Interactive visualization system:

**Features:**
- LineRenderer shows navigation path from player to each artifact
- Green paths for successfully accessible artifacts
- Red paths for debugging failed validations
- Keyboard toggle with 'P' key (configurable)
- Real-time path refresh on toggle
- OnDrawGizmos visualization support

**Controls:**
- `showPathVisualization` - Enable/disable in Inspector
- `togglePathVisualizationKey` - Press to toggle (default: P)
- `successPathColor` - Color for valid paths (default: green)
- `failedPathColor` - Color for debug paths (default: red)
- `pathWidth` - Line width for visualization

**Usage:**
1. Enable `requireNavMeshAccess`
2. Spawn artifacts
3. Toggle visualization with P key
4. Use context menu "Refresh Path Visualizations" to update

#### Step 4.5: Placement Confirmation ✓
After validation:
- Instantiates artifact in scene
- Parents under spawner for organization
- Logs: "ArtifactType #N placed at (x, y, z) - Accessible"
- Tracks in master list per artifact type
- Confirms minimum counts met

Example log output:
```
ArtifactSpawner: Coin #1 placed at (45.2, 8.3, 32.1) - Accessible
ArtifactSpawner: Coin #2 placed at (52.7, 7.9, 28.4) - Accessible
ArtifactSpawner: Successfully placed 5 Coin(s) (min: 3)
ArtifactSpawner: Artifact spawning complete. Total artifacts placed: 23 across 6 types.
```

## Configuration

### Inspector Settings

**Terrain Reference:**
- `terrain` - Reference to terrain (auto-detected if on same GameObject)

**Global Placement:**
- `maxPlacementAttemptsPerItem` - Max tries per artifact (default: 200)
- `allowedMaxSteepness` - Global max slope in degrees (default: 30°)

**Artifact Definitions:**
- Array of 6 artifact types with individual settings:
  - Basic: name, primitive, color, scale
  - Counts: minCount, maxCount
  - Spacing: minSpacing
  - Constraints: minHeight, maxHeight, maxSlope

**Runtime Options:**
- `clearPreviousOnSpawn` - Clear old artifacts when spawning new

**Deterministic Options:**
- `useSeed` - Enable deterministic placement
- `seed` - Seed value for reproducible results

**Scaling:**
- `scaleFactor` - Artifact size relative to terrain (default: 0.01 = 1%)

**NavMesh / Accessibility:**
- `requireNavMeshAccess` - Validate paths (requires NavMesh)
- `playerTransform` - Optional player reference (auto-finds if empty)

**Path Visualization:**
- `showPathVisualization` - Show paths on placement
- `pathMaterial` - Optional material for lines
- `successPathColor` - Color for valid paths
- `failedPathColor` - Color for debug paths
- `pathWidth` - Line width
- `navMeshSampleDistance` - Search radius for NavMesh
- `togglePathVisualizationKey` - Toggle key (default: P)

### Context Menu Commands

Right-click the ArtifactSpawner component:
- **Spawn Artifacts Now** - Generate all artifacts
- **Refresh Path Visualizations** - Update all paths
- **Clear Path Visualizations** - Remove all path lines

## Usage Examples

### Basic Setup
```csharp
// 1. Add ArtifactSpawner to terrain GameObject
// 2. Use defaults (6 types pre-configured)
// 3. Right-click → "Spawn Artifacts Now"
```

### With NavMesh Validation
```csharp
// 1. Add NavMeshSurface to terrain
// 2. Configure and bake NavMesh
// 3. Enable requireNavMeshAccess in ArtifactSpawner
// 4. Ensure player is tagged "Player"
// 5. Spawn artifacts (only accessible ones placed)
```

### Custom Artifact Type
```csharp
// In Inspector:
ArtifactType custom = new ArtifactType();
custom.typeName = "Custom Item";
custom.primitive = PrimitiveType.Cube;
custom.color = Color.yellow;
custom.minHeight = 0.3f;  // Mid-high elevation only
custom.maxHeight = 0.8f;
custom.maxSlope = 20f;    // Gentle slopes only
custom.minCount = 5;      // At least 5 spawned
custom.minSpacing = 3f;   // 3 units apart minimum
```

### Deterministic Placement
```csharp
// For reproducible artifact layouts:
spawner.useSeed = true;
spawner.seed = 12345;
spawner.SpawnArtifacts();
// Same seed = same layout every time
```

## Integration with Terrain Scaling

When using `TerrainScaleManager`:
- `scaleFactor` automatically adjusts artifact size
- Call `CacheTerrain()` after terrain size changes
- Artifacts scale proportionally to terrain dimensions

Example:
```csharp
// After scaling terrain
artifactSpawner.CacheTerrain();
artifactSpawner.SpawnArtifacts();
```

## Troubleshooting

**Artifacts not spawning:**
- Check terrain reference is assigned
- Ensure constraints aren't too restrictive
- Increase `maxPlacementAttemptsPerItem`
- Check console for warnings

**NavMesh validation failing:**
- Verify NavMesh is baked
- Check `navMeshSampleDistance` (try increasing)
- Ensure player exists and is tagged "Player"
- Verify terrain is included in NavMesh

**Paths not visible:**
- Enable `showPathVisualization`
- Ensure `requireNavMeshAccess` is enabled
- Press P key to toggle
- Check path colors aren't transparent

**Too few artifacts placed:**
- Relax height/slope constraints
- Reduce `minSpacing`
- Increase `maxPlacementAttemptsPerItem`
- Disable NavMesh validation for testing

## Performance Considerations

- Artifact spawning: O(n*m) where n=artifacts, m=attempts
- NavMesh validation adds overhead per attempt
- Path visualization: minimal runtime cost
- Use deterministic mode for reproducible testing
- Clear old artifacts before respawning

## Future Enhancements

Potential additions:
- Per-artifact exclusion zones
- Biome-based spawning rules
- Runtime artifact collection/respawn
- Pooling for dynamic spawning
- Custom prefab support beyond primitives
- Multi-player spawn point support

## API Reference

### Public Methods

```csharp
// Spawn all artifacts according to current settings
void SpawnArtifacts()

// Clear all spawned artifacts
void ClearSpawnedArtifacts()

// Update terrain references (call after terrain changes)
void CacheTerrain()

// Refresh path visualizations for all spawned artifacts
void RefreshPathVisualizations()

// Remove all path visualizations
void ClearPathVisualizations()
```

### Context Menu Methods

```csharp
[ContextMenu("Spawn Artifacts Now")]
void SpawnArtifactsContextMenu()

[ContextMenu("Refresh Path Visualizations")]
void RefreshPathVisualizations()

[ContextMenu("Clear Path Visualizations")]
void ClearPathVisualizations()
```

## Credits

Implements comprehensive artifact system with NavMesh integration following procedural generation best practices.
