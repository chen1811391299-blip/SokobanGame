using NUnit.Framework;
using UnityEngine;

public class SokobanGridTests
{
    private static SokobanGrid MakeMovementGrid()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 5;
        data.height = 3;
        data.tiles = new int[15];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.Floor);
        data.SetTile(3, 1, TileType.Box);
        data.portalPairs = new PortalPair[0];
        data.doorLinks = new DoorLink[0];

        var grid = new SokobanGrid();
        grid.LoadLevel(data);
        return grid;
    }

    private static SokobanGrid MakeCompletionGrid()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 6;
        data.height = 3;
        data.tiles = new int[18];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.Floor);
        data.SetTile(3, 1, TileType.Box);
        data.SetTile(4, 1, TileType.Goal);
        data.portalPairs = new PortalPair[0];
        data.doorLinks = new DoorLink[0];

        var grid = new SokobanGrid();
        grid.LoadLevel(data);
        return grid;
    }

    [Test]
    public void LoadLevel_SetsPlayerPos()
    {
        var grid = MakeMovementGrid();
        Assert.AreEqual(new Vector2Int(1, 1), grid.PlayerPos);
    }

    [Test]
    public void TryMove_IntoFloor_Succeeds()
    {
        var grid = MakeMovementGrid();
        bool moved = grid.TryMove(Vector2Int.right);
        Assert.IsTrue(moved);
        Assert.AreEqual(new Vector2Int(2, 1), grid.PlayerPos);
    }

    [Test]
    public void TryMove_IntoWall_Fails()
    {
        var grid = MakeMovementGrid();
        bool moved = grid.TryMove(Vector2Int.left);
        Assert.IsFalse(moved);
        Assert.AreEqual(new Vector2Int(1, 1), grid.PlayerPos);
    }

    [Test]
    public void TryMove_PushBoxIntoWall_Fails()
    {
        var grid = MakeMovementGrid();
        grid.TryMove(Vector2Int.right);
        grid.TryMove(Vector2Int.right);
        bool blocked = grid.TryMove(Vector2Int.right);
        Assert.IsFalse(blocked);
    }

    [Test]
    public void TryMove_PushBoxOntoGoal_IsComplete()
    {
        var grid = MakeCompletionGrid();
        grid.TryMove(Vector2Int.right);
        grid.TryMove(Vector2Int.right);
        Assert.IsTrue(grid.IsComplete());
    }

    [Test]
    public void IsComplete_WithoutBoxOnGoal_IsFalse()
    {
        var grid = MakeCompletionGrid();
        Assert.IsFalse(grid.IsComplete());
    }

    [Test]
    public void GetSnapshot_And_LoadSnapshot_RestoresState()
    {
        var grid = MakeCompletionGrid();
        var snapshot = grid.GetSnapshot(0);
        grid.TryMove(Vector2Int.right);
        grid.LoadSnapshot(snapshot);
        Assert.AreEqual(new Vector2Int(1, 1), grid.PlayerPos);
        Assert.IsFalse(grid.IsComplete());
    }

    [Test]
    public void LevelDataJson_PreservesParMoves()
    {
        var src = ScriptableObject.CreateInstance<LevelData>();
        src.levelId = "test-par";
        src.levelName = "Par Test";
        src.width = 3;
        src.height = 3;
        src.tiles = new int[9];
        src.parMoves = 7;

        var json = LevelDataJson.From(src);
        Assert.AreEqual(7, json.parMoves, "From() must copy parMoves");

        var restored = json.ToLevelData();
        Assert.AreEqual(7, restored.parMoves, "ToLevelData() must restore parMoves");
    }

    [Test]
    public void LevelDataJson_InvalidTiles_NormalizesToPlayableShape()
    {
        var json = new LevelDataJson
        {
            id = "bad-tiles",
            name = "Bad Tiles",
            width = 4,
            height = 4,
            tiles = new[] { (int)TileType.Player }
        };

        var restored = json.ToLevelData();

        Assert.AreEqual(16, restored.tiles.Length);
        Assert.AreEqual(TileType.Wall, restored.GetTile(0, 0));
        Assert.AreEqual(TileType.Floor, restored.GetTile(1, 1));
    }

    [Test]
    public void PortalMechanic_PlayerTeleports()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 8;
        data.height = 3;
        data.tiles = new int[24];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.Floor);
        data.SetTile(3, 1, TileType.Portal);
        data.SetTile(4, 1, TileType.Floor);
        data.SetTile(5, 1, TileType.Portal);
        data.SetTile(6, 1, TileType.Floor);
        data.portalPairs = new[]
        {
            new PortalPair { posA = new Vector2Int(3, 1), posB = new Vector2Int(5, 1), colorId = 0 }
        };
        data.doorLinks = new DoorLink[0];

        var grid = new SokobanGrid();
        grid.LoadLevel(data);
        grid.TryMove(Vector2Int.right);
        grid.TryMove(Vector2Int.right);

        Assert.AreEqual(new Vector2Int(6, 1), grid.PlayerPos);
    }

    [Test]
    public void PressurePlate_PlayerStandingOnPlate_OpensDoor()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 5;
        data.height = 3;
        data.tiles = new int[15];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.PressurePlate);
        data.SetTile(3, 1, TileType.DoorClosed);
        data.portalPairs = new PortalPair[0];
        data.doorLinks = new[]
        {
            new DoorLink { platePos = new Vector2Int(2, 1), doorPos = new Vector2Int(3, 1) }
        };

        var grid = new SokobanGrid();
        grid.LoadLevel(data);

        Assert.IsFalse(grid.DoorStates[0]);
        Assert.IsTrue(grid.TryMove(Vector2Int.right));
        Assert.IsTrue(grid.DoorStates[0]);
        Assert.AreEqual(TileType.DoorOpen, grid.GetBase(new Vector2Int(3, 1)));
    }

    [Test]
    public void PressurePlate_DoorStaysOpenWhilePlayerOccupiesDoor()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 5;
        data.height = 3;
        data.tiles = new int[15];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.PressurePlate);
        data.SetTile(3, 1, TileType.DoorClosed);
        data.portalPairs = new PortalPair[0];
        data.doorLinks = new[]
        {
            new DoorLink { platePos = new Vector2Int(2, 1), doorPos = new Vector2Int(3, 1) }
        };

        var grid = new SokobanGrid();
        grid.LoadLevel(data);

        Assert.IsTrue(grid.TryMove(Vector2Int.right));
        Assert.IsTrue(grid.TryMove(Vector2Int.right));
        Assert.AreEqual(new Vector2Int(3, 1), grid.PlayerPos);
        Assert.AreEqual(TileType.DoorOpen, grid.GetBase(new Vector2Int(3, 1)));
        Assert.IsTrue(grid.DoorStates[0]);
    }

    [Test]
    public void LevelValidator_DoorWithoutLink_IsInvalid()
    {
        var data = MakeValidationGrid();
        data.SetTile(4, 1, TileType.DoorClosed);
        data.doorLinks = new DoorLink[0];

        var result = LevelValidator.Validate(data);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(result.DoorsLinked);
    }

    [Test]
    public void LevelValidator_LinkedDoor_IsValid()
    {
        var data = MakeValidationGrid();
        data.SetTile(4, 1, TileType.PressurePlate);
        data.SetTile(5, 1, TileType.DoorClosed);
        data.doorLinks = new[]
        {
            new DoorLink { platePos = new Vector2Int(4, 1), doorPos = new Vector2Int(5, 1) }
        };

        var result = LevelValidator.Validate(data);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.IsTrue(result.DoorsLinked);
    }

    private static LevelData MakeValidationGrid()
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.width = 7;
        data.height = 3;
        data.tiles = new int[21];
        Fill(data, TileType.Wall);
        data.SetTile(1, 1, TileType.Player);
        data.SetTile(2, 1, TileType.Box);
        data.SetTile(3, 1, TileType.Goal);
        data.SetTile(4, 1, TileType.Floor);
        data.SetTile(5, 1, TileType.Floor);
        data.portalPairs = new PortalPair[0];
        data.doorLinks = new DoorLink[0];
        return data;
    }

    private static void Fill(LevelData data, TileType type)
    {
        for (int i = 0; i < data.tiles.Length; i++)
            data.tiles[i] = (int)type;
    }
}
