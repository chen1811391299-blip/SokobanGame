# Sokoban Product Delivery Notes

This project is structured as a technical designer submission: a playable Sokoban game with a complete level flow, built-in progression, roguelike-style modifiers, and an in-game level editor.

## Complete Player Flow

- Main Menu -> Level Select -> Gameplay -> Level Complete -> Next Level.
- Built-in levels unlock in order.
- Custom levels saved from the editor are loaded from `StreamingAssets/Levels` and are selectable without progression locks.
- Completion records best moves and awards 1 to 3 stars based on each level's par move count.
- Gameplay supports restart, pause, main-menu return, undo, and direct test-play return to the editor.

## Level Editor Flow

- Create a new level with configurable width, height, name, and par moves.
- Paint all supported tile types: empty, wall, floor, player, box, goal, box on goal, ice, pressure plate, door, and portal.
- Portal placement works as a pairing tool: place two portal tiles to create one linked pair.
- Door placement works as a link tool: place a pressure plate first, then a controlled door.
- Validation checks player count, box/goal balance, portal pairs, and pressure-plate/door links.
- Save exports valid custom levels as JSON.
- Load imports the most recently saved custom JSON level.
- Test Play validates the level and launches it through the same Gameplay scene.

## Product-Level Scope

- Temporary art is acceptable and implemented as color-coded Unity materials and prefabs.
- Runtime logic is separated from rendering: `SokobanGrid` is pure gameplay state, while Unity behaviours handle input, UI, rendering, scenes, and persistence.
- EditMode tests cover movement, pushing, win state, undo snapshots, JSON conversion, portals, pressure plates, and editor validation.
- Scene setup tools under `SokobanGame/` can recreate materials, prefabs, level data, and scene hierarchies.

## Recommended Setup Order

Run these Unity editor menu items in order if scenes or assets need to be regenerated:

1. `SokobanGame/1. Create Materials & Prefabs`
2. `SokobanGame/2. Create Level Data Assets`
3. `SokobanGame/3. Create All Scenes`
4. `SokobanGame/4. Build All Scene Hierarchies`
5. `SokobanGame/5. Fix Level Names (English)`

Open `Assets/Scenes/MainMenu.unity` and press Play.
