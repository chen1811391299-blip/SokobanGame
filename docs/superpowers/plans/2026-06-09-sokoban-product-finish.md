# Sokoban Product Finish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the Sokoban project to a product-level technical-design submission, with polished final built-in levels and a production-ready in-game level editor.

**Architecture:** Keep gameplay rules in `Assets/Scripts/Core/SokobanGrid.cs`, built-in content generation in `Assets/Editor/LevelSetupUtility.cs`, and editor authoring flow under `Assets/Scripts/LevelEditor/`. The final pass should improve content, UX, validation, persistence, and QA without rewriting the working core loop.

**Tech Stack:** Unity, C#, ScriptableObject level assets, JSON custom levels in `Assets/StreamingAssets/Levels`, Python verification tooling in `tools/verify_builtin_levels.py`.

---

## Current State

- The Git worktree was clean before this plan was written.
- Built-in levels 01-27 are implemented and documented.
- Levels 28-30 are pure Sokoban endgame levels, but Level 28 has been rejected as relying too much on extra movement rather than meaningful spatial difficulty.
- The level editor already supports painting tiles, portal pairing, pressure-plate/door links, validation, save, load-most-recent, test play, and undo.
- The editor is functionally usable, but not yet strong enough as the project's most important product-level feature.

## Files To Touch

- Modify: `Assets/Editor/LevelSetupUtility.cs`
  - Update final built-in level definitions and generated metadata.
- Modify: `Assets/Resources/Levels/Level_28.asset`
  - Replace with the approved high-difficulty pure Sokoban design.
- Modify: `Assets/Resources/Levels/Level_29.asset`
  - Review and harden if testing shows it is still weak or overly padded.
- Modify: `Assets/Resources/Levels/Level_30.asset`
  - Review and harden if testing shows it is still weak or overly padded.
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`
  - Add editor commands, redo support, safer resize, clear/fill tools, and stronger mechanic state handling.
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`
  - Rebuild editor UX into a product-grade authoring surface with visible tool state, validation state, metadata, and action groups.
- Modify: `Assets/Scripts/LevelEditor/EditorGridView.cs`
  - Improve editor grid feedback, tile hover/selection state, and reliable refresh behavior.
- Modify: `Assets/Scripts/LevelEditor/LevelValidator.cs`
  - Add product-level validation checks beyond basic counts.
- Modify: `Assets/Scripts/LevelEditor/LevelSerializer.cs`
  - Improve save/load clarity, fix garbled logging, and support practical custom level management.
