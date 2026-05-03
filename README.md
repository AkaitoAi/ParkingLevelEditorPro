Parking Level Editor PRO

A custom Unity Editor tool for rapid creation of grid-based parking game levels. This tool replaces manual prop placement with a structured, scalable workflow using a grid system, prop palette, and controlled variation system.

It is designed for games that require precise placement (such as parking or puzzle games) while still supporting fast iteration and visual variation.

Features
Grid-Based Placement
Snap all objects to a configurable grid
Prevent overlapping using multi-cell occupancy
Supports different prop sizes via footprint system
Prop Palette System
Define multiple prop entries
Each entry appears as a selectable button in a grid-based UI
Fast switching between props during level creation
Variant System with Advanced Control
Each prop can have multiple variants
Weighted random selection for controlled distribution
Per-variant settings:
Weight (probability)
Minimum and maximum scale
Rotation control
Drag Painting
Click and drag to place multiple objects quickly
Works with grid constraints and occupancy checks
Smart Prefab Support
Use PropData component to define:
Footprint (grid size)
Pivot offset
Scene-Safe Workflow
Rebuild system reconstructs grid data from scene
Undo support for all operations
Cleanup Utilities
Clear all placed objects
Remove PropData components from all objects under a parent root
Visual Tools
Toggleable grid visualization
Selected prop highlighting
Scrollable palette with adjustable column count
Installation

Place the script inside an Editor folder:

Assets/ParkingLevelEditorPro/Editor

Open the tool:

Tools → Parking Level Editor PRO
Setup
1. Create a Parent Root

Create an empty GameObject in your scene:

LevelRoot

Assign it to the tool under "Parent Root".

2. Prepare Prefabs

Each prefab should have a PropData component:

PropData
- footprint (Vector2Int)
- pivotOffset (Vector3)
  Example:
  Cone → (1,1)
  Barrier → (2,1)
  Car → (2,2)
3. Configure Prop Entries

In the tool:

Add a new Prop Entry
Set:
Name
Base Prefab
Variants (optional)
Variant Setup:

Each variant supports:

Prefab
Weight (probability)
Min/Max scale
Rotation toggle
Usage
Selecting a Prop
Use the palette grid to select a prop
Selected button is highlighted
Placing Objects
Left click: Place object on grid
Left click + drag: Paint multiple objects
Right click: Remove object
Placement Rules
Objects snap to grid
Placement is blocked if space is occupied
Multi-cell objects reserve multiple grid positions
Rebuilding Data

If objects are moved or desynced:

Click → Rebuild Data

This reconstructs the internal grid state.

Clearing the Level
Click → Clear All

Removes all placed objects.

Removing PropData
Click → Remove PropData From Parent Root

Removes PropData components from all child objects under the assigned parent.

Recommended Workflow
Define grid size and cell size
Create and configure prop entries
Build level in layers:
Layout (roads, slots)
Gameplay elements (cars, barriers)
Decoration (cones, props)
Use drag painting for speed
Use weighted variants for visual variation
Finalize by cleaning up PropData if needed

Best Practices
Keep a consistent world scale across all prefabs
Use variants for visual diversity, not gameplay logic
Avoid manually moving placed objects; use the tool instead
Keep variant weights balanced to avoid repetitive patterns
Use low scale variation for realism

Limitations
Grid is currently planar (Y = 0 projection fallback)
No built-in spline or road generation
No brush size or area fill (can be extended)