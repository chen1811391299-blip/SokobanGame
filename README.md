# Sokoban Game

A Unity 2022.3 Sokoban puzzle game built as a technical designer submission. The project includes a complete player flow, built-in levels, scoring/progression, gameplay modifiers, and a runtime level editor.

## Highlights

- Classic Sokoban movement with undo, restart, pause, and best-move tracking.
- Mechanics: ice sliding, pressure plates, doors, portal pairs, and fog of war.
- 8 built-in levels with par move counts and star ratings.
- Roguelike-style optional modifiers: all ice, random portals, mirror controls, and fog of war.
- Level editor with tile painting, portal pairing, door linking, validation, save/load, and test play.
- NUnit EditMode tests for core gameplay and editor validation.

## Controls

| Key | Action |
|---|---|
| WASD / Arrow keys | Move |
| Ctrl+Z | Undo |
| R | Restart |
| Escape | Pause |

## Setup

Open the project in Unity 2022.3. If assets or scenes need to be regenerated, run these menu items in order:

1. `SokobanGame/1. Create Materials & Prefabs`
2. `SokobanGame/2. Create Level Data Assets`
3. `SokobanGame/3. Create All Scenes`
4. `SokobanGame/4. Build All Scene Hierarchies`
5. `SokobanGame/5. Fix Level Names (English)`

Then open `Assets/Scenes/MainMenu.unity` and press Play.

## Project Structure

```text
Assets/
  Editor/          Unity editor setup/build scripts
  Materials/       Temporary product-ready materials
  Prefabs/         Tile, entity, and UI prefabs
  Resources/
    Levels/        Built-in LevelData assets
  Scenes/          MainMenu, LevelSelect, Gameplay, LevelEditor
  Scripts/
    Core/          SokobanGrid, UndoManager, SceneTransition
    Controllers/   PlayerController, GridRenderer
    Data/          LevelData, LevelDataJson, TileType, GameState
    Managers/      GameManager, LevelManager, ModifierManager, SaveManager, AudioManager
    UI/            MainMenuUI, LevelSelectUI, GameHUD
    LevelEditor/   Runtime level editor and validation
  Tests/           EditMode tests
docs/
  product_delivery_notes.md
```

See `docs/product_delivery_notes.md` for the delivery checklist and product-scope notes.
See `docs/level_design_bible_30.md` for the 30-level campaign design standard.
