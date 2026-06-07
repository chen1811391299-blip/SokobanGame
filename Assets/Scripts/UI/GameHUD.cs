using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI moveCountText;
    public TextMeshProUGUI modifierText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI bestMovesText;
    public TextMeshProUGUI parMovesText;    // 目标步数提示

    [Header("Panels")]
    public GameObject levelCompletePanel;
    public GameObject backToEditorButton;
    public GameObject pausePanel;          // 暂停面板
    public GameObject allCompletePanel;    // 全通关面板
    public GameObject portalTutorialPanel;
    public GameObject iceTutorialPanel;
    public GameObject pressureGateTutorialPanel;

    [Header("Buttons")]
    public Button pauseButton;
    public Button restartButton;
    public Button menuButton;
    public Button nextLevelButton;

    [Header("Pause Buttons")]
    public Button pauseResumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;

    [Header("All Complete")]
    public TextMeshProUGUI totalStarsText;
    public Button          allCompleteBtnReplay;
    public Button          allCompleteBtnMenu;

    [Header("Mechanic Tutorials")]
    public Button          portalTutorialCloseButton;
    public Button          iceTutorialCloseButton;
    public Button          pressureGateTutorialCloseButton;

    private int _parMoves = 0;
    private Action _portalTutorialClosed;
    private Action _iceTutorialClosed;
    private Action _pressureGateTutorialClosed;

    void Start()
    {
        EnsureOptionalWidgets();

        pauseButton?.onClick.AddListener(()     => GameManager.Instance?.TogglePause());
        restartButton?.onClick.AddListener(()   => GameManager.Instance?.RestartLevel());
        menuButton?.onClick.AddListener(()      => GameManager.Instance?.GoToMainMenu());
        nextLevelButton?.onClick.AddListener(() => GameManager.Instance?.NextLevel());
        backToEditorButton?.GetComponent<Button>()?.onClick.AddListener(() => GameManager.Instance?.BackToEditor());
        levelCompletePanel?.SetActive(false);

        pausePanel?.SetActive(false);
        pauseResumeButton?.onClick.AddListener(()  => GameManager.Instance?.TogglePause());
        pauseRestartButton?.onClick.AddListener(() =>
        {
            GameManager.Instance?.TogglePause();
            GameManager.Instance?.RestartLevel();
        });
        pauseMenuButton?.onClick.AddListener(() =>
        {
            GameManager.Instance?.TogglePause();
            GameManager.Instance?.GoToMainMenu();
        });

        allCompletePanel?.SetActive(false);
        allCompleteBtnReplay?.onClick.AddListener(() => GameManager.Instance?.ReplayAll());
        allCompleteBtnMenu?.onClick.AddListener(()   => GameManager.Instance?.GoToMainMenu());
    }

    public void SetLevelName(string name)
    {
        if (levelNameText) levelNameText.text = name;
    }

    public void SetParMoves(int par)
    {
        _parMoves = par;
        if (parMovesText)
            parMovesText.text = par > 0 ? $"Par: {par}" : "";
    }

    public void UpdateMoveCount(int count)
    {
        if (moveCountText) moveCountText.text = $"Moves: {count}";
        if (parMovesText && _parMoves > 0)
            parMovesText.color = count > _parMoves
                ? Color.red
                : new Color(0.6f, 1f, 0.6f);
    }

    public void ShowLevelComplete(int moves, int stars, int best)
    {
        if (starsText)
            starsText.text = stars switch { 3 => "3 / 3 Stars", 2 => "2 / 3 Stars", _ => "1 / 3 Stars" };
        if (bestMovesText)
        {
            bestMovesText.text = best == int.MaxValue
                ? $"Moves: {moves}"
                : $"Moves: {moves}   Best: {best}";
        }
        levelCompletePanel?.SetActive(true);
    }

    public void HideLevelComplete() => levelCompletePanel?.SetActive(false);

    public void ShowPause(bool show) => pausePanel?.SetActive(show);

    public void ShowAllComplete(int totalStars, int maxStars)
    {
        if (totalStarsText) totalStarsText.text = $"Stars: {totalStars} / {maxStars}";
        allCompletePanel?.SetActive(true);
    }

    public void HideAllComplete() => allCompletePanel?.SetActive(false);

    public void ShowBackToEditorButton(bool show) => backToEditorButton?.SetActive(show);

    public void SetModifierText(string text)
    {
        if (modifierText) modifierText.text = text;
    }

    public void ShowPortalTutorial(Action onClosed)
    {
        EnsurePortalTutorial();
        _portalTutorialClosed = onClosed;
        portalTutorialPanel.transform.SetAsLastSibling();
        portalTutorialPanel.SetActive(true);
    }

    public void ShowIceTutorial(Action onClosed)
    {
        EnsureIceTutorial();
        _iceTutorialClosed = onClosed;
        iceTutorialPanel.transform.SetAsLastSibling();
        iceTutorialPanel.SetActive(true);
    }

    public void ShowPressureGateTutorial(Action onClosed)
    {
        EnsurePressureGateTutorial();
        _pressureGateTutorialClosed = onClosed;
        pressureGateTutorialPanel.transform.SetAsLastSibling();
        pressureGateTutorialPanel.SetActive(true);
    }

    private void EnsureOptionalWidgets()
    {
        var parent = GetComponentInParent<Canvas>()?.transform ?? transform;

        if (!parMovesText)
        {
            parMovesText = CreateText(parent, "ParMovesText", "", new Vector2(0f, 1f),
                new Vector2(10f, -100f), new Vector2(180f, 40f), 18, TextAlignmentOptions.Left);
        }

        if (!pauseButton)
        {
            pauseButton = CreateButton(parent, "PauseButton", "Pause", new Vector2(1f, 0f),
                new Vector2(-120f, 130f), new Vector2(170f, 45f));
        }

        if (!pausePanel)
        {
            pausePanel = CreatePanel(parent, "PausePanel", new Color(0f, 0f, 0f, 0.72f));
            CreateText(pausePanel.transform, "PauseTitle", "Paused", new Vector2(0.5f, 0.64f),
                Vector2.zero, new Vector2(320f, 70f), 38, TextAlignmentOptions.Center);
        }

        if (!pauseResumeButton)
            pauseResumeButton = CreateButton(pausePanel.transform, "PauseResumeButton", "Resume",
                new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(220f, 55f));
        if (!pauseRestartButton)
            pauseRestartButton = CreateButton(pausePanel.transform, "PauseRestartButton", "Restart",
                new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(220f, 55f));
        if (!pauseMenuButton)
            pauseMenuButton = CreateButton(pausePanel.transform, "PauseMenuButton", "Main Menu",
                new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(220f, 55f));
        pausePanel.SetActive(false);

        if (!allCompletePanel)
        {
            allCompletePanel = CreatePanel(parent, "AllCompletePanel", new Color(0f, 0f, 0f, 0.78f));
            CreateText(allCompletePanel.transform, "AllCompleteTitle", "All Levels Complete!",
                new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(520f, 70f), 38, TextAlignmentOptions.Center);
        }

        if (!totalStarsText)
            totalStarsText = CreateText(allCompletePanel.transform, "TotalStarsText", "Stars: 0 / 0",
                new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(360f, 45f), 24, TextAlignmentOptions.Center);
        if (!allCompleteBtnReplay)
            allCompleteBtnReplay = CreateButton(allCompletePanel.transform, "ReplayAllButton", "Replay",
                new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(220f, 55f));
        if (!allCompleteBtnMenu)
            allCompleteBtnMenu = CreateButton(allCompletePanel.transform, "AllCompleteMenuButton", "Main Menu",
                new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(220f, 55f));
        allCompletePanel.SetActive(false);
    }

    private void EnsurePortalTutorial()
    {
        if (portalTutorialPanel != null && portalTutorialCloseButton != null) return;

        var parent = GetComponentInParent<Canvas>()?.transform ?? transform;
        portalTutorialPanel = CreatePanel(parent, "PortalTutorialPanel", new Color(0f, 0f, 0f, 0.88f));

        CreateRect(portalTutorialPanel.transform, "PortalTutorialShadow", new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(920f, 550f), new Color(0f, 0f, 0f, 0.42f));

        var card = CreateRect(portalTutorialPanel.transform, "PortalTutorialCard", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 530f), new Color(0.06f, 0.07f, 0.10f, 0.98f));
        CreateRect(card.transform, "PortalAccent", new Vector2(0.5f, 1f),
            new Vector2(0f, -5f), new Vector2(900f, 10f), new Color(0.05f, 0.48f, 1f, 1f));

        CreateText(card.transform, "PortalTitle", "Portal Delivery", new Vector2(0.5f, 1f),
            new Vector2(0f, -48f), new Vector2(760f, 54f), 36, TextAlignmentOptions.Center);
        CreateText(card.transform, "PortalBody",
            "Push a box into a portal. It exits from the matching portal and keeps moving in the same direction.",
            new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 50f), 20, TextAlignmentOptions.Center);

        var diagram = CreateRect(card.transform, "PortalDiagram", new Vector2(0.5f, 0.54f),
            Vector2.zero, new Vector2(780f, 235f), new Color(0.025f, 0.03f, 0.045f, 0.95f));

        CreateText(diagram.transform, "DirectionLabel", "same push direction",
            new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(260f, 32f), 18, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "BoxTile", "BOX", "push", new Vector2(0.14f, 0.52f),
            new Color(0.82f, 0.50f, 0.14f));
        CreateText(diagram.transform, "ArrowToPortal", "=>", new Vector2(0.28f, 0.56f),
            Vector2.zero, new Vector2(70f, 42f), 34, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "PortalIn", "IN\nA", "enter", new Vector2(0.40f, 0.52f),
            new Color(0.00f, 0.45f, 1.00f));
        CreateText(diagram.transform, "TeleportArrow", "====>", new Vector2(0.52f, 0.56f),
            Vector2.zero, new Vector2(120f, 42f), 28, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "PortalOut", "OUT\nA", "exit", new Vector2(0.64f, 0.52f),
            new Color(0.00f, 0.45f, 1.00f));
        CreateText(diagram.transform, "ArrowToGoal", "=>", new Vector2(0.76f, 0.56f),
            Vector2.zero, new Vector2(70f, 42f), 34, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "GoalTile", "GOAL", "finish", new Vector2(0.88f, 0.52f),
            new Color(0.10f, 0.58f, 0.34f));

        CreateText(diagram.transform, "StepEnter", "1. Enter portal",
            new Vector2(0.30f, 0.13f), Vector2.zero, new Vector2(180f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepExit", "2. Exit the pair",
            new Vector2(0.55f, 0.13f), Vector2.zero, new Vector2(180f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepGoal", "3. Keep sliding to goal",
            new Vector2(0.80f, 0.13f), Vector2.zero, new Vector2(210f, 28f), 15, TextAlignmentOptions.Center);

        CreateText(card.transform, "PortalHint",
            "In this level, one goal is blocked by a wall. Use the portal lane to deliver that box.",
            new Vector2(0.5f, 0.23f), Vector2.zero, new Vector2(740f, 42f), 19, TextAlignmentOptions.Center);

        portalTutorialCloseButton = CreateButton(card.transform, "PortalTutorialStartButton", "Start Level",
            new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(240f, 58f));
        portalTutorialCloseButton.onClick.AddListener(() =>
        {
            portalTutorialPanel.SetActive(false);
            _portalTutorialClosed?.Invoke();
            _portalTutorialClosed = null;
        });
        portalTutorialPanel.SetActive(false);
    }

    private void EnsureIceTutorial()
    {
        if (iceTutorialPanel != null && iceTutorialCloseButton != null) return;

        var parent = GetComponentInParent<Canvas>()?.transform ?? transform;
        iceTutorialPanel = CreatePanel(parent, "IceTutorialPanel", new Color(0f, 0f, 0f, 0.88f));

        CreateRect(iceTutorialPanel.transform, "IceTutorialShadow", new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(920f, 550f), new Color(0f, 0f, 0f, 0.42f));

        var card = CreateRect(iceTutorialPanel.transform, "IceTutorialCard", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 530f), new Color(0.055f, 0.075f, 0.10f, 0.98f));
        CreateRect(card.transform, "IceAccent", new Vector2(0.5f, 1f),
            new Vector2(0f, -5f), new Vector2(900f, 10f), new Color(0.22f, 0.74f, 1f, 1f));

        CreateText(card.transform, "IceTitle", "Ice Runway", new Vector2(0.5f, 1f),
            new Vector2(0f, -48f), new Vector2(760f, 54f), 36, TextAlignmentOptions.Center);
        CreateText(card.transform, "IceBody",
            "A box pushed onto ice keeps sliding in that direction until the ice ends or something blocks it.",
            new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 50f), 20, TextAlignmentOptions.Center);

        var diagram = CreateRect(card.transform, "IceDiagram", new Vector2(0.5f, 0.54f),
            Vector2.zero, new Vector2(780f, 235f), new Color(0.025f, 0.035f, 0.055f, 0.95f));

        CreateText(diagram.transform, "DirectionLabel", "keeps sliding",
            new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(260f, 32f), 18, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "BoxTile", "BOX", "push", new Vector2(0.14f, 0.52f),
            new Color(0.82f, 0.50f, 0.14f));
        CreateText(diagram.transform, "ArrowToIce", "=>", new Vector2(0.28f, 0.56f),
            Vector2.zero, new Vector2(70f, 42f), 34, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "IceTileA", "ICE", "slide", new Vector2(0.41f, 0.52f),
            new Color(0.20f, 0.72f, 0.95f));
        CreateDemoTile(diagram.transform, "IceTileB", "ICE", "slide", new Vector2(0.55f, 0.52f),
            new Color(0.20f, 0.72f, 0.95f));
        CreateText(diagram.transform, "ArrowToGoal", "=>", new Vector2(0.69f, 0.56f),
            Vector2.zero, new Vector2(70f, 42f), 34, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "GoalTile", "GOAL", "stop", new Vector2(0.84f, 0.52f),
            new Color(0.10f, 0.58f, 0.34f));

        CreateText(diagram.transform, "StepEnter", "1. Push onto ice",
            new Vector2(0.29f, 0.13f), Vector2.zero, new Vector2(190f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepSlide", "2. Slide forward",
            new Vector2(0.53f, 0.13f), Vector2.zero, new Vector2(190f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepStop", "3. Stop after ice",
            new Vector2(0.78f, 0.13f), Vector2.zero, new Vector2(190f, 28f), 15, TextAlignmentOptions.Center);

        CreateText(card.transform, "IceHint",
            "In this level, the lower box must cross the ice strip to reach the right goal.",
            new Vector2(0.5f, 0.23f), Vector2.zero, new Vector2(740f, 42f), 19, TextAlignmentOptions.Center);

        iceTutorialCloseButton = CreateButton(card.transform, "IceTutorialStartButton", "Start Level",
            new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(240f, 58f));
        iceTutorialCloseButton.onClick.AddListener(() =>
        {
            iceTutorialPanel.SetActive(false);
            _iceTutorialClosed?.Invoke();
            _iceTutorialClosed = null;
        });
        iceTutorialPanel.SetActive(false);
    }

    private void EnsurePressureGateTutorial()
    {
        if (pressureGateTutorialPanel != null && pressureGateTutorialCloseButton != null) return;

        var parent = GetComponentInParent<Canvas>()?.transform ?? transform;
        pressureGateTutorialPanel = CreatePanel(parent, "PressureGateTutorialPanel", new Color(0f, 0f, 0f, 0.88f));

        CreateRect(pressureGateTutorialPanel.transform, "PressureGateTutorialShadow", new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(920f, 550f), new Color(0f, 0f, 0f, 0.42f));

        var card = CreateRect(pressureGateTutorialPanel.transform, "PressureGateTutorialCard", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 530f), new Color(0.07f, 0.06f, 0.08f, 0.98f));
        CreateRect(card.transform, "PressureGateAccent", new Vector2(0.5f, 1f),
            new Vector2(0f, -5f), new Vector2(900f, 10f), new Color(0.92f, 0.20f, 0.92f, 1f));

        CreateText(card.transform, "PressureGateTitle", "Pressure Gate", new Vector2(0.5f, 1f),
            new Vector2(0f, -48f), new Vector2(760f, 54f), 36, TextAlignmentOptions.Center);
        CreateText(card.transform, "PressureGateBody",
            "A pressure plate opens its linked door while a player or box is standing on it.",
            new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(760f, 50f), 20, TextAlignmentOptions.Center);

        var diagram = CreateRect(card.transform, "PressureGateDiagram", new Vector2(0.5f, 0.54f),
            Vector2.zero, new Vector2(780f, 235f), new Color(0.035f, 0.025f, 0.040f, 0.95f));

        CreateText(diagram.transform, "DirectionLabel", "keep the plate pressed",
            new Vector2(0.5f, 0.83f), Vector2.zero, new Vector2(280f, 32f), 18, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "HoldBoxTile", "BOX", "hold", new Vector2(0.10f, 0.52f),
            new Color(0.82f, 0.50f, 0.14f));
        CreateText(diagram.transform, "ArrowToPlate", "=>", new Vector2(0.20f, 0.56f),
            Vector2.zero, new Vector2(64f, 42f), 32, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "PlateTile", "PLATE", "press", new Vector2(0.30f, 0.52f),
            new Color(0.76f, 0.12f, 0.86f));
        CreateText(diagram.transform, "ArrowToDoor", "=>", new Vector2(0.40f, 0.56f),
            Vector2.zero, new Vector2(64f, 42f), 32, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "DoorTile", "DOOR\nOPEN", "opens", new Vector2(0.50f, 0.52f),
            new Color(0.16f, 0.58f, 0.32f));
        CreateText(diagram.transform, "ArrowThroughDoor", "=>", new Vector2(0.60f, 0.56f),
            Vector2.zero, new Vector2(64f, 42f), 32, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "PassBoxTile", "BOX", "pass", new Vector2(0.70f, 0.52f),
            new Color(0.82f, 0.50f, 0.14f));
        CreateText(diagram.transform, "ArrowToGoal", "=>", new Vector2(0.80f, 0.56f),
            Vector2.zero, new Vector2(64f, 42f), 32, TextAlignmentOptions.Center);
        CreateDemoTile(diagram.transform, "GoalTile", "GOAL", "finish", new Vector2(0.90f, 0.52f),
            new Color(0.10f, 0.58f, 0.34f));

        CreateText(diagram.transform, "StepPlate", "1. Hold plate",
            new Vector2(0.25f, 0.13f), Vector2.zero, new Vector2(180f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepDoor", "2. Door opens",
            new Vector2(0.50f, 0.13f), Vector2.zero, new Vector2(180f, 28f), 15, TextAlignmentOptions.Center);
        CreateText(diagram.transform, "StepPass", "3. Push through",
            new Vector2(0.75f, 0.13f), Vector2.zero, new Vector2(180f, 28f), 15, TextAlignmentOptions.Center);

        CreateText(card.transform, "PressureGateHint",
            "In this level, one box must hold the plate so the other box can pass through the door.",
            new Vector2(0.5f, 0.23f), Vector2.zero, new Vector2(740f, 42f), 19, TextAlignmentOptions.Center);

        pressureGateTutorialCloseButton = CreateButton(card.transform, "PressureGateTutorialStartButton", "Start Level",
            new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(240f, 58f));
        pressureGateTutorialCloseButton.onClick.AddListener(() =>
        {
            pressureGateTutorialPanel.SetActive(false);
            _pressureGateTutorialClosed?.Invoke();
            _pressureGateTutorialClosed = null;
        });
        pressureGateTutorialPanel.SetActive(false);
    }

    private static void CreateDemoTile(Transform parent, string name, string label, string caption, Vector2 anchor, Color color)
    {
        var shell = CreateRect(parent, name, anchor, Vector2.zero, new Vector2(112f, 108f),
            new Color(0.12f, 0.14f, 0.18f, 1f));
        CreateRect(shell.transform, "Face", new Vector2(0.5f, 0.62f),
            Vector2.zero, new Vector2(92f, 68f), color);
        CreateText(shell.transform, "Label", label, new Vector2(0.5f, 0.62f),
            Vector2.zero, new Vector2(92f, 68f), label.Contains("\n") ? 17 : 19, TextAlignmentOptions.Center);
        CreateText(shell.transform, "Caption", caption, new Vector2(0.5f, 0.11f),
            Vector2.zero, new Vector2(100f, 24f), 14, TextAlignmentOptions.Center);
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        go.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f);
        var button = go.AddComponent<Button>();
        CreateText(go.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 18, TextAlignmentOptions.Center);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchor,
        Vector2 offset, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}
