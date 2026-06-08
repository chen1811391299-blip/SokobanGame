# Level Editor Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the in-game Sokoban level editor to production quality — adding redo, safe resize, BFS validation, warnings, a full B-layout UI with brush highlights and level list panel, and a Custom tab in Level Select.

**Architecture:** Inspector-driven LevelEditorUI wired by AllSceneBuilder; LevelEditorManager stays as the single source of truth for level state; LevelValidator and LevelSerializer are pure static classes extended in-place; LevelSelectUI adds a 6th act tab with a special code-path for custom levels.

**Tech Stack:** Unity 2D, C# 10, TextMeshPro, UnityEngine.UI, NUnit EditMode tests, JsonUtility + StreamingAssets.

---

## File Map

| File | Change |
|------|--------|
| `Assets/Scripts/LevelEditor/LevelValidator.cs` | Add `Warnings[]` to `ValidationResult`; add BFS reachability; add 3 warning checks |
| `Assets/Tests/EditMode/LevelValidatorTests.cs` | **New** — NUnit tests for validator changes |
| `Assets/Scripts/LevelEditor/LevelSerializer.cs` | Add `Delete()`, `GetAllLevelMeta()`; fix Chinese log |
| `Assets/Scripts/LevelEditor/LevelEditorManager.cs` | Add `_redoStack`, `RedoLastAction()`, `IsEraseMode`; make `UndoLastAction()` public; add `ResizeLevel()`, `ClearInterior()` |
| `Assets/Scripts/LevelEditor/EditorGridView.cs` | Respect `IsEraseMode` on left-click |
| `Assets/Scripts/LevelEditor/LevelEditorUI.cs` | New inspector fields; B-layout wiring; brush highlight; level list panel; validation with warnings + hint |
| `Assets/Scripts/UI/LevelSelectUI.cs` | Add Custom tab (6th act); fix act assignment; custom card variant; empty-state label |
| `Assets/Editor/AllSceneBuilder.cs` | Rebuild `BuildLevelEditor()` for B-layout: top bar, bottom brush bar, expanded right panel |

---

## Task 1: LevelValidator — Warnings + BFS Reachability

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelValidator.cs`
- Create: `Assets/Tests/EditMode/LevelValidatorTests.cs`

- [ ] **Step 1.1: Add `Warnings` to `ValidationResult` struct**

Replace the struct at the top of `LevelValidator.cs`:

```csharp
public struct ValidationResult
{
    public bool IsValid;
    public int PlayerCount;
    public int BoxCount;
    public int GoalCount;
    public bool PortalsPaired;
    public bool DoorsLinked;
    public string ErrorMessage;
    public string[] Warnings;   // new — never null after Validate()
}
```

In `Validate()`, change the initial result construction to initialise `Warnings`:

```csharp
var r = new ValidationResult
{
    IsValid = true,
    PortalsPaired = true,
    DoorsLinked = true,
    Warnings = System.Array.Empty<string>()
};
```

- [ ] **Step 1.2: Add helper methods for BFS and warnings**

Add these private static methods to `LevelValidator` (before the existing `Invalid` helper):

```csharp
private static bool IsPassable(TileType t) =>
    t != TileType.Wall && t != TileType.Empty;

private static bool IsWallOrOutOfBounds(LevelData data, int x, int y) =>
    !data.IsInBounds(x, y) || data.GetTile(x, y) == TileType.Wall;

private static bool IsCornerBox(LevelData data, int x, int y)
{
    bool left  = IsWallOrOutOfBounds(data, x - 1, y);
    bool right = IsWallOrOutOfBounds(data, x + 1, y);
    bool down  = IsWallOrOutOfBounds(data, x, y - 1);
    bool up    = IsWallOrOutOfBounds(data, x, y + 1);
    return (left || right) && (down || up);
}

private static System.Collections.Generic.HashSet<Vector2Int> BfsFromPlayer(LevelData data)
{
    var visited = new System.Collections.Generic.HashSet<Vector2Int>();
    Vector2Int? start = null;
    for (int y = 0; y < data.height && start == null; y++)
    for (int x = 0; x < data.width  && start == null; x++)
        if (data.GetTile(x, y) == TileType.Player)
            start = new Vector2Int(x, y);

    if (start == null) return visited;

    var queue = new System.Collections.Generic.Queue<Vector2Int>();
    queue.Enqueue(start.Value);
    visited.Add(start.Value);

    var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    while (queue.Count > 0)
    {
        var cur = queue.Dequeue();
        foreach (var d in dirs)
        {
            var next = cur + d;
            if (!data.IsInBounds(next.x, next.y)) continue;
            if (visited.Contains(next)) continue;
            if (!IsPassable(data.GetTile(next.x, next.y))) continue;
            visited.Add(next);
            queue.Enqueue(next);
        }
    }
    return visited;
}
```

- [ ] **Step 1.3: Insert BFS reachability check and warning generation into `Validate()`**

After the `ValidateDoors` block (before `return r;`) add:

```csharp
// BFS reachability — hard error
var reachable = BfsFromPlayer(data);
for (int y = 0; y < data.height; y++)
for (int x = 0; x < data.width; x++)
{
    var tile = data.GetTile(x, y);
    if (tile == TileType.Wall || tile == TileType.Empty) continue;
    if (!reachable.Contains(new Vector2Int(x, y)))
        return Invalid(r, "Unreachable tiles detected. All floor areas must connect to the player start.");
}

// Warnings (do not set IsValid = false)
var warnings = new System.Collections.Generic.List<string>();

for (int y = 0; y < data.height; y++)
for (int x = 0; x < data.width; x++)
{
    if (data.GetTile(x, y) == TileType.Box && IsCornerBox(data, x, y))
        warnings.Add($"Box at ({x},{y}) is in a corner and cannot be pushed to a goal.");
}

if (r.BoxCount > 6)
    warnings.Add($"High box count ({r.BoxCount}). Custom levels with many boxes may be very hard to solve.");

if (data.parMoves == 20)
    warnings.Add("Par moves is still the default (20). Consider setting a real target.");

