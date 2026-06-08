using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditorUI : MonoBehaviour
{
    [Header("Toolbar")]
    public Button btnBack;
    public Button btnNew;
    public Button btnUndo;
    public Button btnRedo;
    public Button btnClear;
    public Button btnSave;
    public Button btnTestPlay;
    public TMP_InputField levelNameInput;
    public TMP_InputField parMovesInput;

    [Header("Resize")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button btnResize;

    [Header("Brush Bar")]
    public Button[] brushButtons;   // indices 0-11 = TileType values; 12 = Erase

    [Header("Validation Panel")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI boxCountText;
    public TextMeshProUGUI goalCountText;
    public TextMeshProUGUI portalStatusText;
    public TextMeshProUGUI doorStatusText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI warningsText;
    public TextMeshProUGUI hintText;

    [Header("Level List Panel")]
    public Transform levelListContent;
    public Button    btnLoadSelected;
    public Button    btnDelete;

    private LevelEditorManager _editor;
    private Color[] _brushOriginalColors;
    private Image[] _brushSelectionFrames;      // gold border overlay per brush button
    private int     _selectedBrushIdx = 1;      // default: Wall brush
    private string  _selectedLevelId  = null;
    private const int EraseBrushIdx   = 12;

    void Start()
    {
        _editor = LevelEditorManager.Instance;

        btnBack?.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            SceneTransition.LoadScene("MainMenu");
        });

        btnNew?.onClick.AddListener(() => _editor?.NewLevel(15, 15));

        btnUndo?.onClick.AddListener(() => _editor?.UndoLastAction());
        btnRedo?.onClick.AddListener(() => _editor?.RedoLastAction());
        btnClear?.onClick.AddListener(() => _editor?.ClearInterior());

        btnSave?.onClick.AddListener(() =>
        {
            SyncInputsToLevel();
            _editor?.Save();
            RefreshLevelList();
        });

        btnTestPlay?.onClick.AddListener(() =>
        {
            SyncInputsToLevel();
            _editor?.TestPlay();
        });

        btnResize?.onClick.AddListener(() =>
        {
            if (int.TryParse(widthInput?.text, out int w) &&
                int.TryParse(heightInput?.text, out int h))
                _editor?.ResizeLevel(w, h);
        });

        btnLoadSelected?.onClick.AddListener(OnLoadSelected);
        btnDelete?.onClick.AddListener(OnDeleteSelected);

        // Cache original brush button colors, then wire click handlers
        if (brushButtons != null)
        {
            _brushOriginalColors = new Color[brushButtons.Length];
            for (int i = 0; i < brushButtons.Length; i++)
            {
                var img = brushButtons[i]?.GetComponent<Image>();
                _brushOriginalColors[i] = img ? img.color : Color.gray;
            }

            // Create gold selection frame for each brush button
            _brushSelectionFrames = new Image[brushButtons.Length];
            for (int i = 0; i < brushButtons.Length; i++)
            {
                if (brushButtons[i] == null) continue;
                var frameGo = new GameObject("SelFrame");
                frameGo.transform.SetParent(brushButtons[i].transform, false);
                frameGo.transform.SetAsFirstSibling();
                var frameRT = frameGo.AddComponent<RectTransform>();
                frameRT.anchorMin = Vector2.zero;
                frameRT.anchorMax = Vector2.one;
                frameRT.offsetMin = new Vector2(-3f, -3f);
                frameRT.offsetMax = new Vector2(3f, 3f);
                var frameImg = frameGo.AddComponent<Image>();
                frameImg.color = new Color(0f, 0.9f, 1f);   // cyan — distinct from all brush colours
                frameGo.SetActive(false);
                _brushSelectionFrames[i] = frameImg;
            }

            for (int i = 0; i < brushButtons.Length; i++)
            {
                int idx = i;
                brushButtons[i]?.onClick.AddListener(() => SelectBrush(idx));
            }
        }

        SyncFromLevel();
        RefreshLevelList();
        SelectBrush(_selectedBrushIdx);
        InvokeRepeating(nameof(RefreshValidation), 0f, 0.3f);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SyncFromLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput) levelNameInput.text = level.levelName;
        if (parMovesInput)  parMovesInput.text  = Mathf.Max(1, level.parMoves).ToString();
        if (widthInput)     widthInput.text     = level.width.ToString();
        if (heightInput)    heightInput.text    = level.height.ToString();
    }

    public void ShowValidationError(string msg)
    {
        if (errorText) errorText.text = msg;
    }

    public void RefreshLevelList()
    {
        if (levelListContent == null) return;

        foreach (Transform child in levelListContent)
            Destroy(child.gameObject);

        _selectedLevelId = null;
        UpdateListButtons();

        // Force canvas layout so parent rect has valid width before adding children
        Canvas.ForceUpdateCanvases();
        var contentRT  = levelListContent as RectTransform;
        float entryW   = contentRT != null && contentRT.rect.width > 10f
                         ? contentRT.rect.width : 164f;   // fallback if layout not yet ready

        var metas = LevelSerializer.GetAllLevelMeta();
        foreach (var (id, name, modified) in metas)
        {
            string capturedId = id;
            var entryGo = new GameObject($"Entry_{id}");
            entryGo.transform.SetParent(levelListContent, false);

            // Fixed-width entry so text never wraps to single-char columns
            var rt = entryGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(entryW, 36f);
            var le = entryGo.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.preferredWidth  = entryW;
            le.flexibleWidth   = 1f;

            var img = entryGo.AddComponent<Image>();
            img.color = new Color(0.15f, 0.18f, 0.25f);

            var btn = entryGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => SelectListEntry(capturedId, img));

            var label = new GameObject("Label");
            label.transform.SetParent(entryGo.transform, false);
            var labelRt = label.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(6, 2);
            labelRt.offsetMax = new Vector2(-4, -2);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text               = $"{name}\n<size=10><color=#aaaaaa>{modified:yyyy-MM-dd}</color></size>";
            tmp.fontSize           = 12;
            tmp.color              = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;
            tmp.raycastTarget      = false;
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void SyncInputsToLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput)
            level.levelName = string.IsNullOrWhiteSpace(levelNameInput.text)
                ? "New Level" : levelNameInput.text.Trim();

        if (parMovesInput && int.TryParse(parMovesInput.text, out int par))
            level.parMoves = Mathf.Max(1, par);
    }

    private void SelectBrush(int idx)
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor == null || brushButtons == null) return;

        // Deselect previous: restore original colour and hide gold frame
        if (_selectedBrushIdx < brushButtons.Length)
        {
            var prevImg = brushButtons[_selectedBrushIdx]?.GetComponent<Image>();
            if (prevImg != null && _brushOriginalColors != null && _selectedBrushIdx < _brushOriginalColors.Length)
                prevImg.color = _brushOriginalColors[_selectedBrushIdx];
            if (_brushSelectionFrames != null && _selectedBrushIdx < _brushSelectionFrames.Length)
                _brushSelectionFrames[_selectedBrushIdx]?.gameObject.SetActive(false);
        }

        _selectedBrushIdx = idx;

        if (idx == EraseBrushIdx)
        {
            _editor.IsEraseMode = true;
        }
        else
        {
            _editor.IsEraseMode   = false;
            _editor.SelectedBrush = (TileType)idx;
        }

        // Highlight selected: brighten colour + show gold frame
        if (idx < brushButtons.Length)
        {
            var img = brushButtons[idx]?.GetComponent<Image>();
            if (img != null && _brushOriginalColors != null && idx < _brushOriginalColors.Length)
                img.color = _brushOriginalColors[idx] * 1.8f;   // brighten, clamped to 1 per channel
            if (_brushSelectionFrames != null && idx < _brushSelectionFrames.Length)
                _brushSelectionFrames[idx]?.gameObject.SetActive(true);
        }
    }

    private void SelectListEntry(string id, Image img)
    {
        // Deselect all entries
        if (levelListContent != null)
            foreach (Transform child in levelListContent)
            {
                var childImg = child.GetComponent<Image>();
                if (childImg) childImg.color = new Color(0.15f, 0.18f, 0.25f);
            }

        _selectedLevelId = id;
        if (img) img.color = new Color(0.22f, 0.45f, 0.88f);
        UpdateListButtons();
    }

    private void OnLoadSelected()
    {
        if (string.IsNullOrEmpty(_selectedLevelId)) return;
        var data = LevelSerializer.Load(_selectedLevelId);
        if (data != null)
        {
            _editor?.LoadLevelForEditing(data);
            SyncFromLevel();
        }
    }

    private void OnDeleteSelected()
    {
        if (string.IsNullOrEmpty(_selectedLevelId)) return;
        LevelSerializer.Delete(_selectedLevelId);
        _selectedLevelId = null;
        RefreshLevelList();
    }

    private void UpdateListButtons()
    {
        bool hasSelection = !string.IsNullOrEmpty(_selectedLevelId);
        if (btnLoadSelected) btnLoadSelected.interactable = hasSelection;
        if (btnDelete)       btnDelete.interactable       = hasSelection;
    }

    private void RefreshValidation()
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor?.editingLevel == null) return;

        // Mechanic-pairing hint takes priority over error/warning display
        string hint = _editor.GetPairingHint();
        if (hintText) hintText.text = hint ?? "";

        var v = _editor.GetValidation();

        if (playerCountText) playerCountText.text = $"{(v.PlayerCount == 1    ? "✓" : "✗")} Player: {v.PlayerCount}";
        if (boxCountText)    boxCountText.text    = $"{(v.BoxCount > 0 && v.BoxCount == v.GoalCount ? "✓" : "✗")} Boxes: {v.BoxCount}";
        if (goalCountText)   goalCountText.text   = $"{(v.GoalCount > 0 && v.BoxCount == v.GoalCount ? "✓" : "✗")} Goals: {v.GoalCount}";
        if (portalStatusText) portalStatusText.text = v.PortalsPaired ? "✓ Portals OK" : "✗ Portals unpaired";
        if (doorStatusText)   doorStatusText.text   = v.DoorsLinked   ? "✓ Doors OK"   : "✗ Doors unlinked";

        if (errorText)
        {
            errorText.text  = v.IsValid ? "" : v.ErrorMessage;
            errorText.color = Color.red;
        }

        if (warningsText)
        {
            warningsText.text  = v.Warnings != null && v.Warnings.Length > 0
                ? string.Join("\n", v.Warnings)
                : "";
            warningsText.color = new Color(1f, 0.75f, 0.1f);
        }

        bool canAct = v.IsValid;
        if (btnSave)     btnSave.interactable     = canAct;
        if (btnTestPlay) btnTestPlay.interactable = canAct;
    }
}
