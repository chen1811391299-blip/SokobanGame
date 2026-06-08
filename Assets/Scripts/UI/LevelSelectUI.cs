using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    public Transform levelListParent;
    public GameObject levelButtonPrefab;
    public Button btnBack;
    public Toggle toggleModifier;
    public TextMeshProUGUI modifierPreviewText;

    private readonly List<Button> _actButtons = new();
    private Transform _gridParent;
    private TextMeshProUGUI _progressText;
    private int _selectedAct;

    private static readonly string[] ActNames =
    {
        "Simple",
        "Medium",
        "Hard",
        "Remix",
        "Finale",
        "Custom"
    };

    private static readonly Color BgColor = new(0.07f, 0.08f, 0.12f);
    private static readonly Color PanelColor = new(0.10f, 0.12f, 0.18f, 0.96f);
    private static readonly Color CardColor = new(0.15f, 0.18f, 0.26f, 1f);
    private static readonly Color AccentColor = new(0.25f, 0.46f, 0.95f, 1f);
    private static readonly Color LockedColor = new(0.16f, 0.17f, 0.20f, 0.78f);

    void Start()
    {
        ModifierManager.Instance?.ClearModifier();
        BuildLayout();
        PopulateLevels();
        SelectAct(0);
    }

    private void BuildLayout()
    {
        var root = GetComponentInParent<Canvas>()?.transform ?? transform;
        ClearChildren(root);

        var background = CreateRect(root, "LevelSelectBackground", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, BgColor);

        var content = CreateRect(background.transform, "Content", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1360f, 860f), Color.clear);

        var header = CreateRect(content.transform, "Header", new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(0f, -90f), new Vector2(0f, 180f), Color.clear);

        btnBack = CreateButton(header.transform, "BackButton", "Back", new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(64f, -54f), new Vector2(112f, 48f), new Color(0.18f, 0.22f, 0.32f));
        btnBack.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            SceneTransition.LoadScene("MainMenu");
        });

        var titleGroup = CreateRect(header.transform, "TitleGroup", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(452f, -72f), new Vector2(640f, 118f), Color.clear);
        CreateText(titleGroup.transform, "Title", "Select Level", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(320f, -28f), new Vector2(640f, 54f), 34, TextAlignmentOptions.Left);
        _progressText = CreateText(titleGroup.transform, "Progress", "", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(320f, -82f), new Vector2(640f, 34f), 18, TextAlignmentOptions.Left);

        BuildModifierPanel(header.transform);
        BuildActTabs(content.transform);
        BuildGrid(content.transform);
    }

    private void BuildModifierPanel(Transform parent)
    {
        var panel = CreateRect(parent, "ModifierPanel", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-250f, -76f), new Vector2(500f, 112f), new Color(0.09f, 0.11f, 0.16f, 0.95f));

        var toggleGo = CreateRect(panel.transform, "ModifierToggle", new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(36f, 36f), new Color(0.20f, 0.23f, 0.30f));
        var check = CreateRect(toggleGo.transform, "Check", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, AccentColor);
        check.GetComponent<RectTransform>().offsetMin = new Vector2(7f, 7f);
        check.GetComponent<RectTransform>().offsetMax = new Vector2(-7f, -7f);

        toggleModifier = panel.gameObject.AddComponent<Toggle>();
        toggleModifier.targetGraphic = toggleGo.GetComponent<Image>();
        toggleModifier.graphic = check.GetComponent<Image>();
        toggleModifier.SetIsOnWithoutNotify(false);
        toggleModifier.onValueChanged.AddListener(OnToggleModifier);

        CreateText(panel.transform, "ModifierTitle", "Challenge Modifier", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(276f, -28f), new Vector2(400f, 28f), 17, TextAlignmentOptions.Left);
        modifierPreviewText = CreateText(panel.transform, "ModifierPreview", "Off", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(276f, -78f), new Vector2(400f, 46f), 14, TextAlignmentOptions.Left);
        modifierPreviewText.color = new Color(0.72f, 0.76f, 0.86f);
    }

    private void BuildActTabs(Transform parent)
    {
        var tabs = CreateRect(parent, "ActTabs", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -236f), new Vector2(0f, 58f), Color.clear);
        var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        _actButtons.Clear();
        for (int i = 0; i < ActNames.Length; i++)
        {
            int act = i;
            var button = CreateButton(tabs.transform, $"Act{act + 1}Button", $"Act {act + 1}  {ActNames[act]}",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.13f, 0.15f, 0.22f));
            button.onClick.AddListener(() => SelectAct(act));
            _actButtons.Add(button);
        }
    }

    private void BuildGrid(Transform parent)
    {
        var panel = CreateRect(parent, "LevelPanel", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, -326f), PanelColor);
        panel.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 0f);
        panel.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -326f);

        var viewport = CreateRect(panel.transform, "Viewport", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, Color.clear);
        viewport.GetComponent<RectTransform>().offsetMin = new Vector2(28f, 28f);
        viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-28f, -28f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        var content = CreateRect(viewport.transform, "Grid", new Vector2(0f, 1f), new Vector2(1f, 1f),
            Vector2.zero, new Vector2(0f, 0f), Color.clear);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.pivot = new Vector2(0.5f, 1f);

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(236f, 138f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        _gridParent = content.transform;
        levelListParent = _gridParent;
    }

    private void PopulateLevels()
    {
        var lm = LevelManager.Instance;
        if (!lm || _gridParent == null) return;

        foreach (Transform child in _gridParent)
            Destroy(child.gameObject);

        int completed = 0;
        int totalStars = 0;
        int maxStars = lm.LevelCount * 3;

        for (int i = 0; i < lm.LevelCount; i++)
        {
            var data = lm.GetLevel(i);
            if (data == null) continue;
            if (SaveManager.IsCompleted(data.levelId)) completed++;
            totalStars += SaveManager.GetStars(data.levelId, data.parMoves);

            bool isCustom = data.author != "built-in";
            int act = isCustom ? ActNames.Length - 1 : Mathf.Clamp(i / 6, 0, ActNames.Length - 2);
            var card = CreateLevelCard(_gridParent, i, data, IsUnlocked(lm, i), act, isCustom);
            card.SetActive(act == _selectedAct);
        }

        if (_progressText)
            _progressText.text = $"{completed} / {lm.LevelCount} cleared    {totalStars} / {maxStars} stars";

        // Empty-state label for Custom tab — shown by SelectAct when no custom cards exist
        bool hasCustomCards = false;
        for (int i = 0; i < lm.LevelCount; i++)
        {
            var d2 = lm.GetLevel(i);
            if (d2 != null && d2.author != "built-in") { hasCustomCards = true; break; }
        }
        if (!hasCustomCards)
        {
            var emptyGo = new GameObject("CustomEmptyLabel_Act6");
            emptyGo.transform.SetParent(_gridParent, false);
            var eRT = emptyGo.AddComponent<RectTransform>();
            eRT.sizeDelta = new Vector2(600f, 80f);
            var eTMP = emptyGo.AddComponent<TextMeshProUGUI>();
            eTMP.text      = "No custom levels yet.\nGo to Level Editor to create one.";
            eTMP.fontSize  = 18;
            eTMP.alignment = TextAlignmentOptions.Center;
            eTMP.color     = new Color(0.60f, 0.64f, 0.72f);
            eTMP.raycastTarget = false;
            emptyGo.SetActive(false);
        }
    }

    private GameObject CreateLevelCard(Transform parent, int index, LevelData data, bool unlocked, int act, bool isCustom = false)
    {
        Color bgColor = isCustom ? CardColor : (unlocked ? CardColor : LockedColor);
        var go = CreateRect(parent, $"LevelCard_{index + 1:00}", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, bgColor);
        go.name = $"LevelCard_{index + 1:00}_Act{act + 1}";

        Color numColor = isCustom ? new Color(0.28f, 0.50f, 0.70f)
                       : (unlocked ? AccentColor : new Color(0.28f, 0.30f, 0.36f));
        var numberBg = CreateRect(go.transform, "Number", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -28f), new Vector2(42f, 42f), numColor);
        CreateText(numberBg.transform, "NumberText", (index + 1).ToString("00"), Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, 16, TextAlignmentOptions.Center);

        var title = StripLevelPrefix(data.levelName, index + 1);
        CreateText(go.transform, "LevelName", title, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(62f, -18f), new Vector2(-18f, 46f), 15, TextAlignmentOptions.Left);

        // Meta row: custom levels show author; built-in show best/par
        string metaStr;
        if (isCustom)
            metaStr = $"by {data.author ?? "player"}";
        else
        {
            var bestMoves = SaveManager.GetBestMoves(data.levelId);
            metaStr = bestMoves == int.MaxValue ? $"Par {data.parMoves}" : $"Best {bestMoves} / Par {data.parMoves}";
        }
        var meta = CreateText(go.transform, "Meta", metaStr, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -76f), new Vector2(-20f, 28f), 13, TextAlignmentOptions.Left);
        meta.color = isCustom ? new Color(0.60f, 0.85f, 1.00f) : new Color(0.72f, 0.76f, 0.86f);

        // Status row: custom levels show no stars; built-in show star rating or LOCKED
        if (!isCustom)
        {
            int stars = SaveManager.GetStars(data.levelId, data.parMoves);
            string starText = unlocked ? StarText(stars) : "LOCKED";
            var star = CreateText(go.transform, "Stars", starText, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(20f, 18f), new Vector2(-20f, 28f), 14, TextAlignmentOptions.Left);
            star.color = unlocked ? new Color(1f, 0.78f, 0.22f) : new Color(0.62f, 0.65f, 0.72f);
        }

        bool canPlay = isCustom || unlocked;
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.interactable = canPlay;
        if (canPlay)
        {
            int idx = index;
            button.onClick.AddListener(() =>
            {
                AudioManager.PlayMenu();
                LevelManager.Instance.SetCurrent(idx);
                LevelManager.IsTestMode = false;
                SceneTransition.LoadScene("Gameplay");
            });
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
        colors.pressedColor = new Color(0.86f, 0.90f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        return go;
    }

    private void SelectAct(int act)
    {
        _selectedAct = Mathf.Clamp(act, 0, ActNames.Length - 1);
        for (int i = 0; i < _actButtons.Count; i++)
        {
            var image = _actButtons[i].GetComponent<Image>();
            if (image) image.color = i == _selectedAct ? AccentColor : new Color(0.13f, 0.15f, 0.22f);
        }

        if (_gridParent == null) return;
        foreach (Transform child in _gridParent)
            child.gameObject.SetActive(child.name.EndsWith($"Act{_selectedAct + 1}"));
    }

    private static bool IsUnlocked(LevelManager lm, int index)
    {
        var data = lm.GetLevel(index);
        if (data == null) return false;
        bool isCustom = data.author != "built-in";
        return isCustom || index == 0 || SaveManager.IsCompleted(lm.GetLevel(index - 1).levelId);
    }

    private void OnToggleModifier(bool on)
    {
        if (on)
        {
            ModifierManager.Instance?.RollModifier();
            if (modifierPreviewText && ModifierManager.Instance != null)
            {
                modifierPreviewText.text =
                    ModifierManager.Instance.GetDisplayName() + "\n" +
                    ModifierManager.Instance.GetDescription();
            }
        }
        else
        {
            ModifierManager.Instance?.ClearModifier();
            if (modifierPreviewText) modifierPreviewText.text = "Off";
        }
    }

    private static string StripLevelPrefix(string levelName, int number)
    {
        string prefix = $"Level {number} - ";
        return levelName.StartsWith(prefix) ? levelName.Substring(prefix.Length) : levelName;
    }

    private static string StarText(int stars)
    {
        return stars switch
        {
            3 => "***",
            2 => "**-",
            1 => "*--",
            _ => "---"
        };
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        if (color.a > 0f)
            go.AddComponent<Image>().color = color;
        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, size, color);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        CreateText(go.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            16, TextAlignmentOptions.Center);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
        int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }
}