- Modify: `Assets/Scripts/Managers/LevelManager.cs`
  - Ensure custom levels are discoverable and test-play levels return cleanly.
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`
  - Make custom levels and built-in progression readable from the level select screen.
- Modify: `Assets/Editor/SceneSetupUtility.cs`
  - Regenerate or update LevelEditor scene hierarchy after UI changes.
- Modify: `Assets/Scenes/LevelEditor.unity`
  - Persist the final editor layout.
- Modify: `Assets/Tests/EditMode/SokobanGridTests.cs`
  - Add or update tests for editor validation, serialization, and mechanic links.
- Modify: `tools/verify_builtin_levels.py`
  - Strengthen final-level validation and keep level 28-30 checks explicit.
- Modify: `docs/level_design_bible_30.md`
  - Document final level intent, difficulty curve, and endgame design notes.
- Modify: `docs/level_redesign_notes.md`
  - Record accepted level redesigns and rejected design patterns.
- Modify: `docs/level_verification_summary.md`
  - Update with final verification results.
- Modify: `docs/product_delivery_notes.md`
  - Update final product delivery checklist and editor feature list.

## Acceptance Gates

- Built-in level count remains exactly 30.
- Levels 28-30 use only wall, floor, player, box, and goal tiles unless the user changes direction.
- Final levels are not accepted if their difficulty comes mainly from long walks, oversized empty corridors, or repeated filler pushes.
- Level 28 must become a compact high-pressure pure Sokoban puzzle using target order, temporary parking, box routing, and dead-position avoidance.
- The level editor must feel like a shipped tool: clear palette, clear selected brush, clear validation, clear save/load/test flow, and no confusing hidden mechanic state.
- Every final claim must be backed by verification: structural checks, relevant edit-mode tests, Unity play-mode/manual smoke where needed, and `git diff --check`.

---

### Task 1: Baseline Audit

**Files:**
- Read: `Assets/Editor/LevelSetupUtility.cs`
- Read: `Assets/Scripts/LevelEditor/*.cs`
- Read: `docs/level_verification_summary.md`
- Run: `tools/verify_builtin_levels.py`

- [ ] **Step 1: Confirm repository state**

Run:

```powershell
git status --short
```

Expected: no unrelated dirty files. If dirty files exist, inspect them and preserve user work.

- [ ] **Step 2: Confirm current level inventory**

Run:

```powershell
Get-ChildItem -Path "Assets/Resources/Levels" -Filter "Level_*.asset" | Sort-Object Name | Select-Object Name
```

Expected: `Level_01.asset` through `Level_30.asset` exist.

- [ ] **Step 3: Run current built-in level verification**

Run:

```powershell
python tools/verify_builtin_levels.py
```

Expected: existing solver/structure checks pass for levels already covered by the tool. If pure endgame levels are too large for exhaustive solve, record structural validation separately and avoid claiming solver proof.

- [ ] **Step 4: Capture exact editor gaps**

Inspect:

```powershell
Get-Content -Path "Assets/Scripts/LevelEditor/LevelEditorManager.cs"
Get-Content -Path "Assets/Scripts/LevelEditor/LevelEditorUI.cs"
Get-Content -Path "Assets/Scripts/LevelEditor/LevelValidator.cs"
Get-Content -Path "Assets/Scripts/LevelEditor/LevelSerializer.cs"
```

Expected findings to address: no redo, load only most recent, editor UI is functional but sparse, validation lacks reachability/dead-state guidance, and serializer log text contains mojibake.

### Task 2: Rebuild Level 28 As A Real High-Difficulty Pure Sokoban Puzzle

**Files:**
- Modify: `Assets/Editor/LevelSetupUtility.cs`
- Modify: `Assets/Resources/Levels/Level_28.asset`
- Modify: `docs/level_design_bible_30.md`
- Modify: `docs/level_redesign_notes.md`
- Modify: `docs/level_verification_summary.md`

- [ ] **Step 1: Design against the approved concept**

Use this fixed design target:

```text
Level 28 - Packing Lock
Rules: pure Sokoban only
Scale: compact, about 11x9 or 12x10
Boxes: 5
Goals: 5
Difficulty source: target fill order, temporary parking contention, sealed goal room entry, and irreversible wall-side dead positions
Rejected patterns: long corridors, large empty halls, repeated straight-line pushes, extra walking used as difficulty
```

- [ ] **Step 2: Produce at least two candidate maps**

For each candidate, record:

```text
Map width and height
Box count
Goal count
Whether any box starts on a goal
Estimated push count
Main tactical idea
Reason accepted or rejected
```

Acceptance: reject any candidate whose solution mostly consists of walking around a large layout.

- [ ] **Step 3: Verify the selected candidate structurally**

Run a local structural check that confirms:

```text
one player
five boxes
five goals
no portal tiles
no ice tiles
no pressure plates
no doors
all tile rows have equal width
```

- [ ] **Step 4: Try solver verification**

Use `tools/verify_builtin_levels.py` helper functions or a small temporary Python harness to solve the candidate.

Expected: either a valid solution is found, or the search limit is documented honestly and the level is marked for user playtest instead of falsely labeled solver-proven.

- [ ] **Step 5: Write the accepted Level 28 definition**

Update the Level 28 entry in `Assets/Editor/LevelSetupUtility.cs` with:

```text
levelName: Level 28 - Packing Lock
parMoves: based on verified or playtested route, not padded
tiles: accepted compact pure Sokoban map
portalPairs: empty
doorLinks: empty
```

- [ ] **Step 6: Regenerate or write `Level_28.asset`**

Use the existing asset generation path or a controlled script that writes the same YAML shape as current `Level_28.asset`.

Expected: `width * height == tiles.Length`, tile values contain only pure Sokoban tile types, and no black/empty unintended cells appear.

- [ ] **Step 7: Update documentation**

Update:

```text
docs/level_design_bible_30.md
docs/level_redesign_notes.md
docs/level_verification_summary.md
```

Expected: Level 28 notes explain why the difficulty is spatial and tactical, not step-padding.

### Task 3: Review And Harden Levels 29-30

**Files:**
- Modify if needed: `Assets/Editor/LevelSetupUtility.cs`
- Modify if needed: `Assets/Resources/Levels/Level_29.asset`
- Modify if needed: `Assets/Resources/Levels/Level_30.asset`
- Modify: `docs/level_design_bible_30.md`
- Modify: `docs/level_redesign_notes.md`
- Modify: `docs/level_verification_summary.md`

- [ ] **Step 1: Re-evaluate Level 29**

Check Level 29 against:

```text
6 boxes
6 goals
pure Sokoban only
meaningful dual-area routing
no difficulty from empty travel distance
no repeated earlier layout language
```

Acceptance: keep only if it has at least two interacting storage regions and a clear consequence for wrong fill order.

- [ ] **Step 2: Re-evaluate Level 30**

Check Level 30 against:

```text
endgame capstone feel
at least 7 boxes or an explicitly justified extra-box design
pure Sokoban only
multiple box groups that must be scheduled, not simply pushed in sequence
central space used as a real exchange hub
```

Acceptance: keep only if it feels like the final exam for pure Sokoban fundamentals.

- [ ] **Step 3: Patch weak levels one at a time**

If either level is weak, redesign it with the same process as Task 2.

Expected: the user tests Level 28 first, then Level 29, then Level 30, unless the user asks for batch delivery.

### Task 4: Upgrade Editor Interaction Model

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`
- Modify: `Assets/Scripts/LevelEditor/EditorGridView.cs`
- Modify: `Assets/Editor/SceneSetupUtility.cs`
- Modify: `Assets/Scenes/LevelEditor.unity`

- [ ] **Step 1: Define product editor layout**

Implement a clear three-zone layout:

```text
left: tile palette and brush mode
center: editable grid
right: level metadata, validation, mechanic pairing state, save/load/test actions
bottom or top: new, resize, undo, redo, clear, back
```

Expected: no overlapping controls at 1920x1080 and no hidden critical state.

- [ ] **Step 2: Improve brush feedback**

Add visible selected-brush state for:

```text
Wall
Floor
Player
Box
Goal
BoxOnGoal
Ice
Portal
PressurePlate/Door link tool
Erase
```

Expected: the user can always tell which tile will be placed next.

- [ ] **Step 3: Add redo**

Extend `LevelEditorManager` from undo-only to undo/redo:

```text
Ctrl+Z: undo
Ctrl+Y: redo
new paint action after undo: clears redo stack
undo/redo restores tiles, metadata, portal pairs, door links, and pending pairing state
```

- [ ] **Step 4: Add safer resize**

Change resize behavior from always creating a blank level to:

```text
resize preserves overlapping existing tiles
new cells become floor inside the border
outer border becomes wall
mechanic links outside the new bounds are removed
invalid pending links are cleared
```

- [ ] **Step 5: Add clear/fill commands**

Add commands:

```text
Clear Interior: keeps border walls and resets interior to floor
Wall Border: rebuilds outer border as wall
Validate Now: runs validation immediately
```

Expected: each command pushes undo.

### Task 5: Upgrade Editor Validation

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelValidator.cs`
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`
- Modify: `Assets/Tests/EditMode/SokobanGridTests.cs`

- [ ] **Step 1: Keep current hard validation**

Required existing rules stay hard failures:

```text
exactly one player
at least one box
box count equals goal count
every portal belongs to one pair
every door is linked
every pressure plate controls at least one door
```

- [ ] **Step 2: Add reachability validation**

Add checks:

```text
player starting area can reach at least one side of every non-walled gameplay region
goals are not isolated behind walls without any route
boxes are not placed inside sealed single-cell pockets unless already on a goal
```

Expected: hard-fail obviously unreachable or malformed maps.

- [ ] **Step 3: Add warning-level design hints**

Extend validation result to include warnings for:

```text
box in corner not on a goal
box count greater than 6 for custom levels without warning
no par move set beyond default
portal pair exists but neither player nor box can reasonably reach it
door exists but plate is unreachable
```

Expected: warnings do not block saving unless they indicate invalid data.

- [ ] **Step 4: Show validation as a panel**

UI should show:

```text
Player count
Box count
Goal count
Portal pair count
Door link count
Blocking error
Warnings list
Save/Test enabled state
```

Expected: invalid level status is readable without opening the console.

### Task 6: Upgrade Save, Load, And Test-Play Flow

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelSerializer.cs`
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`
- Modify: `Assets/Scripts/Managers/LevelManager.cs`
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`

- [ ] **Step 1: Fix serializer logging**

Replace garbled save log text with clear English or Chinese text.

Expected log:

```text
[LevelSerializer] Saved custom level: <path>
```

- [ ] **Step 2: Add custom level list loading**

Expose all saved JSON custom levels instead of only loading the most recent one.

Expected behavior:

```text
Load button opens/selects from saved custom levels
most recent level can still be loaded quickly
invalid JSON is skipped with a readable warning
```

- [ ] **Step 3: Improve save naming**

Use stable file names:

```text
existing levelId: overwrite same JSON
empty levelId: create an 8-character GUID id
level name changes: do not break the id
```

- [ ] **Step 4: Test-play return**

Ensure test play from editor returns to the editor, not the normal level-select flow.

Expected:

```text
Editor -> Test Play -> Gameplay -> Complete/Back/Restart path keeps test context
Back to Editor button is available in test mode
```

### Task 7: Product-Level QA And Documentation

**Files:**
- Modify: `docs/product_delivery_notes.md`
- Modify: `docs/level_design_bible_30.md`
- Modify: `docs/level_verification_summary.md`
- Modify: `README.md`

- [ ] **Step 1: Run code formatting/error checks**

Run:

```powershell
git diff --check
```

Expected: no whitespace or patch formatting errors.

- [ ] **Step 2: Run Python level checks**

Run:

```powershell
python tools/verify_builtin_levels.py
```

Expected: no regressions. Any solver limit for pure endgame levels must be stated clearly.

- [ ] **Step 3: Run Unity EditMode tests**

Run through Unity Test Runner or the available Unity CLI path.

Expected: all EditMode tests pass, especially movement, portal, ice, door, serializer, and validator tests.

- [ ] **Step 4: Manual smoke test scenes**

In Unity, test:

```text
MainMenu -> LevelSelect -> Level 28
Level 28 restart/undo/complete if solution known
LevelEditor new level
LevelEditor paint each tile type
Portal pair creation and removal
Door link creation and removal
Save custom level
Load custom level
Test play custom level
Return from test play to editor
```

Expected: no console errors, no UI overlap, no invisible black tiles, no lost mechanic links.

- [ ] **Step 5: Final commit**

After verification:

```powershell
git status --short
git add Assets docs tools README.md
git commit -m "feat: finish sokoban product submission"
```

Expected: one clean commit containing final levels, editor polish, tests, and docs.

## Tomorrow Start Order

1. Start with Level 28, because it has explicit user feedback and a clear approved direction.
2. Let the user test Level 28 before touching 29-30 unless the user asks for a batch.
3. After final levels are accepted, switch to editor implementation.
4. Treat editor work as the capstone feature, not as a side panel.
5. End with Unity smoke testing, documentation, and a final save/commit.