r.Warnings = warnings.ToArray();
return r;
```

- [ ] **Step 1.4: Write failing tests**

Create `Assets/Tests/EditMode/LevelValidatorTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class LevelValidatorTests
{
    // 5×5 level: wall border, player at (1,1), box at (2,1), goal at (3,1)
    private static LevelData MakeValid()
    {
        var d = ScriptableObject.CreateInstance<LevelData>();
        d.width = 5; d.height = 5;
        d.tiles = new int[25];
        for (int y = 0; y < 5; y++)
        for (int x = 0; x < 5; x++)
        {
            bool border = x == 0 || x == 4 || y == 0 || y == 4;
            d.SetTile(x, y, border ? TileType.Wall : TileType.Floor);
        }
        d.SetTile(1, 1, TileType.Player);
        d.SetTile(2, 1, TileType.Box);
        d.SetTile(3, 1, TileType.Goal);
        d.parMoves = 5;
        d.portalPairs = new PortalPair[0];
        d.doorLinks = new DoorLink[0];
        return d;
    }

    [Test]
    public void Valid_Level_Passes()
    {
        var r = LevelValidator.Validate(MakeValid());
        Assert.IsTrue(r.IsValid);
        Assert.IsEmpty(r.Warnings);
    }

    [Test]
    public void Warnings_DefaultPar_Reported()
    {
        var d = MakeValid();
        d.parMoves = 20;
        var r = LevelValidator.Validate(d);
        Assert.IsTrue(r.IsValid);
        Assert.IsNotEmpty(r.Warnings);
        StringAssert.Contains("default (20)", r.Warnings[0]);
    }

    [Test]
    public void Warnings_CornerBox_Reported()
    {
        var d = MakeValid();
        // Move box to corner position (1,1) — surrounded by walls on left and below
        d.SetTile(1, 1, TileType.Floor);
        d.SetTile(2, 1, TileType.Floor);
        d.SetTile(1, 2, TileType.Player);
        d.SetTile(2, 3, TileType.Goal);
        d.SetTile(1, 1, TileType.Box); // (1,1) is cornered: wall left (x=0) and wall below (y=0)
        var r = LevelValidator.Validate(d);
        Assert.IsTrue(r.IsValid);
        bool hasCornerWarn = System.Array.Exists(r.Warnings, w => w.Contains("corner"));
        Assert.IsTrue(hasCornerWarn);
    }

    [Test]
    public void Error_Unreachable_Floor_IsInvalid()
    {
        var d = MakeValid();
        // Seal off the goal with walls — it becomes unreachable
        d.SetTile(3, 1, TileType.Wall); // replace goal with wall (box count != goal now)
        d.SetTile(3, 3, TileType.Goal);  // put goal in isolated pocket
        // Surround (3,3) with walls to make it unreachable pocket
        d.SetTile(2, 3, TileType.Wall);
        d.SetTile(3, 2, TileType.Wall);
        // (3,3) is boxed in — unreachable from player
        var r = LevelValidator.Validate(d);
        Assert.IsFalse(r.IsValid);
        StringAssert.Contains("Unreachable", r.ErrorMessage);
    }
}
```

- [ ] **Step 1.5: Run tests in Unity Test Runner — verify they compile and the new tests fail**

Open Unity → Window → General → Test Runner → EditMode → Run All. The new tests should either compile-fail (if code not yet written) or show failures. After step 1.3 is complete, re-run and expect all 4 tests to pass.

- [ ] **Step 1.6: Commit**

```
git add Assets/Scripts/LevelEditor/LevelValidator.cs Assets/Tests/EditMode/LevelValidatorTests.cs
git commit -m "feat: add BFS reachability check and warning-level hints to LevelValidator"
```

---

## Task 2: LevelSerializer — GetAllLevelMeta, Delete, Log Fix

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelSerializer.cs`

- [ ] **Step 2.1: Fix Chinese log message**

In `LevelSerializer.Save()`, change:

```csharp
Debug.Log($"[LevelSerializer] 已保存: {path}");
```
to:
```csharp
Debug.Log($"[LevelSerializer] Saved: {path}");
```

- [ ] **Step 2.2: Add `Delete()` and `GetAllLevelMeta()`**

Add these two methods to the `LevelSerializer` class (after `GetAllLevelIds()`):

```csharp
public static void Delete(string levelId)
{
    var path = Path.Combine(LevelsDir, $"{levelId}.json");
    if (File.Exists(path))
        File.Delete(path);
}

public static (string id, string name, System.DateTime modified)[] GetAllLevelMeta()
{
    if (!Directory.Exists(LevelsDir))
        return System.Array.Empty<(string, string, System.DateTime)>();

    var files = Directory.GetFiles(LevelsDir, "*.json");
    var result = new System.Collections.Generic.List<(string, string, System.DateTime)>();
    foreach (var file in files)
    {
        try
        {
            var id       = Path.GetFileNameWithoutExtension(file);
            var json     = File.ReadAllText(file);
            var dto      = JsonUtility.FromJson<LevelDataJson>(json);
            var modified = File.GetLastWriteTime(file);
            result.Add((id, dto?.levelName ?? id, modified));
        }
        catch { /* skip malformed files */ }
    }
    result.Sort((a, b) => b.modified.CompareTo(a.modified)); // newest first
    return result.ToArray();
}
```

Note: the tuple field names (`id`, `name`, `modified`) require C# 7.1+. If the project targets an older version, use positional access `Item1`, `Item2`, `Item3` in callers.

- [ ] **Step 2.3: Verify compilation in Unity**

Save the file, switch to Unity and wait for recompile. Check Console for errors. No tests for file I/O here — manual verification in step 5.7 when the level list panel is wired up.

- [ ] **Step 2.4: Commit**

```
git add Assets/Scripts/LevelEditor/LevelSerializer.cs
git commit -m "feat: add LevelSerializer.Delete and GetAllLevelMeta; fix log encoding"
```

---

