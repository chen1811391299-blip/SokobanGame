using System;
using UnityEngine;

[Serializable]
public class LevelDataJson
{
    public string id;
    public string name;
    public string author;
    public int width;
    public int height;
    public int parMoves;
    public int[] tiles;
    public PortalPair[] portalPairs;
    public DoorLink[] doorLinks;

    public LevelData ToLevelData()
    {
        int safeWidth = Mathf.Max(3, width);
        int safeHeight = Mathf.Max(3, height);

        var data = ScriptableObject.CreateInstance<LevelData>();
        data.levelId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N")[..8] : id;
        data.levelName = string.IsNullOrEmpty(name) ? "Untitled Level" : name;
        data.author = string.IsNullOrEmpty(author) ? "unknown" : author;
        data.width = safeWidth;
        data.height = safeHeight;
        data.parMoves = parMoves > 0 ? parMoves : 20;
        data.tiles = NormalizeTiles(safeWidth, safeHeight, tiles);
        data.portalPairs = portalPairs ?? Array.Empty<PortalPair>();
        data.doorLinks = doorLinks ?? Array.Empty<DoorLink>();
        return data;
    }

    public static LevelDataJson From(LevelData d) => new LevelDataJson
    {
        id = d.levelId,
        name = d.levelName,
        author = d.author,
        width = d.width,
        height = d.height,
        parMoves = d.parMoves,
        tiles = d.tiles != null ? (int[])d.tiles.Clone() : Array.Empty<int>(),
        portalPairs = d.portalPairs ?? Array.Empty<PortalPair>(),
        doorLinks = d.doorLinks ?? Array.Empty<DoorLink>()
    };

    private static int[] NormalizeTiles(int width, int height, int[] source)
    {
        int expected = width * height;
        if (source != null && source.Length == expected)
            return (int[])source.Clone();

        var normalized = new int[expected];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1;
            normalized[y * width + x] = (int)(border ? TileType.Wall : TileType.Floor);
        }
        return normalized;
    }
}
