# Dynamic Joystick System - Setup Guide

## Overview
The dynamic joystick spawns at the player's touch location and automatically fades out after 2 seconds of inactivity. Includes robust object pooling for performance.

## Components

### `DynamicJoystickSpawner`
- **Purpose**: Listens for touches on a UI area and spawns joysticks dynamically
- **Key Settings**:
  - `Joystick Prefab`: The joystick to spawn
  - `Canvas`: Parent canvas for UI
  - `Idle Time Before Fade`: 2 seconds (default)
  - `Fade Duration`: 0.5 seconds (default)
  - `Spawn Offset`: Offset from touch (e.g., -80 pixels down)
  - `Respawn Distance`: If touch is too far, spawn new joystick
  - `Initial Pool Size`: Pre-create 2 joystick instances

### `DynamicJoystick`
- **Purpose**: Individual joystick instance with fade-out logic
- **Auto-Features**:
  - Fades out after idle timeout
  - Returns to pool when done
  - Resets timer on input

## Setup in Unity (Step-by-Step)

### 1. Create Canvas & Joystick UI
```
1. Create a new Canvas (Screen Space - Overlay)
2. Inside Canvas, create an Image for the joystick base
   - Name it "JoystickBackground"
   - Set size to 120x120 (or desired size)
   - Use a circular sprite or placeholder
3. Inside JoystickBackground, create another Image for the handle
   - Name it "JoystickHandle"
   - Set size to 60x60
   - Use a circle sprite or placeholder
4. Save as Prefab: Assets/Prefabs/DynamicJoystick.prefab
```

### 2. Add Spawner Component
```
1. Select the Canvas
2. Add Component → DynamicJoystickSpawner
3. Drag the joystick prefab into "Joystick Prefab"
4. Drag the Canvas into "Canvas" field
5. Adjust settings if needed:
   - Idle Time Before Fade: 2
   - Fade Duration: 0.5
   - Spawn Offset: (0, -80)
   - Initial Pool Size: 2
```

### 3. Update Player Controller
```
1. Select your Player GameObject
2. In SmoothPlayerController inspector:
   - Drag the Canvas (with DynamicJoystickSpawner) into "Move Joystick Spawner"
   - Leave "Move Joystick" empty (uses spawner instead)
3. Keep Jump Button and Sprint Button as needed
```

### 4. Add Joystick Component to Prefab
```
1. Open the DynamicJoystick prefab
2. In Inspector, add Component → DynamicJoystick
3. Drag JoystickBackground into "Background" field
4. Drag JoystickHandle into "Handle" field
5. Adjust if needed:
   - Handle Range: 60 (pixels)
   - Dead Zone: 0.1
6. Save prefab
```

## How It Works

1. **Touch Down**: Player touches the screen
2. **Spawn**: Joystick appears at touch location
3. **Drag**: Joystick follows touch input
4. **Release**: Timer starts (idle countdown)
5. **Fade**: After 2 seconds with no input, joystick fades out
6. **Pool**: Instance returns to pool for reuse
7. **New Touch**: New joystick spawns or existing one reactivates

## Customization

### Faster Fade
```csharp
// In DynamicJoystickSpawner Inspector:
Idle Time Before Fade: 1.0
Fade Duration: 0.3
```

### Different Spawn Offset
```csharp
// Move joystick further from touch
Spawn Offset: (0, -150)
```

### Larger Pool
```csharp
// For faster paced games
Initial Pool Size: 4
```

## Performance Notes
- **Pooling**: All instances pre-created at start; no allocations during gameplay
- **Fade**: Uses Coroutine (1 per active joystick at a time)
- **Search**: `GetActiveDynamicJoystick()` searches children—minimal overhead for 1-2 joysticks

## Optional: Second Joystick (Camera Look)
You can add a second spawner and joystick on the right side of the screen:
```
1. Duplicate the Canvas setup
2. Create second prefab "DynamicJoystickRight"
3. Add DynamicJoystickSpawner with different positioning
4. Assign to SmoothCameraFollow.lookJoystick
```

## Troubleshooting

**Joystick doesn't appear**
- Check Canvas is enabled
- Joystick prefab has Image components
- DynamicJoystickSpawner is on Canvas

**Joystick doesn't fade**
- Check "Idle Time Before Fade" > 0
- Ensure CanvasGroup is being set (auto-added if missing)

**Input not reading**
- Check SmoothPlayerController has spawner assigned (not static joystick)
- Ensure "Move Joystick" field is empty
- Check player is using camera-relative input

---

Ready to use! Let me know if you want further tweaks.