## Task 3: LevelEditorManager — Redo Stack

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`

- [ ] **Step 3.1: Add `_redoStack` field and `IsEraseMode` property**

After the `_undoStack` declaration, add:

```csharp
private readonly List<EditorSnapshot> _redoStack = new();
public bool IsEraseMode { get; set; } = false;
```

- [ ] **Step 3.2: Update `PushUndo()` to clear redo stack on new actions**

Replace the existing `PushUndo()`:

```csharp
private void PushUndo()
{
    if (editingLevel == null) return;
    if (_undoStack.Count >= MaxUndo)
        _undoStack.RemoveAt(0);
    _undoStack.Add(CaptureSnapshot());
    _redoStack.Clear();
}
```

- [ ] **Step 3.3: Make `UndoLastAction()` public and push to redo before restoring**

Replace the existing `private void UndoLastAction()`:

```csharp
public void UndoLastAction()
{
    if (_undoStack.Count == 0) return;
    if (_redoStack.Count >= MaxUndo) _redoStack.RemoveAt(0);
    _redoStack.Add(CaptureSnapshot());
    var snapshot = _undoStack[^1];
    _undoStack.RemoveAt(_undoStack.Count - 1);
    RestoreSnapshot(snapshot);
    RebuildView();
    GetComponent<LevelEditorUI>()?.SyncFromLevel();
}
```

- [ ] **Step 3.4: Add `RedoLastAction()` method**

Add after `UndoLastAction()`:

```csharp
public void RedoLastAction()
{
    if (_redoStack.Count == 0) return;
    if (_undoStack.Count >= MaxUndo) _undoStack.RemoveAt(0);
    _undoStack.Add(CaptureSnapshot());
    var snapshot = _redoStack[^1];
    _redoStack.RemoveAt(_redoStack.Count - 1);
    RestoreSnapshot(snapshot);
    RebuildView();
    GetComponent<LevelEditorUI>()?.SyncFromLevel();
}
```

- [ ] **Step 3.5: Wire Ctrl+Y in `Update()`**

Replace the existing `Update()`:

```csharp
void Update()
{
    bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    if (ctrl && Input.GetKeyDown(KeyCode.Z)) UndoLastAction();
    if (ctrl && Input.GetKeyDown(KeyCode.Y)) RedoLastAction();
}
```

- [ ] **Step 3.6: Verify compilation. Commit.**

```
git add Assets/Scripts/LevelEditor/LevelEditorManager.cs
git commit -m "feat: add redo stack and IsEraseMode to LevelEditorManager"
```

---

## Task 4: LevelEditorManager — ResizeLevel + ClearInterior

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`

- [ ] **Step 4.1: Add `ResizeLevel()` method**

Add after `NewLevel()`:

```csharp
public void ResizeLevel(int newW, int newH)
{
    newW = Mathf.Clamp(newW, 3, 20);
    newH = Mathf.Clamp(newH, 3, 20);
    PushUndo();

    int oldW = editingLevel.width;
    int oldH = editingLevel.height;
    var newTiles = new int[newW * newH];

    for (int y = 0; y < newH; y++)
    for (int x = 0; x < newW; x++)
    {
        if (x < oldW && y < oldH)
        {
            newTiles[y * newW + x] = editingLevel.tiles[y * oldW + x];
        }
        else
        {
            bool border = x == 0 || x == newW - 1 || y == 0 || y == newH - 1;
            newTiles[y * newW + x] = (int)(border ? TileType.Wall : TileType.Floor);
        }
    }

    editingLevel.width  = newW;
    editingLevel.height = newH;
    editingLevel.tiles  = newTiles;

    _portalPairs.RemoveAll(p =>
        !editingLevel.IsInBounds(p.posA.x, p.posA.y) ||
        !editingLevel.IsInBounds(p.posB.x, p.posB.y));
    _doorLinks.RemoveAll(d =>
        !editingLevel.IsInBounds(d.platePos.x, d.platePos.y) ||
        !editingLevel.IsInBounds(d.doorPos.x,  d.doorPos.y));

    if (_pendingPortalPos.HasValue &&
        !editingLevel.IsInBounds(_pendingPortalPos.Value.x, _pendingPortalPos.Value.y))
        _pendingPortalPos = null;

    if (_pendingPlatePos.HasValue &&
        !editingLevel.IsInBounds(_pendingPlatePos.Value.x, _pendingPlatePos.Value.y))
        _pendingPlatePos = null;

    ApplyMechanicData();
    RebuildView();
    GetComponent<LevelEditorUI>()?.SyncFromLevel();
}
```

- [ ] **Step 4.2: Add `ClearInterior()` method**

Add after `ResizeLevel()`:

```csharp
public void ClearInterior()
{
    PushUndo();
    int w = editingLevel.width;
    int h = editingLevel.height;
    for (int y = 1; y < h - 1; y++)
    for (int x = 1; x < w - 1; x++)
        editingLevel.SetTile(x, y, TileType.Floor);

    _portalPairs.Clear();
    _doorLinks.Clear();
    _pendingPortalPos = null;
    _pendingPlatePos  = null;
    _nextPortalColor  = 0;
    ApplyMechanicData();
    RebuildView();
}
```

- [ ] **Step 4.3: Update `LevelEditorUI.btnResize` listener to call `ResizeLevel` instead of `NewLevel`**

In `LevelEditorUI.Start()`, the resize button currently calls `_editor?.NewLevel(...)`. This is handled by AllSceneBuilder in Task 7. No code change needed here — the wiring in LevelEditorUI will be updated in Task 5.

- [ ] **Step 4.4: Verify compilation. Commit.**

```
git add Assets/Scripts/LevelEditor/LevelEditorManager.cs
git commit -m "feat: add ResizeLevel (content-preserving) and ClearInterior to LevelEditorManager"
```

---

## Task 5: EditorGridView — Respect IsEraseMode

**Files:**
- Modify: `Assets/Scripts/LevelEditor/EditorGridView.cs`

- [ ] **Step 5.1: Update `HandleInput()` to check `IsEraseMode`**

Replace the existing `HandleInput()` method:

```csharp
private void HandleInput(PointerEventData e)
{
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _gridRect, e.position, e.pressEventCamera, out var local)) return;

    var rect = _gridRect.rect;
    int x = Mathf.FloorToInt((local.x - rect.xMin) / tileSize);
    int y = Mathf.FloorToInt((local.y - rect.yMin) / tileSize);
    if (_data == null || !_data.IsInBounds(x, y)) return;

    bool erase = e.button == PointerEventData.InputButton.Right
                 || (LevelEditorManager.Instance?.IsEraseMode ?? false);
    if (erase)
        LevelEditorManager.Instance?.EraseTile(x, y);
    else
        LevelEditorManager.Instance?.PlaceTile(x, y);
}
```

- [ ] **Step 5.2: Verify compilation. Commit.**

```
git add Assets/Scripts/LevelEditor/EditorGridView.cs
git commit -m "feat: EditorGridView respects IsEraseMode for left-click erase brush"
```

---

