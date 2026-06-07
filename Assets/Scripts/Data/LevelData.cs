using System;
using UnityEngine;

[Serializable]
public struct PortalPair
{
    public Vector2Int posA;
    public Vector2Int posB;
    public int colorId;   // 0=blue, 1=orange, 2=green
}

[Serializable]
public struct DoorLink
{
    public Vector2Int platePos;
    public Vector2Int doorPos;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Sokoban/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelId;
    public string levelName  = "New Level";
    public string author     = "built-in";
    public int    parMoves   = 20;
    public int    width      = 8;
    public int    height     = 8;
    public int[]  tiles;
    public PortalPair[] portalPairs = Array.Empty<PortalPair>();
    public DoorLink[]   doorLinks   = Array.Empty<DoorLink>();

    public TileType GetTile(int x, int y)
    {
        if (!IsInBounds(x, y)) return TileType.Wall;
        return (TileType)tiles[y * width + x];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (IsInBounds(x, y)) tiles[y * width + x] = (int)type;
    }

    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < width && y >= 0 && y < height;

    public void Init(int w, int h)
    {
        width  = w;
        height = h;
        tiles  = new int[w * h];
    }
}
