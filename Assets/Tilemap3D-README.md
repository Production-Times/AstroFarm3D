Tilemap3D — Simple 3D Tilemap with Paint Mode

Quick overview

- Easy to set up: attach `Tilemap3D` to an empty GameObject and configure grid size and cell size.
- Use a `TilePalette` ScriptableObject (or a single prefab) to select which prefab to paint.
- Set placement rotation: `Place Rotation (Euler)` or enable `Use Prefab Rotation` in the inspector — that rotation is applied when placing tiles.
- Editor "Paint Mode": left-click to place, right-click to remove, Fill Layer and Clear available.

Setup

1. Create an empty GameObject in your scene and add the `Tilemap3D` component.
2. (Optional) Create a `TilePalette` asset: right-click in Project window → Create → Tilemap3D → Tile Palette. Add prefabs to it.
3. Assign the `TilePalette` (or assign a single prefab to `Single Prefab`) in the inspector.
4. Enter "Enter Paint Mode" in the `Tilemap3D` inspector, then left-click in the Scene view to paint.

Notes & tips

- Placed prefabs are instantiated into the `Tilemap3D` GameObject (or `Tile Parent` if set).
- The system works in Edit mode and Play mode.
- Extend by adding pooling, serialization, or snapping rules for procedural generation.

Enjoy — let me know if you want saving/loading, brushes, or runtime spawn APIs added.