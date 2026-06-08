# Level Editor — Product-Grade Design Spec

**Date:** 2026-06-08  
**Project:** Sokoban (Unity 2D)  
**Scope:** Upgrade the existing in-game level editor to production quality for a technical-design exam submission.

---

## Background

The level editor already has a working foundation:
- Tile painting (all TileType values), portal pairing, pressure-plate/door linking
- Undo (Ctrl+Z), save to `StreamingAssets/Levels/*.json`, load-most-recent, test play, resize

The gaps are: no redo, resize destroys existing content, no multi-level list, test-play does not return to editor, validation lacks reachability checks, and the UI is sparse with no visible selected-brush state.

This spec covers five modules that close all gaps and deliver a polished tool.

---

## Module 1 — UI Layout (Layout B)

### Structure

```
┌─────────────────────────────────────────────────────┐
│  TOP BAR: Back | New | Undo | Redo | Resize | Clear  │
│           [Level Name_______] Par[20] | Save | Test  │
├────────────────────────────────┬────────────────────┤
│                                │  VALIDATION PANEL  │
│                                │  Player: 1  ✓      │
│          GRID (center)         │  Boxes:  2  ✓      │
│          largest area          │  Goals:  2  ✓      │
│          left-click: place     │  Portals: OK       │
│          right-click: erase    │  Doors:   OK       │
│          drag: paint           │  [error / warn]    │
│                                ├────────────────────┤
│                                │  SAVED LEVELS      │
│                                │  > my-puzzle ●     │
│                                │    test-level      │
│                                │  [Load Selected]   │
│                                │  [Delete]          │
├────────────────────────────────┴────────────────────┤
│  BRUSH BAR: Wall Floor Player Box Goal BoxOnGoal     │
│             Ice  Portal  Door  Erase                 │
│             (selected brush highlighted with border) │
└─────────────────────────────────────────────────────┘
```

### Brush bar behaviour
- Each brush is a button. The active brush has a coloured border highlight.
- Brushes map directly to `TileType` index (0–10) plus Erase as a special mode.
- Portal and Door brushes trigger the two-step mechanic pairing workflows already implemented in `LevelEditorManager`.

### Mechanic pairing hints
When a mechanic pairing is in progress, the validation panel shows a one-line hint:
- Portal waiting for second tile → `"Click a second Portal tile to complete the pair"`
- Door waiting for door tile → `"Click a Door tile to link"`

---

## Module 2 — Interaction Model Upgrades

### Redo (Ctrl+Y)

Add `_redoStack` alongside `_undoStack` in `LevelEditorManager`:

- `PushUndo()`: clears `_redoStack`, then pushes snapshot to `_undoStack` as before.
- `UndoLastAction()`: pops `_undoStack` → restores → pushes current state onto `_redoStack`.
- `RedoLastAction()`: pops `_redoStack` → restores → pushes current state onto `_undoStack`.
- Both stacks capped at `MaxUndo = 100`.
- `EditorSnapshot` struct is unchanged; same data captures redo state.

### Safe Resize (preserve content)

Replace the current `NewLevel(w, h)` call in the resize button with a new `ResizeLevel(int newW, int newH)` method:

1. Push undo before any change.
2. Create new tile array `newW × newH`, initialised as wall-border + floor interior.
3. Copy tiles from old array where coordinates overlap both bounds.
4. Remove `portalPairs` and `doorLinks` entries where either referenced position falls outside the new bounds.
5. Clear `_pendingPortalPos` / `_pendingPlatePos` if they fall outside.
6. Replace `editingLevel` dimensions and tiles in-place; call `RebuildView()`.

Constraints: width and height clamped to [3, 20].

### Clear Interior

New method `ClearInterior()`:
1. Push undo.
2. For every tile not on the outer border, set to `TileType.Floor`.
3. Remove all portal pairs and door links (interior is now blank).
4. Call `RebuildView()`.

This is exposed as a toolbar button labelled **Clear**.

---

## Module 3 — Validation Upgrades

### New: Reachability Check (hard error)

Added to `LevelValidator.Validate()` after the existing count checks:

- Find the player tile position. If none, the earlier "exactly 1 player" check already fails.
- BFS/flood-fill from player position over tiles where `IsPassable` returns true (Floor, Goal, Ice, PressurePlate, DoorOpen, DoorClosed, Portal, Box, BoxOnGoal, Player).
- After BFS, scan all tiles: any non-Wall, non-Empty tile that was not visited is unreachable.
- If any unreachable gameplay tile found → hard error: `"Unreachable tiles detected. All floor areas must connect to the player start."`

### New: Warning-Level Hints

`ValidationResult` gains `string[] Warnings`. Populated but does not set `IsValid = false`:

| Condition | Warning text |
|-----------|-------------|
| Box in corner, not on goal | `"Box at (x,y) is in a corner and cannot be pushed to a goal"` |
| `parMoves == 20` (default, never changed) | `"Par moves is still the default (20). Consider setting a real target."` |
| Box count > 6 | `"High box count (N). Custom levels with many boxes may be very hard to solve."` |

Corner detection: a box is cornered if two perpendicular neighbours are both walls.

### UI Display

