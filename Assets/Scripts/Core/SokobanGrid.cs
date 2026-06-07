using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SokobanGrid
{
    public int Width  { get; private set; }
    public int Height { get; private set; }

    private TileType[,]              _base;
    public  Vector2Int               PlayerPos { get; private set; }
    private HashSet<Vector2Int>      _boxes    = new();
    private HashSet<Vector2Int>      _goals    = new();
    private bool[]                   _doorStates;
    private DoorLink[]               _doorLinks = Array.Empty<DoorLink>();
    private Dictionary<Vector2Int, Vector2Int> _portals = new();

    public IReadOnlyCollection<Vector2Int>               Boxes       => _boxes;
    public IReadOnlyCollection<Vector2Int>               Goals       => _goals;
    public bool[]                                        DoorStates  => _doorStates;
    public DoorLink[]                                    DoorLinks   => _doorLinks;
    public IReadOnlyDictionary<Vector2Int, Vector2Int>   PortalLinks => _portals;

    // ── 加载关卡 ────────────────────────────────────────────────
    public void LoadLevel(LevelData data)
    {
        Width  = data.width;
        Height = data.height;
        _base  = new TileType[Width, Height];
        _boxes = new HashSet<Vector2Int>();
        _goals = new HashSet<Vector2Int>();

        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
        {
            var pos  = new Vector2Int(x, y);
            var tile = data.GetTile(x, y);
            switch (tile)
            {
                case TileType.Player:
                    _base[x, y] = TileType.Floor;
                    PlayerPos   = pos;
                    break;
                case TileType.Box:
                    _base[x, y] = TileType.Floor;
                    _boxes.Add(pos);
                    break;
                case TileType.Goal:
                    _base[x, y] = TileType.Goal;
                    _goals.Add(pos);
                    break;
                case TileType.BoxOnGoal:
                    _base[x, y] = TileType.Goal;
                    _goals.Add(pos);
                    _boxes.Add(pos);
                    break;
                default:
                    _base[x, y] = tile;
                    break;
            }
        }

        _portals.Clear();
        foreach (var p in data.portalPairs)
        {
            _portals[p.posA] = p.posB;
            _portals[p.posB] = p.posA;
        }

        _doorLinks  = data.doorLinks ?? Array.Empty<DoorLink>();
        _doorStates = new bool[_doorLinks.Length];
        SyncDoors();
    }

    // ── 移动 ────────────────────────────────────────────────────
    public bool TryMove(Vector2Int dir)
    {
        var dest = TeleportIfPortal(PlayerPos + dir, dir);

        if (!IsPassable(dest)) return false;

        if (_boxes.Contains(dest))
        {
            var boxDest = PushBox(dest, dir);
            if (boxDest == dest) return false;
            _boxes.Remove(dest);
            _boxes.Add(boxDest);
        }

        PlayerPos = SlideOnIce(dest, dir);
        SyncDoors();
        return true;
    }

    private Vector2Int TeleportIfPortal(Vector2Int pos, Vector2Int dir)
    {
        if (_portals.TryGetValue(pos, out var linked))
            return linked + dir;
        return pos;
    }

    private Vector2Int SlideOnIce(Vector2Int pos, Vector2Int dir)
    {
        while (GetBase(pos) == TileType.Ice)
        {
            var next = TeleportIfPortal(pos + dir, dir);
            if (!IsPassable(next) || _boxes.Contains(next)) break;
            pos = next;
        }
        return pos;
    }

    private Vector2Int PushBox(Vector2Int boxPos, Vector2Int dir)
    {
        var dest = TeleportIfPortal(boxPos + dir, dir);
        if (!IsPassable(dest) || _boxes.Contains(dest)) return boxPos;
        return SlideOnIce(dest, dir);
    }

    private void SyncDoors()
    {
        for (int i = 0; i < _doorLinks.Length; i++)
        {
            var platePos = _doorLinks[i].platePos;
            var dp = _doorLinks[i].doorPos;
            bool plateHeld = _boxes.Contains(platePos) || PlayerPos == platePos;
            bool doorOccupied = _boxes.Contains(dp) || PlayerPos == dp;
            _doorStates[i] = plateHeld || doorOccupied;
            _base[dp.x, dp.y] = _doorStates[i] ? TileType.DoorOpen : TileType.DoorClosed;
        }
    }

    // ── 查询 ────────────────────────────────────────────────────
    public bool IsComplete() => _goals.Count > 0 && _goals.All(g => _boxes.Contains(g));

    public TileType GetBase(Vector2Int pos) =>
        IsInBounds(pos) ? _base[pos.x, pos.y] : TileType.Wall;

    public bool IsInBounds(Vector2Int pos) =>
        pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

    private bool IsPassable(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return false;
        var t = _base[pos.x, pos.y];
        return t != TileType.Wall && t != TileType.DoorClosed && t != TileType.Empty;
    }

    // ── 撤销快照 ────────────────────────────────────────────────
    public GameState GetSnapshot(int moves) =>
        new GameState(PlayerPos, _boxes.ToArray(), _doorStates, moves);

    public void LoadSnapshot(GameState s)
    {
        PlayerPos = s.playerPos;
        _boxes    = new HashSet<Vector2Int>(s.boxPositions);
        if (s.doorStates != null && s.doorStates.Length == _doorStates.Length)
            Array.Copy(s.doorStates, _doorStates, _doorStates.Length);
        SyncDoors();
    }

    // ── 修饰符接口 ──────────────────────────────────────────────
    public void OverrideAllFloorToIce()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width;  x++)
            if (_base[x, y] == TileType.Floor)
                _base[x, y] = TileType.Ice;
    }

    public void InjectPortalPair(Vector2Int a, Vector2Int b)
    {
        _portals[a] = b;
        _portals[b] = a;
        _base[a.x, a.y] = TileType.Portal;
        _base[b.x, b.y] = TileType.Portal;
    }
}
