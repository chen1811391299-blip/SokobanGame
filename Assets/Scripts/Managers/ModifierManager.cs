using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ModifierType
{
    None,
    AllIce,
    RandomPortals,
    MirrorControls,
    FogOfWar
}

public class ModifierManager : MonoBehaviour
{
    public static ModifierManager Instance { get; private set; }

    public ModifierType ActiveModifier { get; private set; } = ModifierType.None;

    private static readonly ModifierType[] _pool =
    {
        ModifierType.AllIce,
        ModifierType.RandomPortals,
        ModifierType.MirrorControls,
        ModifierType.FogOfWar
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RollModifier() =>
        ActiveModifier = _pool[Random.Range(0, _pool.Length)];

    public void ClearModifier() => ActiveModifier = ModifierType.None;

    public string GetDisplayName() => ActiveModifier switch
    {
        ModifierType.AllIce         => "[Modifier] All Ice",
        ModifierType.RandomPortals  => "[Modifier] Random Portals",
        ModifierType.MirrorControls => "[Modifier] Mirror Controls",
        ModifierType.FogOfWar       => "[Modifier] Fog of War",
        _                           => ""
    };

    public string GetDescription() => ActiveModifier switch
    {
        ModifierType.AllIce         => "All floors become ice — slide until you hit a wall",
        ModifierType.RandomPortals  => "A random portal pair appears in the level",
        ModifierType.MirrorControls => "Left/Right arrow keys are swapped",
        ModifierType.FogOfWar       => "Only tiles within 3 cells of the player are visible",
        _                           => ""
    };

    public void ApplyToGrid(SokobanGrid grid)
    {
        switch (ActiveModifier)
        {
            case ModifierType.AllIce:
                grid.OverrideAllFloorToIce();
                break;
            case ModifierType.RandomPortals:
                InjectRandomPortals(grid);
                break;
        }
    }

    public void ApplyToRenderer(GridRenderer renderer, SokobanGrid grid)
    {
        if (ActiveModifier == ModifierType.FogOfWar)
            renderer.ApplyFogOfWar(grid.PlayerPos, 3);
    }

    private void InjectRandomPortals(SokobanGrid grid)
    {
        var floors = new List<Vector2Int>();
        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width;  x++)
        {
            var pos = new Vector2Int(x, y);
            if (grid.GetBase(pos) == TileType.Floor &&
                !grid.Boxes.Contains(pos)           &&
                pos != grid.PlayerPos)
                floors.Add(pos);
        }
        if (floors.Count < 2) return;
        int ia = Random.Range(0, floors.Count);
        int ib;
        do { ib = Random.Range(0, floors.Count); } while (ib == ia);
        grid.InjectPortalPair(floors[ia], floors[ib]);
    }
}
