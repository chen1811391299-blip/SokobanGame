using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelSetupUtility
{
    [MenuItem("SokobanGame/5. Fix Level Names (English)")]
    public static void FixLevelNames()
    {
        foreach (var def in Definitions())
            FixMeta(def.Id, def.Name, def.ParMoves);
        AssetDatabase.SaveAssets();
        Debug.Log("[LevelSetupUtility] Level names/par updated.");
    }

    [MenuItem("SokobanGame/2. Create Level Data Assets")]
    public static void CreateLevels()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Levels");

        var defs = Definitions();
        foreach (var def in defs)
            MakeLevel(def);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LevelSetupUtility] Done - {defs.Length} levels created.");
    }

    private readonly struct LevelDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int ParMoves;
        public readonly string[] Rows;

        public LevelDefinition(string id, string name, int parMoves, string[] rows)
        {
            Id = id;
            Name = name;
            ParMoves = parMoves;
            Rows = rows;
        }
    }

    private static LevelDefinition Def(string id, string name, int parMoves, string[] rows) =>
        new LevelDefinition(id, name, parMoves, rows);

    private static LevelDefinition[] Definitions() => new[]
    {
            Def("Level_01", "Level 1 - First Push", 5, new[]
            {
                "#######",
                "#     #",
                "#  .  #",
                "#     #",
                "#  $  #",
                "#@    #",
                "#######"
            }),
            Def("Level_02", "Level 2 - Many Goals", 20, new[]
            {
                "xx     xx",
                "xx ... xx",
                "x  $$$  x",
                "x   @   x",
                "x  $$$  x",
                "xx ... xx",
                "xx     xx"
            }),
            Def("Level_03", "Level 3 - Turnaround Room", 28, new[]
            {
                "######",
                "# @$.#",
                "#    #",
                "# $#.#",
                "#    #",
                "######"
            }),
            Def("Level_04", "Level 4 - Portal Delivery", 4, new[]
            {
                "##########",
                "#   .    #",
                "#   $  ###",
                "# @ $A#A.#",
                "#      ###",
                "#        #",
                "##########"
            }),
            Def("Level_05", "Level 5 - Ice Runway", 4, new[]
            {
                "##########",
                "#   .    #",
                "#   $    #",
                "# @ $~~. #",
                "#        #",
                "#        #",
                "##########"
            }),
            Def("Level_06", "Level 6 - Pressure Gate", 10, new[]
            {
                "##########",
                "##########",
                "### $D .##",
                "# @ $p####",
                "#    .   #",
                "#        #",
                "##########"
            }),
            Def("Level_07", "Level 7 - Frozen Dock", 30, new[]
            {
                "#############",
                "#     .     #",
                "#  ### ###  #",
                "#   $       #",
                "# @  $~~.   #",
                "#  ### ###  #",
                "#   $ .     #",
                "#           #",
                "#############"
            }),
            Def("Level_08", "Level 8 - Portal Vault", 44, new[]
            {
                "#############",
                "#    .      #",
                "#  ###  #####",
                "# @  $ A#A.##",
                "#  $   ######",
                "#     $  $  #",
                "#    .   .  #",
                "#############"
            }),
            Def("Level_09", "Level 9 - Portal Gate Key", 32, new[]
            {
                "##########",
                "#@ $D A  #",
                "# ## p$ .#",
                "#     A. #",
                "##########"
            }),
            Def("Level_10", "Level 10 - Ice Portal Gate", 10, new[]
            {
                "##########",
                "#@ $~~A  #",
                "# ##     #",
                "#  A.pD$.#",
                "##########"
            }),
            Def("Level_11", "Level 11 - Portal Gate Switch", 18, new[]
            {
                "##########",
                "#@ $ A  .#",
                "# ## ##  #",
                "#  $pD A.#",
                "##########"
            }),
            Def("Level_12", "Level 12 - Ice Gate Switch", 7, new[]
            {
                "##########",
                "#@ $~~pD.#",
                "# ##     #",
                "#        #",
                "##########"
            }),
            Def("Level_13", "Level 13 - Reverse Portal Key", 16, new[]
            {
                "#############",
                "#@  $ pD.   #",
                "# #### ###  #",
                "# A  $ .A   #",
                "#############"
            }),
            Def("Level_14", "Level 14 - Frozen Key Detour", 34, new[]
            {
                "############",
                "#@ $~~pD.  #",
                "# ### ###  #",
                "#   $      #",
                "#   ### .  #",
                "############"
            }),
            Def("Level_15", "Level 15 - Portal Ice Split", 22, new[]
            {
                "#############",
                "#@ $ A~~~.  #",
                "# ## ###    #",
                "#   $ A     #",
                "#   ### .   #",
                "#############"
            }),
            Def("Level_16", "Level 16 - Locked Portal Bend", 26, new[]
            {
                "##############",
                "#@ $~~pD.    #",
                "# ### ###    #",
                "#  A $ #.A   #",
                "#       ##   #",
                "##############"
            }),
            Def("Level_17", "Level 17 - Portal Courtyard Lock", 48, new[]
            {
                "#########",
                "#@  #  .#",
                "# $ #A  #",
                "# ~ # # #",
                "# ~pD $ #",
                "#   # A.#",
                "#       #",
                "#########"
            }),
            Def("Level_18", "Level 18 - Twin Room Relay", 34, new[]
            {
                "##########",
                "#@  #  . #",
                "# $~#A   #",
                "#  ~pD $ #",
                "# ## # A.#",
                "# .  $   #",
                "##########"
            }),
            Def("Level_19", "Level 19 - Vertical Ice Portal", 22, new[]
            {
                "######",
                "#   @#",
                "#  # #",
                "#  #$#",
                "##$  #",
                "## #A#",
                "##A#~#",
                "#  #~#",
                "#.  ~#",
                "#   .#",
                "######"
            }),
            Def("Level_20", "Level 20 - Ice Gate Switchback", 40, new[]
            {
                "##########",
                "#@ $~~pD.#",
                "# ### ## #",
                "#     $  #",
                "#   .    #",
                "##########"
            }),
            Def("Level_21", "Level 21 - Portal Gate Atrium", 22, new[]
            {
                "#########",
                "#@ $pD. #",
                "# ### # #",
                "#A$  #.A#",
                "#       #",
                "#########"
            }),
            Def("Level_22", "Level 22 - Return Ferry", 48, new[]
            {
                "############",
                "#@ A#    . #",
                "#####  # # #",
                "#.~~~$   # #",
                "##### A  $ #",
                "#####   #  #",
                "###### #   #",
                "############"
            }),
            Def("Level_23", "Level 23 - Ice Portal Loop", 27, new[]
            {
                "########",
                "#@   . #",
                "# $A~~.#",
                "# ## # #",
                "#  $ A #",
                "#      #",
                "########"
            }),
            Def("Level_24", "Level 24 - Gatehouse Exchange", 58, new[]
            {
                "###########",
                "#@ $ D  . #",
                "# ### # # #",
                "#   $p  . #",
                "#.  #$    #",
                "#         #",
                "###########"
            }),
            Def("Level_25", "Level 25 - Frozen Stopper Dock", 76, new[]
            {
                "############",
                "#@         #",
                "#  $~~~~$  #",
                "#  ### #   #",
                "#        . #",
                "#    $~~~  #",
                "#  ### # # #",
                "# #.    .  #",
                "############"
            }),
            Def("Level_26", "Level 26 - Portal Swap Vault", 90, new[]
            {
                "############",
                "#@   #  #  #",
                "#  # #A $ ##",
                "#.$ A#  #  #",
                "##$# #     #",
                "#    #     #",
                "#    #.   .#",
                "############"
            }),
            Def("Level_27", "Level 27 - Gate Relay Vault", 74, new[]
            {
                "############",
                "#@ $pD    .#",
                "#    #$    #",
                "#. A #A   ##",
                "#    # #   #",
                "#  # #$    #",
                "#   ##    .#",
                "############"
            }),
            Def("Level_28", "Level 28 - Fill Order", 160, new[]
            {
                "###############",
                "#    @        #",
                "# ###$# ### # #",
                "#   # # #   # #",
                "### #.  # ### #",
                "#   ##### .   #",
                "# #  .$   #$# #",
                "# # #####.# # #",
                "#   # $.# $ # #",
                "#     #       #",
                "###############"
            }),
            Def("Level_29", "Level 29 - Dual Warehouse", 220, new[]
            {
                "#################",
                "#               #",
                "# #### ### #### #",
                "# #.$. # # .$.# #",
                "# # ## # # ## # #",
                "# $ #       #   #",
                "### # ##### # ###",
                "#   #       # $ #",
                "# # ## # # ## # #",
                "# #@$. # # .$ # #",
                "#               #",
                "#################"
            }),
            Def("Level_30", "Level 30 - Split Depot", 300, new[]
            {
                "###################",
                "#                 #",
                "# ##### ### ##### #",
                "# #     # #     # #",
                "# # ### # # ### # #",
                "#   #   # #  .#.$ #",
                "### # ### ###$# ###",
                "# $ #  $@ # . $ # #",
                "# # ##### ##### # #",
                "# #.$. .# #     # #",
                "# #####$# # ##### #",
                "#                 #",
                "###################"
            }),
    };

    private static void FixMeta(string id, string newName, int par)
    {
        string path = $"Assets/Resources/Levels/{id}.asset";
        var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (ld == null)
        {
            Debug.LogWarning($"[LevelSetupUtility] {id} not found.");
            return;
        }

        ld.levelName = newName;
        ld.parMoves = par;
        EditorUtility.SetDirty(ld);
    }

    private static LevelData MakeLevel(LevelDefinition def)
    {
        string path = $"Assets/Resources/Levels/{def.Id}.asset";
        var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (ld == null)
        {
            ld = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(ld, path);
        }

        int width = def.Rows[0].Length;
        int height = def.Rows.Length;
        var tiles = new int[width * height];
        var portals = new Dictionary<char, List<Vector2Int>>();
        var plates = new List<Vector2Int>();
        var doors = new List<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            if (def.Rows[y].Length != width)
                throw new InvalidOperationException($"{def.Id} has inconsistent row width at row {y}.");

            for (int x = 0; x < width; x++)
            {
                char c = def.Rows[y][x];
                tiles[y * width + x] = TileFromChar(c);
                if (c == 'p') plates.Add(new Vector2Int(x, y));
                if (c == 'D') doors.Add(new Vector2Int(x, y));
                if (c >= 'A' && c <= 'C')
                {
                    if (!portals.TryGetValue(c, out var list))
                    {
                        list = new List<Vector2Int>();
                        portals[c] = list;
                    }
                    list.Add(new Vector2Int(x, y));
                }
            }
        }

        ld.levelId = def.Id;
        ld.levelName = def.Name;
        ld.author = "built-in";
        ld.parMoves = def.ParMoves;
        ld.width = width;
        ld.height = height;
        ld.tiles = tiles;
        ld.portalPairs = BuildPortalPairs(def.Id, portals);
        ld.doorLinks = BuildDoorLinks(plates, doors);

        EditorUtility.SetDirty(ld);
        return ld;
    }

    private static int TileFromChar(char c)
    {
        if (c >= 'A' && c <= 'C') return (int)TileType.Portal;

        switch (c)
        {
            case 'x': return (int)TileType.Empty;
            case '#': return (int)TileType.Wall;
            case ' ': return (int)TileType.Floor;
            case '@': return (int)TileType.Player;
            case '$': return (int)TileType.Box;
            case '.': return (int)TileType.Goal;
            case '*': return (int)TileType.BoxOnGoal;
            case '~': return (int)TileType.Ice;
            case 'p': return (int)TileType.PressurePlate;
            case 'D': return (int)TileType.DoorClosed;
            default:
                throw new InvalidOperationException($"Unsupported level tile '{c}'.");
        }
    }

    private static PortalPair[] BuildPortalPairs(string id, Dictionary<char, List<Vector2Int>> portals)
    {
        var pairs = new List<PortalPair>();
        for (char c = 'A'; c <= 'C'; c++)
        {
            if (!portals.TryGetValue(c, out var positions)) continue;
            if (positions.Count != 2)
                throw new InvalidOperationException($"{id} portal {c} has {positions.Count} endpoints.");

            pairs.Add(new PortalPair
            {
                posA = positions[0],
                posB = positions[1],
                colorId = c - 'A'
            });
        }
        return pairs.ToArray();
    }

    private static DoorLink[] BuildDoorLinks(List<Vector2Int> plates, List<Vector2Int> doors)
    {
        if (plates.Count == 0 && doors.Count == 0) return Array.Empty<DoorLink>();
        if (plates.Count != 1 || doors.Count != 1)
            throw new InvalidOperationException("This generator currently expects one pressure plate for one door.");

        return new[]
        {
            new DoorLink { platePos = plates[0], doorPos = doors[0] }
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