`LevelEditorUI.RefreshValidation()` updated to:
- Show Player / Box / Goal counts with ✓ / ✗ prefix.
- Show Portal pair count and Door link count.
- Show hard error in red if present.
- Show each warning in amber below the error line.
- Save and Test Play buttons are disabled while `!v.IsValid`.

---

## Module 4 — Save, Load, and Test-Play Flow

### Multi-Level List (right panel)

`LevelEditorUI` right panel "Saved Levels" section:
- On `Start()` and after each `Save()`, calls `LevelSerializer.GetAllLevelMeta()` and rebuilds the list.
- Each entry shows `levelName` and modified date.
- One entry is selected at a time (highlighted). Clicking an entry selects it.
- **Load Selected** button is disabled when nothing is selected; when active it calls `LevelSerializer.Load(levelId)` and passes the result to `LevelEditorManager.LoadLevelForEditing()`.
- **Delete** button calls `LevelSerializer.Delete(levelId)`, then refreshes the list. No confirmation dialog — the undo stack is not affected (file deletion is immediate).

`LevelSerializer` additions:
```csharp
public static void Delete(string levelId)   // deletes JSON file
public static LevelData Load(string levelId) // already exists
public static (string id, string name, System.DateTime modified)[] GetAllLevelMeta()
```

### Save Strategy (stable IDs)

Already implemented correctly: empty `levelId` → new GUID, existing `levelId` → overwrite same file. No change needed.

### Test-Play Return to Editor

`LevelManager.IsTestMode` is already set before loading Gameplay. The fix is in `GameManager` or `GameHUD`:

- When `LevelManager.IsTestMode == true`, the **Back** button (and level-complete flow) loads `"LevelEditor"` instead of `"LevelSelect"`.
- The editing `LevelData` is already held in `LevelManager.TestLevel` (a static reference), so editor state survives the scene round-trip.

### Log Fix

`LevelSerializer.Save()`: replace `Debug.Log($"[LevelSerializer] 已保存: {path}")` with `Debug.Log($"[LevelSerializer] Saved: {path}")`.

---

## Module 5 — Custom Tab in Level Select

### LevelSelectUI Changes

`ActNames` array extended from 5 to 6 entries:
```csharp
private static readonly string[] ActNames =
{
    "Simple", "Medium", "Hard", "Remix", "Finale", "Custom"
};
```

Act assignment in `PopulateLevels()`:
```csharp
bool isCustom = data.author != "built-in";
int act = isCustom ? 5 : Mathf.Clamp(i / 6, 0, 4);
```

Custom-level cards:
- Always unlocked (no lock overlay).
- No star display (custom levels have no save-progress entry by default).
- Show `data.levelName` and `data.author`.

Empty state: if Custom tab is selected and no custom-level cards exist, show a centred label:
```
No custom levels yet.
Go to Level Editor to create one.
```

### Main Menu Entry Point

`MainMenuUI` gains a **Level Editor** button that calls `SceneTransition.LoadScene("LevelEditor")`.  
The button is placed below the existing Play button.

---

## Files to Modify

| File | Change |
|------|--------|
| `Assets/Scripts/LevelEditor/LevelEditorManager.cs` | Add `_redoStack`, `RedoLastAction()`, `ResizeLevel()`, `ClearInterior()` |
| `Assets/Scripts/LevelEditor/LevelEditorUI.cs` | Rebuild layout to B; add brush highlight; update validation panel; add level list panel |
| `Assets/Scripts/LevelEditor/EditorGridView.cs` | Minor: reliable tile refresh on resize |
| `Assets/Scripts/LevelEditor/LevelValidator.cs` | Add BFS reachability check; add `Warnings` to `ValidationResult` |
| `Assets/Scripts/LevelEditor/LevelSerializer.cs` | Add `Delete()`, `GetAllLevelMeta()`; fix log text |
| `Assets/Scripts/Managers/LevelManager.cs` | No change required |
| `Assets/Scripts/UI/LevelSelectUI.cs` | Add Custom tab; fix act assignment for custom levels; add empty-state label |
| `Assets/Scripts/UI/MainMenuUI.cs` | Add Level Editor button |
| `Assets/Scripts/Managers/GameManager.cs` or `GameHUD.cs` | Return to LevelEditor when `IsTestMode == true` |
| `Assets/Editor/SceneSetupUtility.cs` | Regenerate LevelEditor scene hierarchy |
| `Assets/Scenes/LevelEditor.unity` | Persist rebuilt layout |

---

## Acceptance Criteria

- Undo and Redo both work across 100 steps without losing mechanic data.
- Resize with existing tiles: tiles inside new bounds are preserved exactly; outside tiles are gone.
- Clear Interior: all non-border tiles become floor; undo restores previous state.
- Save button is disabled when the level has a hard validation error.
- Reachability check catches a disconnected floor pocket.
- Corner-box warning appears but does not block saving.
- Right-side level list shows all saved JSONs; Load Selected and Delete work correctly.
- Test Play → complete/back → returns to LevelEditor scene (not MainMenu).
- Custom Tab in Level Select shows only user-created levels; empty state message shown when list is empty.
- Level Editor accessible from Main Menu via dedicated button.
- No garbled log text in the console.