## Task 6: LevelEditorUI — B-Layout Rebuild

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`

This task replaces the entire LevelEditorUI script with the new inspector field set and B-layout logic. The new script keeps the same class name and MonoBehaviour interface.

- [ ] **Step 6.1: Replace LevelEditorUI.cs with the full rewrite**

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditorUI : MonoBehaviour
{
    [Header("Toolbar")]
    public Button btnBack;
    public Button btnNew;
    public Button btnUndo;
    public Button btnRedo;
    public Button btnClear;
    public Button btnSave;
    public Button btnTestPlay;
    public TMP_InputField levelNameInput;
    public TMP_InputField parMovesInput;

    [Header("Resize")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button btnResize;

    [Header("Brush Bar")]
    public Button[] brushButtons;   // indices 0-11 = TileType; 12 = Erase

    [Header("Validation Panel")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI boxCountText;
    public TextMeshProUGUI goalCountText;
    public TextMeshProUGUI portalStatusText;
    public TextMeshProUGUI doorStatusText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI warningsText;
    public TextMeshProUGUI hintText;

    [Header("Level List Panel")]
    public Transform       levelListContent;
    public Button          btnLoadSelected;
    public Button          btnDelete;

    private LevelEditorManager _editor;
    private Color[] _brushOriginalColors;
    private int     _selectedBrushIdx = 1;      // default: Wall
    private string  _selectedLevelId  = null;
    private const int EraseBrushIdx   = 12;

    void Start()
    {
        _editor = LevelEditorManager.Instance;

        btnBack?.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            SceneTransition.LoadScene("MainMenu");
        });

        btnNew?.onClick.AddListener(() => _editor?.NewLevel(8, 8));

        btnUndo?.onClick.AddListener(() => _editor?.UndoLastAction());
        btnRedo?.onClick.AddListener(() => _editor?.RedoLastAction());
        btnClear?.onClick.AddListener(() => _editor?.ClearInterior());

        btnSave?.onClick.AddListener(() =>
        {
            SyncInputsToLevel();
            _editor?.Save();
            RefreshLevelList();
        });

        btnTestPlay?.onClick.AddListener(() =>
        {
            SyncInputsToLevel();
            _editor?.TestPlay();
        });

        btnResize?.onClick.AddListener(() =>
        {
            if (int.TryParse(widthInput?.text, out int w) &&
                int.TryParse(heightInput?.text, out int h))
                _editor?.ResizeLevel(w, h);
        });

        btnLoadSelected?.onClick.AddListener(OnLoadSelected);
        btnDelete?.onClick.AddListener(OnDeleteSelected);

        // Cache brush button original colors for highlight restore
        if (brushButtons != null)
        {
            _brushOriginalColors = new Color[brushButtons.Length];
            for (int i = 0; i < brushButtons.Length; i++)
            {
                var img = brushButtons[i]?.GetComponent<Image>();
                _brushOriginalColors[i] = img ? img.color : Color.gray;
            }

            for (int i = 0; i < brushButtons.Length; i++)
            {
                int idx = i;
                brushButtons[i]?.onClick.AddListener(() => SelectBrush(idx));
            }
        }

        SyncFromLevel();
        RefreshLevelList();
        SelectBrush(_selectedBrushIdx);
        InvokeRepeating(nameof(RefreshValidation), 0f, 0.3f);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SyncFromLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput) levelNameInput.text = level.levelName;
        if (parMovesInput)  parMovesInput.text  = Mathf.Max(1, level.parMoves).ToString();
        if (widthInput)     widthInput.text     = level.width.ToString();
        if (heightInput)    heightInput.text    = level.height.ToString();
    }

    public void ShowValidationError(string msg)
    {
        if (errorText) errorText.text = msg;
    }

    public void RefreshLevelList()
    {
        if (levelListContent == null) return;

        foreach (Transform child in levelListContent)
            Destroy(child.gameObject);

        _selectedLevelId = null;
        UpdateListButtons();

        var metas = LevelSerializer.GetAllLevelMeta();
        foreach (var (id, name, modified) in metas)
        {
            string capturedId = id;
            var entryGo = new GameObject($"Entry_{id}");
            entryGo.transform.SetParent(levelListContent, false);

            var rt = entryGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 36f);

            var img = entryGo.AddComponent<Image>();
            img.color = new Color(0.15f, 0.18f, 0.25f);

            var btn = entryGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => SelectListEntry(capturedId, img));

            var label = new GameObject("Label");
            label.transform.SetParent(entryGo.transform, false);
            var labelRt = label.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(6, 2);
            labelRt.offsetMax = new Vector2(-4, -2);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text      = $"{name}\n<size=10><color=#aaaaaa>{modified:yyyy-MM-dd}</color></size>";
            tmp.fontSize  = 12;
            tmp.color     = Color.white;
            tmp.raycastTarget = false;
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void SyncInputsToLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput)
            level.levelName = string.IsNullOrWhiteSpace(levelNameInput.text)
                ? "New Level" : levelNameInput.text.Trim();

        if (parMovesInput && int.TryParse(parMovesInput.text, out int par))
            level.parMoves = Mathf.Max(1, par);
    }

    private void SelectBrush(int idx)
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor == null || brushButtons == null) return;

        // Restore previous button colour
        if (_selectedBrushIdx < brushButtons.Length)
        {
            var prevImg = brushButtons[_selectedBrushIdx]?.GetComponent<Image>();
            if (prevImg && _selectedBrushIdx < _brushOriginalColors?.Length)
                prevImg.color = _brushOriginalColors[_selectedBrushIdx];
        }

        _selectedBrushIdx = idx;

        if (idx == EraseBrushIdx)
        {
            _editor.IsEraseMode = true;
        }
        else
        {
            _editor.IsEraseMode  = false;
            _editor.SelectedBrush = (TileType)idx;
        }

        // Highlight selected button
        if (idx < brushButtons.Length)
        {
            var img = brushButtons[idx]?.GetComponent<Image>();
            if (img) img.color = Color.white;
        }
    }

    private void SelectListEntry(string id, Image img)
    {
        // Deselect all
        if (levelListContent != null)
            foreach (Transform child in levelListContent)
            {
                var childImg = child.GetComponent<Image>();
                if (childImg) childImg.color = new Color(0.15f, 0.18f, 0.25f);
            }

        _selectedLevelId = id;
        if (img) img.color = new Color(0.22f, 0.45f, 0.88f);
        UpdateListButtons();
    }

    private void OnLoadSelected()
    {
        if (string.IsNullOrEmpty(_selectedLevelId)) return;
        var data = LevelSerializer.Load(_selectedLevelId);
        if (data != null)
        {
            _editor?.LoadLevelForEditing(data);
            SyncFromLevel();
        }
    }

    private void OnDeleteSelected()
    {
        if (string.IsNullOrEmpty(_selectedLevelId)) return;
        LevelSerializer.Delete(_selectedLevelId);
        _selectedLevelId = null;
        RefreshLevelList();
    }

    private void UpdateListButtons()
    {
        bool hasSelection = !string.IsNullOrEmpty(_selectedLevelId);
        if (btnLoadSelected) btnLoadSelected.interactable = hasSelection;
        if (btnDelete)       btnDelete.interactable       = hasSelection;
    }

    private void RefreshValidation()
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor?.editingLevel == null) return;

        // Check for mechanic-pairing hint (takes priority over normal validation display)
        string hint = _editor.GetPairingHint();
        if (hintText) hintText.text = hint ?? "";

        var v = _editor.GetValidation();

        if (playerCountText) playerCountText.text = $"{(v.PlayerCount == 1 ? "✓" : "✗")} Player: {v.PlayerCount}";
        if (boxCountText)    boxCountText.text    = $"{(v.BoxCount > 0 && v.BoxCount == v.GoalCount ? "✓" : "✗")} Boxes: {v.BoxCount}";
        if (goalCountText)   goalCountText.text   = $"{(v.GoalCount > 0 && v.BoxCount == v.GoalCount ? "✓" : "✗")} Goals: {v.GoalCount}";
        if (portalStatusText) portalStatusText.text = v.PortalsPaired ? "✓ Portals OK" : "✗ Portals unpaired";
        if (doorStatusText)   doorStatusText.text   = v.DoorsLinked   ? "✓ Doors OK"   : "✗ Doors unlinked";

        if (errorText)
        {
            errorText.text  = v.IsValid ? "" : v.ErrorMessage;
            errorText.color = Color.red;
        }

        if (warningsText)
        {
            warningsText.text  = v.Warnings != null && v.Warnings.Length > 0
                ? string.Join("\n", v.Warnings)
                : "";
            warningsText.color = new Color(1f, 0.75f, 0.1f);
        }

        bool canAct = v.IsValid;
        if (btnSave)     btnSave.interactable     = canAct;
        if (btnTestPlay) btnTestPlay.interactable = canAct;
    }
}
```

