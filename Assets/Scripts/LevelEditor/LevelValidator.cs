using System.Collections.Generic;
using UnityEngine;

public struct ValidationResult
{
    public bool IsValid;
    public int PlayerCount;
    public int BoxCount;
    public int GoalCount;
    public bool PortalsPaired;
    public bool DoorsLinked;
    public string ErrorMessage;
    public string[] Warnings;   // never null after Validate()
}

public static class LevelValidator
{
    public static ValidationResult Validate(LevelData data)
    {
        var r = new ValidationResult
        {
            IsValid = true,
            PortalsPaired = true,
            DoorsLinked = true,
            Warnings = System.Array.Empty<string>()
        };

        if (data == null || data.tiles == null || data.tiles.Length != data.width * data.height)
        {
            r.IsValid = false;
            r.ErrorMessage = "Level data is missing or has an invalid tile array.";
            return r;
        }

        int portalCount = 0;
        int plateCount = 0;
        int doorCount = 0;

        for (int y = 0; y < data.height; y++)
        for (int x = 0; x < data.width; x++)
        {
            switch (data.GetTile(x, y))
            {
                case TileType.Player:
                    r.PlayerCount++;
                    break;
                case TileType.Box:
                    r.BoxCount++;
                    break;
                case TileType.Goal:
                    r.GoalCount++;
                    break;
                case TileType.BoxOnGoal:
                    r.BoxCount++;
                    r.GoalCount++;
                    break;
                case TileType.Portal:
                    portalCount++;
                    break;
                case TileType.PressurePlate:
                    plateCount++;
                    break;
                case TileType.DoorClosed:
                case TileType.DoorOpen:
                    doorCount++;
                    break;
            }
        }

        if (r.PlayerCount != 1)
            return Invalid(r, $"Level must contain exactly 1 player. Current: {r.PlayerCount}");

        if (r.BoxCount == 0)
            return Invalid(r, "Level must contain at least 1 box.");

        if (r.BoxCount != r.GoalCount)
            return Invalid(r, $"Box count ({r.BoxCount}) must equal goal count ({r.GoalCount}).");

        if (!ValidatePortals(data, portalCount, out string portalError))
        {
            r.PortalsPaired = false;
            return Invalid(r, portalError);
        }

        if (!ValidateDoors(data, plateCount, doorCount, out string doorError))
        {
            r.DoorsLinked = false;
            return Invalid(r, doorError);
        }

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
        var warnings = new List<string>();

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
    }

    // ── Private helpers ───────────────────────────────────────────────────────

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

    private static HashSet<Vector2Int> BfsFromPlayer(LevelData data)
    {
        var visited = new HashSet<Vector2Int>();
        Vector2Int? start = null;
        for (int y = 0; y < data.height && start == null; y++)
        for (int x = 0; x < data.width  && start == null; x++)
            if (data.GetTile(x, y) == TileType.Player)
                start = new Vector2Int(x, y);

        if (start == null) return visited;

        var queue = new Queue<Vector2Int>();
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

    private static bool ValidatePortals(LevelData data, int portalCount, out string error)
    {
        error = "";
        var pairs = data.portalPairs ?? System.Array.Empty<PortalPair>();
        if (portalCount != pairs.Length * 2)
        {
            error = "Every portal tile must be part of a pair.";
            return false;
        }

        var used = new HashSet<Vector2Int>();
        foreach (var pair in pairs)
        {
            if (pair.posA == pair.posB)
            {
                error = "A portal pair cannot link to itself.";
                return false;
            }

            if (!IsTile(data, pair.posA, TileType.Portal) || !IsTile(data, pair.posB, TileType.Portal))
            {
                error = "Portal pair points to a non-portal tile.";
                return false;
            }

            if (!used.Add(pair.posA) || !used.Add(pair.posB))
            {
                error = "A portal tile is used by more than one pair.";
                return false;
            }
        }

        return true;
    }

    private static bool ValidateDoors(LevelData data, int plateCount, int doorCount, out string error)
    {
        error = "";
        var links = data.doorLinks ?? System.Array.Empty<DoorLink>();
        if (plateCount == 0 && doorCount == 0 && links.Length == 0)
            return true;

        var linkedDoors  = new HashSet<Vector2Int>();
        var linkedPlates = new HashSet<Vector2Int>();
        foreach (var link in links)
        {
            if (!IsTile(data, link.platePos, TileType.PressurePlate))
            {
                error = "Door link points to a non-pressure-plate tile.";
                return false;
            }

            if (!IsDoorTile(data, link.doorPos))
            {
                error = "Door link points to a non-door tile.";
                return false;
            }

            linkedPlates.Add(link.platePos);
            linkedDoors.Add(link.doorPos);
        }

        if (linkedDoors.Count != doorCount)
        {
            error = "Every door tile must be linked to a pressure plate.";
            return false;
        }

        if (linkedPlates.Count != plateCount)
        {
            error = "Every pressure plate must control at least one door.";
            return false;
        }

        return true;
    }

    private static ValidationResult Invalid(ValidationResult r, string message)
    {
        r.IsValid = false;
        r.ErrorMessage = message;
        return r;
    }

    private static bool IsTile(LevelData data, Vector2Int pos, TileType type) =>
        data.IsInBounds(pos.x, pos.y) && data.GetTile(pos.x, pos.y) == type;

    private static bool IsDoorTile(LevelData data, Vector2Int pos)
    {
        if (!data.IsInBounds(pos.x, pos.y)) return false;
        var tile = data.GetTile(pos.x, pos.y);
        return tile == TileType.DoorClosed || tile == TileType.DoorOpen;
    }
}
