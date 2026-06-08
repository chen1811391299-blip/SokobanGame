using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEditorManager : MonoBehaviour
{
    public static LevelEditorManager Instance { get; private set; }

    [Header("Current Level")]
    public LevelData editingLevel;

    private Vector2Int? _pendingPortalPos;
    private int _nextPortalColor;
    private List<PortalPair> _portalPairs = new();

    private Vector2Int? _pendingPlatePos;
    private List<DoorLink> _doorLinks = new();

    private readonly List<EditorSnapshot> _undoStack = new();
    private readonly List<EditorSnapshot> _redoStack = new();
    private const int MaxUndo = 100;

    public TileType SelectedBrush { get; set; } = TileType.Wall;
    public bool     IsEraseMode   { get; set; } = false;

    private struct EditorSnapshot
    {
        public string levelId;
        public string levelName;
        public string author;
        public int parMoves;
        public int width;
        public int height;
        public int[] tiles;
        public PortalPair[] portalPairs;
        public DoorLink[] doorLinks;
        public Vector2Int? pendingPortalPos;
        public Vector2Int? pendingPlatePos;
        public int nextPortalColor;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (LevelManager.IsTestMode && LevelManager.TestLevel != null)
        {
            // Returning from test play — restore the in-progress level
            var level = LevelManager.TestLevel;
            LevelManager.IsTestMode = false;
            LoadLevelForEditing(level);
        }
        else if (editingLevel != null)
        {
            LoadLevelForEditing(editingLevel);
        }
        else
        {
            NewLevel(15, 15);
        }
        AudioManager.PlayBGM(AudioManager.Instance?.bgmEditor);
    }

    void Update()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.Z)) UndoLastAction();
        if (ctrl && Input.GetKeyDown(KeyCode.Y)) RedoLastAction();
    }

    public void NewLevel(int w, int h)
    {
        editingLevel = ScriptableObject.CreateInstance<LevelData>();
        editingLevel.Init(w, h);
        editingLevel.levelName = "New Level";
        editingLevel.author = "player";
        editingLevel.parMoves = 20;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool border = x == 0 || x == w - 1 || y == 0 || y == h - 1;
            editingLevel.SetTile(x, y, border ? TileType.Wall : TileType.Floor);
        }

        ResetMechanicState();
        _undoStack.Clear();
        _redoStack.Clear();
        RebuildView();
        GetComponent<LevelEditorUI>()?.SyncFromLevel();
    }

    public void LoadLevelForEditing(LevelData source)
    {
        if (source == null) return;

        editingLevel = CloneLevel(source);
        _portalPairs = new List<PortalPair>(editingLevel.portalPairs ?? Array.Empty<PortalPair>());
        _doorLinks = new List<DoorLink>(editingLevel.doorLinks ?? Array.Empty<DoorLink>());
        _pendingPortalPos = null;
        _pendingPlatePos = null;
        _nextPortalColor = _portalPairs.Count % 3;
        _undoStack.Clear();
        _redoStack.Clear();

        RebuildView();
        GetComponent<LevelEditorUI>()?.SyncFromLevel();
    }

    public bool LoadLatestSaved()
    {
        var loaded = LevelSerializer.LoadMostRecent();
        if (loaded == null)
        {
            GetComponent<LevelEditorUI>()?.ShowValidationError("No saved custom level found.");
            return false;
        }

        LoadLevelForEditing(loaded);
        return true;
    }

    public void PlaceTile(int x, int y)
    {
        if (editingLevel == null || !editingLevel.IsInBounds(x, y)) return;
        PushUndo();

        var pos = new Vector2Int(x, y);
        if (SelectedBrush == TileType.Portal)
        {
            HandlePortalPlacement(pos);
            return;
        }

        if (SelectedBrush == TileType.DoorClosed)
        {
            HandleDoorPlacement(pos);
            return;
        }

        var affected = RemoveMechanicsAt(pos);
        if (SelectedBrush == TileType.Player)
            affected.AddRange(RemoveExistingPlayersExcept(pos));

        editingLevel.SetTile(x, y, SelectedBrush);
        affected.Add(pos);
        ApplyMechanicData();
        RefreshTiles(affected);
    }

    public void EraseTile(int x, int y)
    {
        if (editingLevel == null || !editingLevel.IsInBounds(x, y)) return;
        PushUndo();

        var pos = new Vector2Int(x, y);
        var affected = RemoveMechanicsAt(pos);
        editingLevel.SetTile(x, y, TileType.Floor);
        affected.Add(pos);
        ApplyMechanicData();
        RefreshTiles(affected);
    }

    public void Save()
    {
        ApplyMechanicData();
        var result = LevelValidator.Validate(editingLevel);
        if (!result.IsValid)
        {
            Debug.LogWarning($"[Editor] Invalid level: {result.ErrorMessage}");
            GetComponent<LevelEditorUI>()?.ShowValidationError(result.ErrorMessage);
            return;
        }

        LevelSerializer.Save(editingLevel);
        LevelManager.Instance?.Refresh();
        GetComponent<LevelEditorUI>()?.ShowValidationError("Saved.");
    }

    public void TestPlay()
    {
        ApplyMechanicData();
        var result = LevelValidator.Validate(editingLevel);
        if (!result.IsValid)
        {
            Debug.LogWarning($"[Editor] Invalid level: {result.ErrorMessage}");
            GetComponent<LevelEditorUI>()?.ShowValidationError(result.ErrorMessage);
            return;
        }

        LevelManager.TestLevel = CloneLevel(editingLevel);
        LevelManager.IsTestMode = true;
        SceneTransition.LoadScene("Gameplay");
    }

    public ValidationResult GetValidation() => LevelValidator.Validate(editingLevel);

    public string GetPairingHint()
    {
        if (_pendingPortalPos.HasValue)
            return "Click a second Portal tile to complete the pair.";
        if (_pendingPlatePos.HasValue)
            return "Click a Door tile to link it to the pressure plate.";
        return null;
    }

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

    private void PushUndo()
    {
        if (editingLevel == null) return;
        if (_undoStack.Count >= MaxUndo)
            _undoStack.RemoveAt(0);
        _undoStack.Add(CaptureSnapshot());
        _redoStack.Clear();
    }

    private void HandlePortalPlacement(Vector2Int pos)
    {
        var affected = RemoveMechanicsAt(pos);
        editingLevel.SetTile(pos.x, pos.y, TileType.Portal);
        affected.Add(pos);

        if (_pendingPortalPos == null)
        {
            _pendingPortalPos = pos;
        }
        else if (_pendingPortalPos.Value == pos)
        {
            _pendingPortalPos = null;
        }
        else if (editingLevel.GetTile(_pendingPortalPos.Value.x, _pendingPortalPos.Value.y) == TileType.Portal)
        {
            _portalPairs.Add(new PortalPair
            {
                posA = _pendingPortalPos.Value,
                posB = pos,
                colorId = _nextPortalColor++ % 3
            });
            _pendingPortalPos = null;
        }
        else
        {
            _pendingPortalPos = pos;
        }

        ApplyMechanicData();
        RefreshTiles(affected);
    }

    private void HandleDoorPlacement(Vector2Int pos)
    {
        var affected = new List<Vector2Int>();

        if (_pendingPlatePos == null)
        {
            if (editingLevel.GetTile(pos.x, pos.y) != TileType.PressurePlate)
            {
                affected.AddRange(RemoveMechanicsAt(pos));
                editingLevel.SetTile(pos.x, pos.y, TileType.PressurePlate);
            }
            _pendingPlatePos = pos;
        }
        else if (_pendingPlatePos.Value == pos)
        {
            _pendingPlatePos = null;
        }
        else
        {
            affected.AddRange(RemoveMechanicsAt(pos));
            editingLevel.SetTile(pos.x, pos.y, TileType.DoorClosed);
            _doorLinks.Add(new DoorLink
            {
                platePos = _pendingPlatePos.Value,
                doorPos = pos
            });
            _pendingPlatePos = null;
        }

        affected.Add(pos);
        ApplyMechanicData();
        RefreshTiles(affected);
    }

    private List<Vector2Int> RemoveMechanicsAt(Vector2Int pos)
    {
        var affected = new List<Vector2Int>();
        affected.AddRange(RemovePortalAt(pos));
        affected.AddRange(RemoveDoorLinkAt(pos));
        return affected;
    }

    private List<Vector2Int> RemovePortalAt(Vector2Int pos)
    {
        var affected = new List<Vector2Int>();

        if (_pendingPortalPos == pos)
        {
            _pendingPortalPos = null;
            affected.Add(pos);
        }

        for (int i = _portalPairs.Count - 1; i >= 0; i--)
        {
            var pair = _portalPairs[i];
            if (pair.posA != pos && pair.posB != pos) continue;

            affected.Add(pair.posA);
            affected.Add(pair.posB);
            _portalPairs.RemoveAt(i);
        }

        foreach (var p in affected)
        {
            if (editingLevel.IsInBounds(p.x, p.y) && editingLevel.GetTile(p.x, p.y) == TileType.Portal)
                editingLevel.SetTile(p.x, p.y, TileType.Floor);
        }

        return affected;
    }

    private List<Vector2Int> RemoveDoorLinkAt(Vector2Int pos)
    {
        var affected = new List<Vector2Int>();
        var platesToMaybeClear = new List<Vector2Int>();

        if (_pendingPlatePos == pos)
        {
            _pendingPlatePos = null;
            affected.Add(pos);
        }

        for (int i = _doorLinks.Count - 1; i >= 0; i--)
        {
            var link = _doorLinks[i];
            if (link.platePos != pos && link.doorPos != pos) continue;

            if (link.platePos == pos)
            {
                affected.Add(link.platePos);
                affected.Add(link.doorPos);
            }
            else
            {
                affected.Add(link.doorPos);
                platesToMaybeClear.Add(link.platePos);
            }
            _doorLinks.RemoveAt(i);
        }

        foreach (var plate in platesToMaybeClear)
        {
            bool stillControlsDoor = false;
            foreach (var link in _doorLinks)
            {
                if (link.platePos != plate) continue;
                stillControlsDoor = true;
                break;
            }

            if (!stillControlsDoor)
                affected.Add(plate);
        }

        foreach (var p in affected)
        {
            if (!editingLevel.IsInBounds(p.x, p.y)) continue;
            var tile = editingLevel.GetTile(p.x, p.y);
            if (tile == TileType.PressurePlate || tile == TileType.DoorClosed || tile == TileType.DoorOpen)
                editingLevel.SetTile(p.x, p.y, TileType.Floor);
        }

        return affected;
    }

    private List<Vector2Int> RemoveExistingPlayersExcept(Vector2Int keep)
    {
        var affected = new List<Vector2Int>();
        for (int y = 0; y < editingLevel.height; y++)
        for (int x = 0; x < editingLevel.width; x++)
        {
            var pos = new Vector2Int(x, y);
            if (pos == keep || editingLevel.GetTile(x, y) != TileType.Player) continue;
            editingLevel.SetTile(x, y, TileType.Floor);
            affected.Add(pos);
        }
        return affected;
    }

    private EditorSnapshot CaptureSnapshot() => new EditorSnapshot
    {
        levelId = editingLevel.levelId,
        levelName = editingLevel.levelName,
        author = editingLevel.author,
        parMoves = editingLevel.parMoves,
        width = editingLevel.width,
        height = editingLevel.height,
        tiles = (int[])editingLevel.tiles.Clone(),
        portalPairs = _portalPairs.ToArray(),
        doorLinks = _doorLinks.ToArray(),
        pendingPortalPos = _pendingPortalPos,
        pendingPlatePos = _pendingPlatePos,
        nextPortalColor = _nextPortalColor
    };

    private void RestoreSnapshot(EditorSnapshot snapshot)
    {
        editingLevel.levelId = snapshot.levelId;
        editingLevel.levelName = snapshot.levelName;
        editingLevel.author = snapshot.author;
        editingLevel.parMoves = snapshot.parMoves;
        editingLevel.width = snapshot.width;
        editingLevel.height = snapshot.height;
        editingLevel.tiles = (int[])snapshot.tiles.Clone();

        _portalPairs = new List<PortalPair>(snapshot.portalPairs ?? Array.Empty<PortalPair>());
        _doorLinks = new List<DoorLink>(snapshot.doorLinks ?? Array.Empty<DoorLink>());
        _pendingPortalPos = snapshot.pendingPortalPos;
        _pendingPlatePos = snapshot.pendingPlatePos;
        _nextPortalColor = snapshot.nextPortalColor;
        ApplyMechanicData();
    }

    private void ResetMechanicState()
    {
        _portalPairs.Clear();
        _doorLinks.Clear();
        _pendingPortalPos = null;
        _pendingPlatePos = null;
        _nextPortalColor = 0;
        ApplyMechanicData();
    }

    private void ApplyMechanicData()
    {
        if (editingLevel == null) return;
        editingLevel.portalPairs = _portalPairs.ToArray();
        editingLevel.doorLinks = _doorLinks.ToArray();
    }

    private void RebuildView() =>
        GetComponent<EditorGridView>()?.Rebuild(editingLevel);

    private void RefreshTiles(IEnumerable<Vector2Int> positions)
    {
        var seen = new HashSet<Vector2Int>();
        var view = GetComponent<EditorGridView>();
        if (view == null) return;

        foreach (var pos in positions)
        {
            if (!seen.Add(pos)) continue;
            if (editingLevel.IsInBounds(pos.x, pos.y))
                view.RefreshTile(pos.x, pos.y, editingLevel);
        }
    }

    private static LevelData CloneLevel(LevelData source)
    {
        var clone = ScriptableObject.CreateInstance<LevelData>();
        clone.levelId = source.levelId;
        clone.levelName = source.levelName;
        clone.author = source.author;
        clone.parMoves = source.parMoves;
        clone.width = Mathf.Max(3, source.width);
        clone.height = Mathf.Max(3, source.height);
        clone.tiles = CloneOrCreateTiles(source, clone.width, clone.height);
        clone.portalPairs = source.portalPairs != null ? (PortalPair[])source.portalPairs.Clone() : Array.Empty<PortalPair>();
        clone.doorLinks = source.doorLinks != null ? (DoorLink[])source.doorLinks.Clone() : Array.Empty<DoorLink>();
        return clone;
    }

    private static int[] CloneOrCreateTiles(LevelData source, int width, int height)
    {
        int expected = width * height;
        if (source.tiles != null && source.tiles.Length == expected)
            return (int[])source.tiles.Clone();

        var tiles = new int[expected];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1;
            tiles[y * width + x] = (int)(border ? TileType.Wall : TileType.Floor);
        }
        return tiles;
    }
}
