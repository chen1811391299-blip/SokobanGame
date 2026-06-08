using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EditorGridView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler
{
    [Header("格子Prefab（带Image组件）")]
    public GameObject editorTilePrefab;
    public float      tileSize = 60f;

    private LevelData      _data;
    private GameObject[,]  _tiles;
    private RectTransform  _gridRect;

    private static readonly Color[] TileColors =
    {
        new Color(0.15f, 0.15f, 0.15f),  // Empty
        new Color(0.35f, 0.35f, 0.35f),  // Wall
        new Color(0.90f, 0.85f, 0.70f),  // Floor
        new Color(0.20f, 0.80f, 0.20f),  // Player
        new Color(0.80f, 0.50f, 0.10f),  // Box
        new Color(0.95f, 0.90f, 0.20f),  // Goal
        new Color(1.00f, 0.70f, 0.00f),  // BoxOnGoal
        new Color(0.70f, 0.90f, 1.00f),  // Ice
        new Color(0.50f, 1.00f, 0.50f),  // PressurePlate
        new Color(0.00f, 0.80f, 0.20f),  // DoorOpen
        new Color(0.80f, 0.10f, 0.10f),  // DoorClosed
        new Color(0.30f, 0.50f, 1.00f),  // Portal
    };

    void Awake() => _gridRect = GetComponent<RectTransform>();

    public void Rebuild(LevelData data)
    {
        _data = data;
        if (_tiles != null)
            foreach (var t in _tiles) if (t) Destroy(t);
        _tiles = new GameObject[data.width, data.height];
        for (int y = 0; y < data.height; y++)
        for (int x = 0; x < data.width;  x++)
            SpawnTile(x, y);
    }

    public void RefreshTile(int x, int y, LevelData data)
    {
        _data = data;
        if (_tiles == null || _tiles[x, y] == null) return;
        var img = _tiles[x, y].GetComponent<Image>();
        if (img) img.color = GetTileColor(data.GetTile(x, y));
    }

    private void SpawnTile(int x, int y)
    {
        var go = Instantiate(editorTilePrefab, transform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta        = new Vector2(tileSize - 1, tileSize - 1);
        rt.anchoredPosition = new Vector2(x * tileSize, y * tileSize);
        var img = go.GetComponent<Image>();
        if (img) img.color = GetTileColor(_data.GetTile(x, y));
        _tiles[x, y] = go;
    }

    private static Color GetTileColor(TileType t)
    {
        int idx = (int)t;
        return idx >= 0 && idx < TileColors.Length ? TileColors[idx] : Color.magenta;
    }

    public void OnPointerClick(PointerEventData e) => HandleInput(e);
    public void OnPointerDown(PointerEventData e)  { }
    public void OnDrag(PointerEventData e)          => HandleInput(e);

    private void HandleInput(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRect, e.position, e.pressEventCamera, out var local)) return;

        var rect = _gridRect.rect;
        int x = Mathf.FloorToInt((local.x - rect.xMin) / tileSize);
        int y = Mathf.FloorToInt((local.y - rect.yMin) / tileSize);
        if (_data == null || !_data.IsInBounds(x, y)) return;

        bool erase = e.button == PointerEventData.InputButton.Right
                     || (LevelEditorManager.Instance?.IsEraseMode ?? false);
        if (erase)
            LevelEditorManager.Instance?.EraseTile(x, y);
        else
            LevelEditorManager.Instance?.PlaceTile(x, y);
    }
}
