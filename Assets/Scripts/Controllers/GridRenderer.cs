using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    [Header("Tile Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject goalPrefab;
    public GameObject icePrefab;
    public GameObject pressurePlatePrefab;
    public GameObject doorOpenPrefab;
    public GameObject doorClosedPrefab;
    public GameObject portalBluePrefab;
    public GameObject portalOrangePrefab;
    public GameObject portalGreenPrefab;
    public GameObject playerPrefab;
    public GameObject boxPrefab;
    public GameObject boxOnGoalPrefab;

    [Header("配置")]
    public float tileSize = 1f;

    private SokobanGrid  _grid;
    private readonly List<GameObject>                _staticObjs = new();
    private readonly Dictionary<Vector2Int, GameObject> _boxObjs = new();
    private GameObject   _playerObj;

    // 传送门位置→对应prefab索引（0蓝/1橙/2绿）
    private readonly Dictionary<Vector2Int, int> _portalColorMap = new();

    public void Init(SokobanGrid grid)
    {
        _grid = grid;
        BuildPortalColorMap();
        Rebuild();
    }

    private void BuildPortalColorMap()
    {
        _portalColorMap.Clear();
        int colorIdx = 0;
        var visited  = new HashSet<Vector2Int>();
        foreach (var kv in _grid.PortalLinks)
        {
            if (visited.Contains(kv.Key)) continue;
            _portalColorMap[kv.Key]   = colorIdx;
            _portalColorMap[kv.Value] = colorIdx;
            visited.Add(kv.Key);
            visited.Add(kv.Value);
            colorIdx = (colorIdx + 1) % 3;
        }
    }

    public void Rebuild()
    {
        foreach (var go in _staticObjs) if (go) Destroy(go);
        foreach (var kv in _boxObjs)    if (kv.Value) Destroy(kv.Value);
        if (_playerObj) Destroy(_playerObj);
        _staticObjs.Clear();
        _boxObjs.Clear();

        for (int y = 0; y < _grid.Height; y++)
        for (int x = 0; x < _grid.Width;  x++)
        {
            var pos  = new Vector2Int(x, y);
            var tile = _grid.GetBase(pos);
            var go   = SpawnStaticTile(tile, pos);
            if (go) _staticObjs.Add(go);
        }

        foreach (var bp in _grid.Boxes)
        {
            var prefab = _grid.Goals.Contains(bp) ? (boxOnGoalPrefab ? boxOnGoalPrefab : boxPrefab) : boxPrefab;
            _boxObjs[bp] = Spawn(prefab, bp);
        }

        _playerObj = Spawn(playerPrefab, _grid.PlayerPos);
    }

    private GameObject SpawnStaticTile(TileType tile, Vector2Int pos)
    {
        GameObject prefab;
        if (tile == TileType.Portal)
        {
            int colorIdx = _portalColorMap.TryGetValue(pos, out var c) ? c : 0;
            prefab = colorIdx switch
            {
                1 => portalOrangePrefab ? portalOrangePrefab : portalBluePrefab,
                2 => portalGreenPrefab  ? portalGreenPrefab  : portalBluePrefab,
                _ => portalBluePrefab
            };
        }
        else
        {
            prefab = tile switch
            {
                TileType.Wall          => wallPrefab,
                TileType.Floor         => floorPrefab,
                TileType.Goal          => goalPrefab,
                TileType.Ice           => icePrefab,
                TileType.PressurePlate => pressurePlatePrefab,
                TileType.DoorOpen      => doorOpenPrefab,
                TileType.DoorClosed    => doorClosedPrefab,
                _                      => null
            };
        }
        return prefab ? Spawn(prefab, pos) : null;
    }

    private GameObject Spawn(GameObject prefab, Vector2Int gridPos)
    {
        if (!prefab) return null;
        var worldPos = new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
        return Instantiate(prefab, worldPos, Quaternion.identity, transform);
    }

    public void ApplyFogOfWar(Vector2Int center, int radius)
    {
        foreach (var go in _staticObjs)
        {
            if (!go) continue;
            var gp = WorldToGrid(go.transform.position);
            go.SetActive(IsInFogRange(gp, center, radius));
        }

        foreach (var kv in _boxObjs)
        {
            if (kv.Value)
                kv.Value.SetActive(IsInFogRange(kv.Key, center, radius));
        }

        if (_playerObj)
            _playerObj.SetActive(true);
    }

    public void ClearFog()
    {
        foreach (var go in _staticObjs) if (go) go.SetActive(true);
        foreach (var kv in _boxObjs) if (kv.Value) kv.Value.SetActive(true);
        if (_playerObj) _playerObj.SetActive(true);
    }

    private static bool IsInFogRange(Vector2Int pos, Vector2Int center, int radius) =>
        Mathf.Abs(pos.x - center.x) + Mathf.Abs(pos.y - center.y) <= radius;

    private Vector2Int WorldToGrid(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / tileSize),
                       Mathf.RoundToInt(worldPos.z / tileSize));
}