- [ ] **Step 6.2: Add `GetPairingHint()` to `LevelEditorManager`**

`LevelEditorUI.RefreshValidation()` calls `_editor.GetPairingHint()`. Add this method to `LevelEditorManager`:

```csharp
public string GetPairingHint()
{
    if (_pendingPortalPos.HasValue)
        return "Click a second Portal tile to complete the pair.";
    if (_pendingPlatePos.HasValue)
        return "Click a Door tile to link it to the pressure plate.";
    return null;
}
```

- [ ] **Step 6.3: Verify compilation. Commit.**

```
git add Assets/Scripts/LevelEditor/LevelEditorUI.cs Assets/Scripts/LevelEditor/LevelEditorManager.cs
git commit -m "feat: rebuild LevelEditorUI for B-layout with brush highlight, level list, and warnings"
```

---

## Task 7: AllSceneBuilder — Rebuild LevelEditor Scene

**Files:**
- Modify: `Assets/Editor/AllSceneBuilder.cs`

This task replaces `BuildLevelEditor()` entirely to produce the B-layout scene and wire all new inspector fields.

- [ ] **Step 7.1: Replace `BuildLevelEditor()` in AllSceneBuilder.cs**

Find the `static void BuildLevelEditor()` method (starts around line 256) and replace everything from its opening brace to its closing `Save(scene, "LevelEditor");` line with the following:

