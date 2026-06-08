using NUnit.Framework;
using UnityEngine;

public class LevelValidatorTests
{
    // 5x5 level: wall border, player at (1,1), box at (2,1), goal at (3,1), parMoves=5
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
        d.doorLinks   = new DoorLink[0];
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
        StringAssert.Contains("default (20)", r.Warnings[r.Warnings.Length - 1]);
    }

    [Test]
    public void Warnings_CornerBox_Reported()
    {
        var d = MakeValid();
        // Remove original tiles
        d.SetTile(1, 1, TileType.Floor);   // remove Player
        d.SetTile(2, 1, TileType.Floor);   // remove Box
        d.SetTile(3, 1, TileType.Floor);   // remove Goal
        // Place new layout: player at (1,2), goal at (3,3), box at (1,1) [cornered: wall left+below]
        d.SetTile(1, 2, TileType.Player);
        d.SetTile(3, 3, TileType.Goal);
        d.SetTile(1, 1, TileType.Box);
        var r = LevelValidator.Validate(d);
        Assert.IsTrue(r.IsValid);
        bool hasCornerWarn = System.Array.Exists(r.Warnings, w => w.Contains("corner"));
        Assert.IsTrue(hasCornerWarn, "Expected corner-box warning");
    }

    [Test]
    public void Error_Unreachable_Floor_IsInvalid()
    {
        var d = MakeValid();
        // Move goal to isolated pocket: surround (3,3) with walls
        d.SetTile(3, 1, TileType.Floor);  // remove original goal
        d.SetTile(3, 3, TileType.Goal);   // goal in pocket
        d.SetTile(2, 3, TileType.Wall);   // seal left
        d.SetTile(3, 2, TileType.Wall);   // seal below
        // (3,3) is now unreachable: left=Wall, below=Wall, right=(4,3)=border Wall, above=(3,4)=border Wall
        var r = LevelValidator.Validate(d);
        Assert.IsFalse(r.IsValid);
        StringAssert.Contains("Unreachable", r.ErrorMessage);
    }
}
