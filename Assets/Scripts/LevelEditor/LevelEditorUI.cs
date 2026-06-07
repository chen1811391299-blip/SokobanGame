using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditorUI : MonoBehaviour
{
    [Header("Toolbar")]
    public Button btnBack;
    public Button btnNew;
    public Button btnLoadSaved;
    public Button btnSave;
    public Button btnTestPlay;
    public TMP_InputField levelNameInput;
    public TMP_InputField parMovesInput;

    [Header("Brush Buttons")]
    public Button[] brushButtons;

    [Header("Validation")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI boxCountText;
    public TextMeshProUGUI goalCountText;
    public TextMeshProUGUI portalStatusText;
    public TextMeshProUGUI doorStatusText;
    public TextMeshProUGUI errorText;

    [Header("Map Size")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button btnResize;

    private LevelEditorManager _editor;

    void Start()
    {
        _editor = LevelEditorManager.Instance;
        EnsureOptionalControls();

        btnBack?.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            SceneTransition.LoadScene("MainMenu");
        });

        btnNew?.onClick.AddListener(() => _editor?.NewLevel(8, 8));
        btnLoadSaved?.onClick.AddListener(() => _editor?.LoadLatestSaved());

        btnSave?.onClick.AddListener(() =>
        {
            SyncInputsToLevel();
            _editor?.Save();
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
            {
                _editor?.NewLevel(Mathf.Clamp(w, 3, 20), Mathf.Clamp(h, 3, 20));
            }
        });

        if (brushButtons != null)
        {
            for (int i = 0; i < brushButtons.Length; i++)
            {
                int idx = i;
                brushButtons[i]?.onClick.AddListener(() => SelectBrush(idx));
            }
        }

        SyncFromLevel();
        InvokeRepeating(nameof(RefreshValidation), 0f, 0.3f);
    }

    private void EnsureOptionalControls()
    {
        var toolbar = GameObject.Find("Toolbar")?.transform;
        if (toolbar == null) return;

        btnBack ??= FindButton(toolbar, "BtnBack") ??
            CreateToolbarButton(toolbar, "BtnBack", "Back", new Vector2(720f, -7.5f), new Vector2(80f, 40f));
        btnLoadSaved ??= FindButton(toolbar, "BtnLoadSaved") ??
            CreateToolbarButton(toolbar, "BtnLoadSaved", "Load", new Vector2(805f, -7.5f), new Vector2(80f, 40f));
        parMovesInput ??= FindInput(toolbar, "ParMovesInput") ??
            CreateToolbarInput(toolbar, "ParMovesInput", new Vector2(890f, -7.5f), new Vector2(70f, 40f), "20");
    }

    public void SyncFromLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput) levelNameInput.text = level.levelName;
        if (parMovesInput) parMovesInput.text = Mathf.Max(1, level.parMoves).ToString();
        if (widthInput) widthInput.text = level.width.ToString();
        if (heightInput) heightInput.text = level.height.ToString();
    }

    private void SyncInputsToLevel()
    {
        _editor ??= LevelEditorManager.Instance;
        var level = _editor?.editingLevel;
        if (level == null) return;

        if (levelNameInput)
            level.levelName = string.IsNullOrWhiteSpace(levelNameInput.text) ? "New Level" : levelNameInput.text.Trim();

        if (parMovesInput && int.TryParse(parMovesInput.text, out int par))
            level.parMoves = Mathf.Max(1, par);
    }

    private void SelectBrush(int idx)
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor != null)
            _editor.SelectedBrush = (TileType)idx;
    }

    void RefreshValidation()
    {
        _editor ??= LevelEditorManager.Instance;
        if (_editor?.editingLevel == null) return;

        var v = _editor.GetValidation();
        if (playerCountText) playerCountText.text = $"Player: {v.PlayerCount}";
        if (boxCountText) boxCountText.text = $"Boxes: {v.BoxCount}";
        if (goalCountText) goalCountText.text = $"Goals: {v.GoalCount}";
        if (portalStatusText) portalStatusText.text = v.PortalsPaired ? "OK Portals paired" : "ERR Unpaired portals";
        if (doorStatusText) doorStatusText.text = v.DoorsLinked ? "OK Doors linked" : "ERR Doors unlinked";
        if (errorText)
            errorText.text = v.IsValid ? "" : v.ErrorMessage;
    }

    public void ShowValidationError(string msg)
    {
        if (errorText) errorText.text = msg;
    }

    private static Button FindButton(Transform parent, string name) =>
        parent.Find(name)?.GetComponent<Button>();

    private static TMP_InputField FindInput(Transform parent, string name) =>
        parent.Find(name)?.GetComponent<TMP_InputField>();

    private static Button CreateToolbarButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        go.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f);
        var button = go.AddComponent<Button>();
        CreateToolbarLabel(go.transform, label, 17);
        return button;
    }

    private static TMP_InputField CreateToolbarInput(Transform parent, string name, Vector2 position, Vector2 size, string value)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

        var textArea = new GameObject("TextArea");
        textArea.transform.SetParent(go.transform, false);
        var textAreaRt = textArea.AddComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(4f, 2f);
        textAreaRt.offsetMax = new Vector2(-4f, -2f);
        textArea.AddComponent<RectMask2D>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(textArea.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 16;
        tmp.color = Color.white;

        var input = go.AddComponent<TMP_InputField>();
        input.textComponent = tmp;
        input.textViewport = textAreaRt;
        input.text = value;
        return input;
    }

    private static void CreateToolbarLabel(Transform parent, string label, int fontSize)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