```csharp
static void BuildLevelEditor()
{
    var scene = EditorSceneManager.OpenScene("Assets/Scenes/LevelEditor.unity", OpenSceneMode.Single);
    ClearScene();
    Cam2D();
    ES();

    var cvs = Canvas();

    // ── Top Toolbar (height 55px) ─────────────────────────────────────────
    var toolbar = new GameObject("Toolbar");
    toolbar.transform.SetParent(cvs.transform, false);
    var tbRT = toolbar.AddComponent<RectTransform>();
    tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
    tbRT.offsetMin = new Vector2(0, -55); tbRT.offsetMax = Vector2.zero;
    toolbar.AddComponent<Image>().color = new Color(.13f, .13f, .18f, 1f);

    // Row 1 of toolbar: Back | New | Undo | Redo | Clear | [spacer] | Save | Test
    var btnBack   = BtnAbs(toolbar.transform, "BtnBack",     "Back",     new Vector2(5,   -7.5f), new Vector2(72,  40));
    btnBack.GetComponent<Image>().color = new Color(.55f, .20f, .20f);
    var btnNew    = BtnAbs(toolbar.transform, "BtnNew",      "New",      new Vector2(82,  -7.5f), new Vector2(72,  40));
    var btnUndo   = BtnAbs(toolbar.transform, "BtnUndo",     "Undo",     new Vector2(159, -7.5f), new Vector2(72,  40));
    var btnRedo   = BtnAbs(toolbar.transform, "BtnRedo",     "Redo",     new Vector2(236, -7.5f), new Vector2(72,  40));
    var btnClear  = BtnAbs(toolbar.transform, "BtnClear",    "Clear",    new Vector2(313, -7.5f), new Vector2(72,  40));
    btnClear.GetComponent<Image>().color = new Color(.50f, .35f, .10f);

    var nameIn = Input_(toolbar.transform, "LevelNameInput", new Vector2(400, -7.5f), new Vector2(200, 40));
    var parLbl = BtnAbs(toolbar.transform,  "ParLabel",      "Par",      new Vector2(607, -7.5f), new Vector2(36,  40));
    parLbl.GetComponent<Image>().color = new Color(.18f, .18f, .24f);
    parLbl.GetComponent<Button>().interactable = false;
    var parIn  = Input_(toolbar.transform, "ParMovesInput",  new Vector2(647, -7.5f), new Vector2(55,  40));
    parIn.GetComponent<TMP_InputField>().text = "20";

    var wLbl   = BtnAbs(toolbar.transform, "WLabel",   "W",    new Vector2(715, -7.5f), new Vector2(22, 40));
    wLbl.GetComponent<Image>().color = new Color(.18f, .18f, .24f);
    wLbl.GetComponent<Button>().interactable = false;
    var wIn    = Input_(toolbar.transform, "WidthInput",  new Vector2(741, -7.5f), new Vector2(45, 40));
    wIn.GetComponent<TMP_InputField>().text = "8";
    var hLbl   = BtnAbs(toolbar.transform, "HLabel",   "H",    new Vector2(791, -7.5f), new Vector2(22, 40));
    hLbl.GetComponent<Image>().color = new Color(.18f, .18f, .24f);
    hLbl.GetComponent<Button>().interactable = false;
    var hIn    = Input_(toolbar.transform, "HeightInput", new Vector2(817, -7.5f), new Vector2(45, 40));
    hIn.GetComponent<TMP_InputField>().text = "8";
    var btnResize = BtnAbs(toolbar.transform, "BtnResize", "Resize", new Vector2(867, -7.5f), new Vector2(75, 40));

    var btnSave = BtnAbs(toolbar.transform, "BtnSave",    "Save",      new Vector2(955,  -7.5f), new Vector2(85, 40));
    btnSave.GetComponent<Image>().color = new Color(.20f, .48f, .22f);
    var btnTest = BtnAbs(toolbar.transform, "BtnTestPlay", "Test Play", new Vector2(1045, -7.5f), new Vector2(105, 40));
    btnTest.GetComponent<Image>().color = new Color(.18f, .38f, .70f);

    // ── Right Panel (width 180px) — Validation + Level List ───────────────
    var rightPanel = new GameObject("RightPanel");
    rightPanel.transform.SetParent(cvs.transform, false);
    var rpRT = rightPanel.AddComponent<RectTransform>();
    rpRT.anchorMin = new Vector2(1, 0); rpRT.anchorMax = new Vector2(1, 1);
    rpRT.offsetMin = new Vector2(-180, 50); rpRT.offsetMax = new Vector2(0, -55);
    rightPanel.AddComponent<Image>().color = new Color(.10f, .10f, .16f, 1f);

    // Validation section
    float vy = -8f;
    var pcTMP  = ValTMP(rightPanel.transform, "PlayerCount",  "Player: 0",   vy);      vy -= 22f;
    var bcTMP  = ValTMP(rightPanel.transform, "BoxCount",     "Boxes: 0",    vy);      vy -= 22f;
    var gcTMP  = ValTMP(rightPanel.transform, "GoalCount",    "Goals: 0",    vy);      vy -= 22f;
    var prTMP  = ValTMP(rightPanel.transform, "PortalStatus", "Portals OK",  vy);      vy -= 22f;
    var drTMP  = ValTMP(rightPanel.transform, "DoorStatus",   "Doors OK",    vy);      vy -= 22f;
    var errTMP = ValTMP(rightPanel.transform, "ErrorText",    "",            vy, Color.red, 12);  vy -= 30f;
    var wrnTMP = ValTMP(rightPanel.transform, "WarningsText", "",            vy, new Color(1f, .75f, .1f), 11); vy -= 40f;
    var hntTMP = ValTMP(rightPanel.transform, "HintText",     "",            vy, new Color(.5f, .85f, 1f), 11); vy -= 30f;

    // Divider label
    var divLbl = ValTMP(rightPanel.transform, "ListHeader", "── Saved Levels ──", vy, new Color(.5f, .5f, .6f), 10); vy -= 22f;

    // Scrollable level list
    var listViewport = new GameObject("ListViewport");
    listViewport.transform.SetParent(rightPanel.transform, false);
    var lvRT = listViewport.AddComponent<RectTransform>();
    lvRT.anchorMin = new Vector2(0, 0); lvRT.anchorMax = new Vector2(1, 1);
    lvRT.offsetMin = new Vector2(4, 80);    // 80px from bottom for the two buttons
    lvRT.offsetMax = new Vector2(-4, vy);   // vy is current cursor (negative from top)
    listViewport.AddComponent<Image>().color = new Color(.08f, .08f, .12f);
    listViewport.AddComponent<Mask>().showMaskGraphic = false;

    var listContent = new GameObject("ListContent");
    listContent.transform.SetParent(listViewport.transform, false);
    var lcRT = listContent.AddComponent<RectTransform>();
    lcRT.anchorMin = new Vector2(0, 1); lcRT.anchorMax = new Vector2(1, 1);
    lcRT.pivot = new Vector2(0.5f, 1f);
    lcRT.offsetMin = Vector2.zero; lcRT.offsetMax = Vector2.zero;
    var vlg = listContent.AddComponent<VerticalLayoutGroup>();
    vlg.spacing = 2f;
    vlg.childForceExpandWidth  = true;
    vlg.childForceExpandHeight = false;
    vlg.childControlHeight = true;
    listContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    var listScroll = listViewport.AddComponent<ScrollRect>();
    listScroll.content    = lcRT;
    listScroll.viewport   = lvRT;
    listScroll.horizontal = false;
    listScroll.vertical   = true;
    listScroll.movementType = ScrollRect.MovementType.Clamped;
    listScroll.scrollSensitivity = 20f;

    // Load Selected / Delete buttons at the bottom of right panel
    var btnLoad   = BtnAbs(rightPanel.transform, "BtnLoadSelected", "Load Selected",
                           new Vector2(4, -4 + 80 - 40), new Vector2(172, 36));
    btnLoad.GetComponent<Image>().color = new Color(.18f, .40f, .18f);
    var btnDel    = BtnAbs(rightPanel.transform, "BtnDelete", "Delete",
                           new Vector2(4, -4), new Vector2(172, 34));
    btnDel.GetComponent<Image>().color = new Color(.45f, .15f, .15f);

    // ── Bottom Brush Bar (height 50px) ────────────────────────────────────
    var brushBar = new GameObject("BrushBar");
    brushBar.transform.SetParent(cvs.transform, false);
    var bbRT = brushBar.AddComponent<RectTransform>();
    bbRT.anchorMin = new Vector2(0, 0); bbRT.anchorMax = new Vector2(1, 0);
    bbRT.offsetMin = Vector2.zero; bbRT.offsetMax = new Vector2(-180, 50);
    brushBar.AddComponent<Image>().color = new Color(.11f, .11f, .17f, 1f);

    var hlg = brushBar.AddComponent<HorizontalLayoutGroup>();
    hlg.padding = new RectOffset(6, 6, 6, 6);
    hlg.spacing = 4f;
    hlg.childForceExpandWidth  = false;
    hlg.childForceExpandHeight = true;

    string[] blabels = { "Empty", "Wall", "Floor", "Player", "Box", "Goal", "Box+Goal", "Ice", "Plate", "Door(O)", "Door(C)", "Portal", "Erase" };
    Color[] bcolors =
    {
        new Color(.15f,.15f,.15f), new Color(.35f,.35f,.35f), new Color(.55f,.50f,.40f),
        new Color(.10f,.50f,.90f), new Color(.70f,.40f,.10f), new Color(.95f,.85f,.10f),
        new Color(.15f,.72f,.20f), new Color(.60f,.88f,1.0f), new Color(.80f,.25f,.90f),
        new Color(.12f,.80f,.25f), new Color(.80f,.12f,.12f), new Color(.00f,.50f,1.0f),
        new Color(.40f,.40f,.40f)  // Erase
    };

    var brushBtns = new Button[blabels.Length];
    for (int i = 0; i < blabels.Length; i++)
    {
        var bGo = new GameObject($"Brush_{i}");
        bGo.transform.SetParent(brushBar.transform, false);
        var bRT = bGo.AddComponent<RectTransform>();
        bRT.sizeDelta = new Vector2(72f, 38f);
        var bImg = bGo.AddComponent<Image>();
        bImg.color = bcolors[i];
        var bBtn = bGo.AddComponent<Button>();
        bBtn.targetGraphic = bImg;
        CreateButtonLabel(bGo.transform, blabels[i], 12);
        brushBtns[i] = bBtn;
    }

    // ── Center Grid Area ──────────────────────────────────────────────────
    var gridArea = new GameObject("GridArea");
    gridArea.transform.SetParent(cvs.transform, false);
    var gaRT = gridArea.AddComponent<RectTransform>();
    gaRT.anchorMin = new Vector2(0, 0); gaRT.anchorMax = new Vector2(1, 1);
    gaRT.offsetMin = new Vector2(0, 50); gaRT.offsetMax = new Vector2(-180, -55);
    gridArea.AddComponent<Image>().color = new Color(.08f, .08f, .12f, 1f);

    var edView = gridArea.AddComponent<EditorGridView>();
    edView.editorTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Prefab_EditorTile.prefab");
    edView.tileSize = 60f;
    gridArea.AddComponent<LevelEditorManager>();
    var edUI = gridArea.AddComponent<LevelEditorUI>();

    // Wire toolbar buttons
    edUI.btnBack     = btnBack.GetComponent<Button>();
    edUI.btnNew      = btnNew.GetComponent<Button>();
    edUI.btnUndo     = btnUndo.GetComponent<Button>();
    edUI.btnRedo     = btnRedo.GetComponent<Button>();
    edUI.btnClear    = btnClear.GetComponent<Button>();
    edUI.btnSave     = btnSave.GetComponent<Button>();
    edUI.btnTestPlay = btnTest.GetComponent<Button>();
    edUI.levelNameInput = nameIn.GetComponent<TMP_InputField>();
    edUI.parMovesInput  = parIn.GetComponent<TMP_InputField>();
    edUI.widthInput     = wIn.GetComponent<TMP_InputField>();
    edUI.heightInput    = hIn.GetComponent<TMP_InputField>();
    edUI.btnResize      = btnResize.GetComponent<Button>();

    // Wire brush bar
    edUI.brushButtons = brushBtns;

    // Wire validation panel
    edUI.playerCountText  = pcTMP;
    edUI.boxCountText     = bcTMP;
    edUI.goalCountText    = gcTMP;
    edUI.portalStatusText = prTMP;
    edUI.doorStatusText   = drTMP;
    edUI.errorText        = errTMP;
    edUI.warningsText     = wrnTMP;
    edUI.hintText         = hntTMP;

    // Wire level list panel
    edUI.levelListContent = listContent.transform;
    edUI.btnLoadSelected  = btnLoad.GetComponent<Button>();
    edUI.btnDelete        = btnDel.GetComponent<Button>();

    Save(scene, "LevelEditor");
}

// Helper: create a text label child inside a button GO
static void CreateButtonLabel(Transform parent, string text, int fontSize = 14)
{
    var go = new GameObject("Label");
    go.transform.SetParent(parent, false);
    var rt = go.AddComponent<RectTransform>();
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text      = text;
    tmp.fontSize  = fontSize;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.color     = Color.white;
}
```

Note: the `ValTMP` helper already exists in `AllSceneBuilder`; check its signature and adjust the `vy` absolute-Y calls to match. If `ValTMP` uses anchoredPosition on an absolute-anchor helper, the pattern will match the existing code.

- [ ] **Step 7.2: Rebuild the LevelEditor scene via the menu**

In Unity: Tools → Build All Scenes (or whichever menu item calls `AllSceneBuilder`). Alternatively, call `AllSceneBuilder.BuildLevelEditor()` directly via the menu. Check for compile errors, then switch to the LevelEditor scene to visually inspect the layout.

Expected: top toolbar across the full width, bottom brush bar, right panel with validation stats + scrollable level list + Load Selected/Delete buttons, center grid area.

- [ ] **Step 7.3: Play the LevelEditor scene — smoke test**

- Grid appears, clicking places tiles, right-click erases
- Erase brush button in bottom bar switches to erase mode (left-click also erases)
- Undo (Ctrl+Z) works, Redo (Ctrl+Y) works
- Brush highlight: selected brush turns white, previous reverts
- Save → level list refreshes with the saved entry
- Load Selected → grid reloads selected level
- Delete → entry removed from list
- New → 8×8 blank level
- Resize W=10, H=10 → preserves content, new border is wall
- Clear → interior becomes floor, undo restores

- [ ] **Step 7.4: Commit**

```
git add Assets/Editor/AllSceneBuilder.cs Assets/Scenes/LevelEditor.unity
git commit -m "feat: rebuild LevelEditor scene to B-layout with bottom brush bar and level list panel"
```

---

## Task 8: LevelSelectUI — Custom Tab

**Files:**
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`

- [ ] **Step 8.1: Add "Custom" to `ActNames`**

Replace:

```csharp
private static readonly string[] ActNames =
{
    "Simple",
    "Medium",
    "Hard",
    "Remix",
    "Finale"
};
```

with:

```csharp
private static readonly string[] ActNames =
{
    "Simple",
    "Medium",
    "Hard",
    "Remix",
    "Finale",
    "Custom"
};
```

- [ ] **Step 8.2: Fix act assignment in `PopulateLevels()`**

In `PopulateLevels()`, replace the act assignment line:

```csharp
int act = Mathf.Clamp(i / 6, 0, ActNames.Length - 1);
```

with:

```csharp
bool isCustom = data.author != "built-in";
int act = isCustom ? ActNames.Length - 1 : Mathf.Clamp(i / 6, 0, ActNames.Length - 2);
```

- [ ] **Step 8.3: Add custom-card variant — no star display, no lock**

In `CreateLevelCard()`, the current star line is:

```csharp
string starText = unlocked ? StarText(stars) : "LOCKED";
```

Update `CreateLevelCard()` signature to accept `bool isCustom`, and replace star logic:

```csharp
private GameObject CreateLevelCard(Transform parent, int index, LevelData data, bool unlocked, int act, bool isCustom = false)
{
    // ... existing card body creation unchanged ...

    // Star / status row
    string starText;
    if (isCustom)
        starText = data.author ?? "player";
    else
        starText = unlocked ? StarText(stars) : "LOCKED";

    var star = CreateText(go.transform, "Stars", starText, new Vector2(0f, 0f), new Vector2(1f, 0f),
        new Vector2(20f, 18f), new Vector2(-20f, 28f), 14, TextAlignmentOptions.Left);
    star.color = isCustom
        ? new Color(0.60f, 0.85f, 1.00f)
        : (unlocked ? new Color(1f, 0.78f, 0.22f) : new Color(0.62f, 0.65f, 0.72f));

    // Custom levels are always unlocked
    bool canPlay = isCustom || unlocked;
    var button = go.AddComponent<Button>();
    button.targetGraphic = go.GetComponent<Image>();
    button.interactable = canPlay;
    if (canPlay)
    {
        int idx = index;
        button.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            LevelManager.Instance.SetCurrent(idx);
            LevelManager.IsTestMode = false;
            SceneTransition.LoadScene("Gameplay");
        });
    }
    // ... rest unchanged ...
    return go;
}
```

Update the call-site in `PopulateLevels()`:

```csharp
var card = CreateLevelCard(_gridParent, i, data, IsUnlocked(lm, i), act, isCustom);
```

- [ ] **Step 8.4: Add empty-state label for Custom tab**

After the `PopulateLevels()` loop, add:

```csharp
// Empty-state label for Custom tab
var customEmpty = new GameObject("CustomEmptyLabel");
customEmpty.transform.SetParent(_gridParent, false);
customEmpty.name = "CustomEmpty_ActCustom";  // ends with "Act6" equivalent naming
var ceRT = customEmpty.AddComponent<RectTransform>();
ceRT.sizeDelta = new Vector2(600f, 80f);
var ceTMP = customEmpty.AddComponent<TextMeshProUGUI>();
ceTMP.text = "No custom levels yet.\nGo to Level Editor to create one.";
ceTMP.fontSize = 18;
ceTMP.alignment = TextAlignmentOptions.Center;
ceTMP.color = new Color(0.60f, 0.64f, 0.72f);
ceTMP.raycastTarget = false;
customEmpty.SetActive(false);
```

Note: `SelectAct()` uses `child.name.EndsWith($"Act{_selectedAct + 1}")` to show/hide cards. The Custom tab is act index 5, so we need names ending in `"Act6"`. Update the naming:

In `CreateLevelCard()`, the current naming is:
```csharp
go.name = $"LevelCard_{index + 1:00}_Act{act + 1}";
```
This already works — act 5 produces `"Act6"`.

For the empty label, change its name to:
```csharp
customEmpty.name = "CustomEmpty_Act6";
```

And after the loop, count how many cards have `"Act6"` suffix — if only the empty label, show it. Actually the simpler approach: always create the label with the `Act6` suffix so `SelectAct()` shows/hides it automatically. The label is always created but only visible when the Custom tab is selected AND no actual level cards exist with `Act6` suffix. But SelectAct will show ALL `Act6` objects including the label if the label ends with `Act6`...

The simplest fix: show the empty label only when no real custom level cards exist. Update `PopulateLevels()` to count custom cards and conditionally create the empty label. Replace the empty-state block above with:

```csharp
bool hasCustomCards = false;
for (int i = 0; i < lm.LevelCount; i++)
{
    var d2 = lm.GetLevel(i);
    if (d2 != null && d2.author != "built-in") { hasCustomCards = true; break; }
}
if (!hasCustomCards)
{
    var customEmpty = new GameObject("CustomEmptyLabel_Act6");
    customEmpty.transform.SetParent(_gridParent, false);
    var ceRT = customEmpty.AddComponent<RectTransform>();
    ceRT.sizeDelta = new Vector2(600f, 80f);
    var ceTMP = customEmpty.AddComponent<TextMeshProUGUI>();
    ceTMP.text      = "No custom levels yet.\nGo to Level Editor to create one.";
    ceTMP.fontSize  = 18;
    ceTMP.alignment = TextAlignmentOptions.Center;
    ceTMP.color     = new Color(0.60f, 0.64f, 0.72f);
    ceTMP.raycastTarget = false;
    customEmpty.SetActive(false);  // SelectAct() will show it when tab 6 is selected
}
```

- [ ] **Step 8.5: Verify in Unity — open LevelSelect scene and check Custom tab**

- 6 tabs appear (Simple through Custom)
- Clicking "Custom" tab shows only custom levels (or the empty-state message)
- Built-in levels do not appear in Custom tab
- Unlocking / star logic unchanged for other tabs

- [ ] **Step 8.6: Commit**

```
git add Assets/Scripts/UI/LevelSelectUI.cs
git commit -m "feat: add Custom tab to LevelSelectUI for user-created levels"
```

---

## Self-Review Checklist

**Spec coverage:**

| Spec requirement | Covered by |
|---|---|
| Redo (Ctrl+Y) | Task 3 |
| Safe Resize (preserve content) | Task 4 |
| Clear Interior | Task 4 |
| BFS reachability hard error | Task 1 |
| Corner-box / high-box-count / default-par warnings | Task 1 |
| Warnings shown in UI without blocking save | Task 6 |
| Brush bar in bottom row with highlight | Tasks 6 + 7 |
| Erase brush mode | Tasks 5 + 6 + 7 |
| Right-side constant level list | Tasks 6 + 7 |
| Load Selected / Delete from level list | Tasks 6 + 7 |
| Mechanic pairing hint text | Task 6 (GetPairingHint) |
| Custom Tab in Level Select | Task 8 |
| Custom-level card (always unlocked, show author) | Task 8 |
| Empty-state label for Custom tab | Task 8 |
| LevelSerializer.Delete() | Task 2 |
| LevelSerializer.GetAllLevelMeta() | Task 2 |
| Fix Chinese log text | Task 2 |
| Save/Test disabled when level invalid | Task 6 |
| Test-play returns to LevelEditor | Already implemented (LevelManager.IsTestMode) — no new task needed |
| Main Menu Level Editor button | Already implemented (MainMenuUI.btnEditor) — no new task needed |

**Type / name consistency check:**
- `GetAllLevelMeta()` returns `(string id, string name, DateTime modified)[]` — used in `RefreshLevelList()` with `foreach (var (id, name, modified) in metas)`
- `GetPairingHint()` returns `string?` — used with null-check `hint ?? ""`
- `ResizeLevel(int, int)` called from `LevelEditorUI.btnResize` listener ✓
- `ClearInterior()` called from `LevelEditorUI.btnClear` listener ✓
- `UndoLastAction()` and `RedoLastAction()` are now `public` ✓
- `brushButtons` array index 12 = Erase, constant `EraseBrushIdx = 12` ✓
- AllSceneBuilder creates 13 brush buttons (indices 0-12), matches `brushBtns` array ✓
