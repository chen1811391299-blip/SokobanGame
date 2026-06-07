using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public static class AllSceneBuilder
{
    [MenuItem("SokobanGame/4. Build All Scene Hierarchies")]
    public static void BuildAll()
    {
        CreateEditorTilePrefab();
        CreateLevelButtonPrefab();
        BuildMainMenu();
        BuildLevelSelect();
        BuildGameplay();
        BuildLevelEditor();
        Debug.Log("[AllSceneBuilder] Done — all 4 scenes built.");
    }

    // ── Prefab helpers ──────────────────────────────────────────────────────
    static void CreateEditorTilePrefab()
    {
        const string path = "Assets/Prefabs/Prefab_EditorTile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject("EditorTile");
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = Color.gray;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateLevelButtonPrefab()
    {
        const string path = "Assets/Prefabs/Prefab_LevelButton.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject("LevelButton");
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320, 55);
        go.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f);
        go.AddComponent<Button>();
        AddLabel(go.transform, "Level", 20);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ── MainMenu ────────────────────────────────────────────────────────────
    static void BuildMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        ClearScene();
        Cam2D();
        ES();

        var mgrs = new GameObject("Managers");
        var lm = mgrs.AddComponent<LevelManager>();
        lm.builtinLevels = Levels();
        mgrs.AddComponent<ModifierManager>();

        // Persistent singletons — created once in MainMenu, survive all scene loads
        new GameObject("SceneTransition").AddComponent<SceneTransition>();
        new GameObject("AudioManager").AddComponent<AudioManager>();

        var cvs  = Canvas();
        var mmUI = cvs.AddComponent<MainMenuUI>();
        TMP(cvs.transform, "Title", "SOKOBAN", .5f, .72f, 500, 80, 52);
        var p = Btn(cvs.transform, "BtnPlay",   "Play",         .5f, .56f, 220, 55);
        var e = Btn(cvs.transform, "BtnEditor", "Level Editor", .5f, .46f, 220, 55);
        var q = Btn(cvs.transform, "BtnQuit",   "Quit",         .5f, .36f, 220, 55);
        mmUI.btnPlay   = p.GetComponent<Button>();
        mmUI.btnEditor = e.GetComponent<Button>();
        mmUI.btnQuit   = q.GetComponent<Button>();

        Save(scene, "MainMenu");
    }

    // ── LevelSelect ─────────────────────────────────────────────────────────
    static void BuildLevelSelect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity", OpenSceneMode.Single);
        ClearScene();
        Cam2D();
        ES();

        var cvs  = Canvas();
        var lsUI = cvs.AddComponent<LevelSelectUI>();
        TMP(cvs.transform, "Title", "SELECT LEVEL", .5f, .86f, 400, 60, 36);

        // Level list (vertical layout)
        var listGO = new GameObject("LevelList");
        listGO.transform.SetParent(cvs.transform, false);
        var lRT = listGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(.2f, .3f);
        lRT.anchorMax = new Vector2(.8f, .80f);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10; vlg.childForceExpandWidth = true; vlg.childControlHeight = false;
        vlg.padding = new RectOffset(0, 0, 4, 4);

        // Modifier toggle
        var togRow = Panel(cvs.transform, "ToggleRow", .1f, .18f, .9f, .25f);
        togRow.GetComponent<Image>().color = new Color(0, 0, 0, .3f);
        var bgGO = new GameObject("BG"); bgGO.transform.SetParent(togRow.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0,.15f); bgRT.anchorMax = new Vector2(0,.85f);
        bgRT.anchoredPosition = new Vector2(20, 0); bgRT.sizeDelta = new Vector2(30, 30);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(.3f,.3f,.3f);
        var ckGO = new GameObject("Check"); ckGO.transform.SetParent(bgGO.transform, false);
        var ckRT = ckGO.AddComponent<RectTransform>();
        ckRT.anchorMin = new Vector2(.1f,.1f); ckRT.anchorMax = new Vector2(.9f,.9f);
        ckRT.offsetMin = ckRT.offsetMax = Vector2.zero;
        var ckImg = ckGO.AddComponent<Image>(); ckImg.color = Color.green;
        var labelGO = new GameObject("Label"); labelGO.transform.SetParent(togRow.transform, false);
        var lblRT = labelGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(.05f,0); lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(45,0); lblRT.offsetMax = Vector2.zero;
        var lblTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = "Enable Roguelike Modifier"; lblTMP.fontSize = 20;
        lblTMP.alignment = TextAlignmentOptions.Left; lblTMP.color = Color.white;
        var toggle = togRow.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg; toggle.graphic = ckImg; toggle.isOn = false;

        var modTxt = TMP(cvs.transform, "ModifierPreview", "", .5f, .10f, 720, 50, 16)
                        .GetComponent<TextMeshProUGUI>();
        var backBtn = BtnCorner(cvs.transform, "BtnBack", "<- Back", 0, 0, 120, 45, new Vector2(10, 10));

        lsUI.levelListParent    = listGO.transform;
        lsUI.levelButtonPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Prefab_LevelButton.prefab");
        lsUI.btnBack            = backBtn.GetComponent<Button>();
        lsUI.toggleModifier     = toggle;
        lsUI.modifierPreviewText = modTxt;

        Save(scene, "LevelSelect");
    }

    // ── Gameplay ────────────────────────────────────────────────────────────
    static void BuildGameplay()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
        ClearScene();
        Cam3D(new Vector3(3.5f, 12f, -1f), new Vector3(60, 0, 0));
        Light();
        ES();

        // GameManager/UndoManager on their own object — must NOT be destroyed
        var mgrs    = new GameObject("Managers");
        var gm      = mgrs.AddComponent<GameManager>();
        var undoMgr = mgrs.AddComponent<UndoManager>();

        // LevelManager/ModifierManager are DontDestroyOnLoad from MainMenu.
        // Keep a fallback here for direct-play from editor; they self-destruct if duplicates exist.
        var fallback = new GameObject("FallbackManagers");
        var lm       = fallback.AddComponent<LevelManager>(); lm.builtinLevels = Levels();
        fallback.AddComponent<ModifierManager>();

        var grGO = new GameObject("GridRenderer");
        var gr   = grGO.AddComponent<GridRenderer>();
        gr.wallPrefab          = P("Prefab_Wall");
        gr.floorPrefab         = P("Prefab_Floor");
        gr.goalPrefab          = P("Prefab_Goal");
        gr.icePrefab           = P("Prefab_Ice");
        gr.pressurePlatePrefab = P("Prefab_PressurePlate");
        gr.doorOpenPrefab      = P("Prefab_DoorOpen");
        gr.doorClosedPrefab    = P("Prefab_DoorClosed");
        gr.portalBluePrefab    = P("Prefab_PortalBlue");
        gr.portalOrangePrefab  = P("Prefab_PortalOrange");
        gr.portalGreenPrefab   = P("Prefab_PortalGreen");
        gr.playerPrefab        = P("Prefab_Player");
        gr.boxPrefab           = P("Prefab_Box");
        gr.boxOnGoalPrefab     = P("Prefab_BoxOnGoal");

        var pcGO = new GameObject("PlayerController");
        var pc   = pcGO.AddComponent<PlayerController>();

        var cvs = Canvas();
        var hud = cvs.AddComponent<GameHUD>();

        var lvlNameTMP  = TMP(cvs.transform, "LevelNameText", "Level", .5f, 1f, 320, 50, 22, new Vector2(0,-15))
                            .GetComponent<TextMeshProUGUI>();
        var moveTMP     = TMP(cvs.transform, "MoveCountText", "Moves: 0", 0f, 1f, 180, 40, 20, new Vector2(10,-60))
                            .GetComponent<TextMeshProUGUI>();
        moveTMP.alignment = TextAlignmentOptions.Left;
        var parTMP      = TMP(cvs.transform, "ParMovesText", "Par: 0", 0f, 1f, 180, 40, 18, new Vector2(10,-100))
                            .GetComponent<TextMeshProUGUI>();
        parTMP.alignment = TextAlignmentOptions.Left;
        var modTMP      = TMP(cvs.transform, "ModifierText", "", 0f, 1f, 360, 40, 16, new Vector2(10,-140))
                            .GetComponent<TextMeshProUGUI>();
        modTMP.alignment = TextAlignmentOptions.Left;
        modTMP.color = new Color(1f, 0.8f, 0.2f);

        var lcPanel = Panel(cvs.transform, "LevelCompletePanel", 0, 0, 1, 1);
        lcPanel.GetComponent<Image>().color = new Color(0, 0, 0, .7f);
        lcPanel.SetActive(false);
        TMP(lcPanel.transform, "CompleteText", "Level Complete!", .5f, .65f, 400, 80, 40);
        var starsTMP     = TMP(lcPanel.transform, "StarsText",    "***",         .5f, .55f, 300, 50, 36)
                             .GetComponent<TextMeshProUGUI>();
        starsTMP.color = new Color(1f, 0.85f, 0.1f);
        var bestTMP      = TMP(lcPanel.transform, "BestMovesText", "Moves: 0",   .5f, .47f, 360, 40, 22)
                             .GetComponent<TextMeshProUGUI>();
        var nextBtn      = Btn(lcPanel.transform, "NextLevelButton", "Next Level ->", .5f, .37f, 200, 55);

        var backEditorBtn = BtnCorner(cvs.transform, "BackToEditorButton", "<- Editor", 1, 1, 170, 45, new Vector2(-120,-32));
        backEditorBtn.SetActive(false);
        var pauseBtn     = BtnCorner(cvs.transform, "PauseButton",   "Pause",       1, 0, 170, 45, new Vector2(-120, 130));
        var restartBtn   = BtnCorner(cvs.transform, "RestartButton", "Restart (R)", 1, 0, 170, 45, new Vector2(-120, 80));
        var menuBtn      = BtnCorner(cvs.transform, "MenuButton",    "Menu",        1, 0, 170, 45, new Vector2(-120, 30));

        var pausePanel = Panel(cvs.transform, "PausePanel", 0, 0, 1, 1);
        pausePanel.GetComponent<Image>().color = new Color(0, 0, 0, .72f);
        pausePanel.SetActive(false);
        TMP(pausePanel.transform, "PauseTitle", "Paused", .5f, .64f, 320, 70, 38);
        var resumeBtn = Btn(pausePanel.transform, "PauseResumeButton", "Resume", .5f, .52f, 220, 55);
        var pauseRestartBtn = Btn(pausePanel.transform, "PauseRestartButton", "Restart", .5f, .43f, 220, 55);
        var pauseMenuBtn = Btn(pausePanel.transform, "PauseMenuButton", "Main Menu", .5f, .34f, 220, 55);

        var allCompletePanel = Panel(cvs.transform, "AllCompletePanel", 0, 0, 1, 1);
        allCompletePanel.GetComponent<Image>().color = new Color(0, 0, 0, .78f);
        allCompletePanel.SetActive(false);
        TMP(allCompletePanel.transform, "AllCompleteTitle", "All Levels Complete!", .5f, .64f, 520, 70, 38);
        var totalStarsTMP = TMP(allCompletePanel.transform, "TotalStarsText", "Stars: 0 / 0", .5f, .54f, 360, 45, 24)
                             .GetComponent<TextMeshProUGUI>();
        var replayBtn = Btn(allCompletePanel.transform, "ReplayAllButton", "Replay", .5f, .43f, 220, 55);
        var allMenuBtn = Btn(allCompletePanel.transform, "AllCompleteMenuButton", "Main Menu", .5f, .34f, 220, 55);

        gm.gridRenderer     = gr;
        gm.playerController = pc;
        gm.undoManager      = undoMgr;
        gm.hud              = hud;
        hud.levelNameText      = lvlNameTMP;
        hud.moveCountText      = moveTMP;
        hud.modifierText       = modTMP;
        hud.parMovesText       = parTMP;
        hud.starsText          = starsTMP;
        hud.bestMovesText      = bestTMP;
        hud.levelCompletePanel = lcPanel;
        hud.backToEditorButton = backEditorBtn;
        hud.pausePanel         = pausePanel;
        hud.allCompletePanel   = allCompletePanel;
        hud.pauseButton        = pauseBtn.GetComponent<Button>();
        hud.restartButton      = restartBtn.GetComponent<Button>();
        hud.menuButton         = menuBtn.GetComponent<Button>();
        hud.nextLevelButton    = nextBtn.GetComponent<Button>();
        hud.pauseResumeButton  = resumeBtn.GetComponent<Button>();
        hud.pauseRestartButton = pauseRestartBtn.GetComponent<Button>();
        hud.pauseMenuButton    = pauseMenuBtn.GetComponent<Button>();
        hud.totalStarsText     = totalStarsTMP;
        hud.allCompleteBtnReplay = replayBtn.GetComponent<Button>();
        hud.allCompleteBtnMenu   = allMenuBtn.GetComponent<Button>();

        Save(scene, "Gameplay");
    }

    // ── LevelEditor ──────────────────────────────────────────────────────────
    static void BuildLevelEditor()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LevelEditor.unity", OpenSceneMode.Single);
        ClearScene();
        Cam2D();
        ES();

        var cvs = Canvas();

        // Top toolbar
        var toolbar = new GameObject("Toolbar"); toolbar.transform.SetParent(cvs.transform, false);
        var tbRT = toolbar.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0,1); tbRT.anchorMax = new Vector2(1,1);
        tbRT.offsetMin = new Vector2(0,-55); tbRT.offsetMax = Vector2.zero;
        toolbar.AddComponent<Image>().color = new Color(.13f,.13f,.18f,1f);

        var btnBack     = BtnAbs(toolbar.transform,"BtnBack",     "Back",     new Vector2(5,-7.5f),   new Vector2(80,40));
        var btnNew      = BtnAbs(toolbar.transform,"BtnNew",      "New",      new Vector2(95,-7.5f),  new Vector2(80,40));
        var btnLoad     = BtnAbs(toolbar.transform,"BtnLoadSaved","Load",     new Vector2(185,-7.5f), new Vector2(80,40));
        var btnSave     = BtnAbs(toolbar.transform,"BtnSave",     "Save",     new Vector2(275,-7.5f), new Vector2(80,40));
        var btnTest     = BtnAbs(toolbar.transform,"BtnTestPlay", "Test Play",new Vector2(365,-7.5f), new Vector2(110,40));
        btnTest.GetComponent<Image>().color = new Color(.2f,.7f,.3f);
        var nameIn  = Input_(toolbar.transform,"LevelNameInput",new Vector2(490,-7.5f),new Vector2(180,40));
        var parIn   = Input_(toolbar.transform,"ParMovesInput", new Vector2(685,-7.5f),new Vector2(70,40));
        parIn.GetComponent<TMP_InputField>().text = "20";
        var wIn     = Input_(toolbar.transform,"WidthInput",    new Vector2(770,-7.5f),new Vector2(55,40));
        wIn.GetComponent<TMP_InputField>().text = "8";
        var hIn     = Input_(toolbar.transform,"HeightInput",   new Vector2(830,-7.5f),new Vector2(55,40));
        hIn.GetComponent<TMP_InputField>().text = "8";
        var btnResize = BtnAbs(toolbar.transform,"BtnResize","Resize",new Vector2(890,-7.5f),new Vector2(85,40));

        // Left brush panel
        var brushPanel = new GameObject("BrushPanel"); brushPanel.transform.SetParent(cvs.transform, false);
        var bpRT = brushPanel.AddComponent<RectTransform>();
        bpRT.anchorMin = new Vector2(0,0); bpRT.anchorMax = new Vector2(0,1);
        bpRT.offsetMin = Vector2.zero; bpRT.offsetMax = new Vector2(115,-55);
        brushPanel.AddComponent<Image>().color = new Color(.11f,.11f,.17f,1f);

        string[] blabels = {"Empty","Wall","Floor","Player","Box","Goal","Box+Goal","Ice","Plate","Door(O)","Door(C)","Portal"};
        Color[]  bcolors =
        {
            new Color(.15f,.15f,.15f), new Color(.35f,.35f,.35f), new Color(.55f,.50f,.40f),
            new Color(.10f,.50f,.90f), new Color(.70f,.40f,.10f), new Color(.95f,.85f,.10f),
            new Color(.15f,.72f,.20f), new Color(.60f,.88f,1.0f), new Color(.80f,.25f,.90f),
            new Color(.12f,.80f,.25f), new Color(.80f,.12f,.12f), new Color(.00f,.50f,1.0f),
        };
        var brushBtns = new Button[blabels.Length];
        for (int i = 0; i < blabels.Length; i++)
        {
            var b = BtnAbs(brushPanel.transform, $"Brush_{i}", blabels[i],
                           new Vector2(5, -5 - i*42f), new Vector2(105,38));
            b.GetComponent<Image>().color = bcolors[i];
            brushBtns[i] = b.GetComponent<Button>();
        }

        // Right validation panel
        var valPanel = new GameObject("ValidationPanel"); valPanel.transform.SetParent(cvs.transform, false);
        var vpRT = valPanel.AddComponent<RectTransform>();
        vpRT.anchorMin = new Vector2(1,0); vpRT.anchorMax = new Vector2(1,1);
        vpRT.offsetMin = new Vector2(-145,0); vpRT.offsetMax = new Vector2(0,-55);
        valPanel.AddComponent<Image>().color = new Color(.11f,.11f,.17f,1f);

        var pcTMP  = ValTMP(valPanel.transform,"PlayerCount", "Player: 0", 0);
        var bcTMP  = ValTMP(valPanel.transform,"BoxCount",    "Boxes: 0",  1);
        var gcTMP  = ValTMP(valPanel.transform,"GoalCount",   "Goals: 0",  2);
        var prTMP  = ValTMP(valPanel.transform,"PortalStatus","o Portals",  3);
        var drTMP  = ValTMP(valPanel.transform,"DoorStatus",  "o Plates",   4);
        var errTMP = ValTMP(valPanel.transform,"ErrorText",   "",           5, Color.red, 14);

        // Center grid area — all 3 editor components on same GO (so GetComponent works)
        var gridArea = new GameObject("GridArea"); gridArea.transform.SetParent(cvs.transform, false);
        var gaRT = gridArea.AddComponent<RectTransform>();
        gaRT.anchorMin = new Vector2(0,0); gaRT.anchorMax = new Vector2(1,1);
        gaRT.offsetMin = new Vector2(115,0); gaRT.offsetMax = new Vector2(-145,-55);
        gridArea.AddComponent<Image>().color = new Color(.08f,.08f,.12f,1f);

        var edView = gridArea.AddComponent<EditorGridView>();
        edView.editorTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Prefab_EditorTile.prefab");
        edView.tileSize = 60f;
        gridArea.AddComponent<LevelEditorManager>();
        var edUI = gridArea.AddComponent<LevelEditorUI>();

        edUI.btnBack       = btnBack.GetComponent<Button>();
        edUI.btnNew        = btnNew.GetComponent<Button>();
        edUI.btnLoadSaved  = btnLoad.GetComponent<Button>();
        edUI.btnSave       = btnSave.GetComponent<Button>();
        edUI.btnTestPlay   = btnTest.GetComponent<Button>();
        edUI.levelNameInput = nameIn.GetComponent<TMP_InputField>();
        edUI.parMovesInput = parIn.GetComponent<TMP_InputField>();
        edUI.widthInput    = wIn.GetComponent<TMP_InputField>();
        edUI.heightInput   = hIn.GetComponent<TMP_InputField>();
        edUI.btnResize     = btnResize.GetComponent<Button>();
        edUI.brushButtons  = brushBtns;
        edUI.playerCountText  = pcTMP;
        edUI.boxCountText     = bcTMP;
        edUI.goalCountText    = gcTMP;
        edUI.portalStatusText = prTMP;
        edUI.doorStatusText   = drTMP;
        edUI.errorText        = errTMP;

        Save(scene, "LevelEditor");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    static void ClearScene()
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }

    static void Cam2D()
    {
        var go = new GameObject("Main Camera"); go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.12f,.12f,.18f);
        go.transform.position = new Vector3(0, 0, -10);
        go.AddComponent<AudioListener>();
    }

    static void Cam3D(Vector3 pos, Vector3 euler)
    {
        var go = new GameObject("Main Camera"); go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.08f,.10f,.15f);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;
        go.AddComponent<AudioListener>();
    }

    static void Light()
    {
        var go = new GameObject("Directional Light");
        go.transform.eulerAngles = new Vector3(50, -30, 0);
        var l = go.AddComponent<Light>(); l.type = LightType.Directional;
    }

    static void ES()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static GameObject Canvas()
    {
        var go = new GameObject("Canvas");
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // Panel: anchor-based fill
    static GameObject Panel(Transform p, string name, float axMin, float ayMin, float axMax, float ayMax)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(axMin,ayMin); rt.anchorMax = new Vector2(axMax,ayMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0,0,0,.6f);
        return go;
    }

    // TMP: centered pivot
    static GameObject TMP(Transform p, string name, string text,
        float ax, float ay, float w, float h, int fs = 24, Vector2? apos = null)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = apos ?? Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    // Btn: centered pivot
    static GameObject Btn(Transform p, string name, string label, float ax, float ay, float w, float h)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = new Color(.25f,.45f,.85f);
        go.AddComponent<Button>();
        AddLabel(go.transform, label, 18);
        return go;
    }

    // Btn: corner-anchored with explicit offset
    static GameObject BtnCorner(Transform p, string name, string label,
        float ax, float ay, float w, float h, Vector2 offset)
    {
        var go = Btn(p, name, label, ax, ay, w, h);
        go.GetComponent<RectTransform>().anchoredPosition = offset;
        return go;
    }

    // Btn: absolute position (top-left origin)
    static GameObject BtnAbs(Transform p, string name, string label, Vector2 apos, Vector2 size)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = size; rt.anchoredPosition = apos;
        go.AddComponent<Image>().color = new Color(.25f,.45f,.85f);
        go.AddComponent<Button>();
        AddLabel(go.transform, label, 17);
        return go;
    }

    static GameObject Input_(Transform p, string name, Vector2 apos, Vector2 size)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = size; rt.anchoredPosition = apos;
        go.AddComponent<Image>().color = new Color(.15f,.15f,.2f);
        var ta = new GameObject("TextArea"); ta.transform.SetParent(go.transform, false);
        var taRT = ta.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(4,2); taRT.offsetMax = new Vector2(-4,-2);
        ta.AddComponent<RectMask2D>();
        var tg = new GameObject("Text"); tg.transform.SetParent(ta.transform, false);
        var tgRT = tg.AddComponent<RectTransform>();
        tgRT.anchorMin = Vector2.zero; tgRT.anchorMax = Vector2.one;
        tgRT.offsetMin = tgRT.offsetMax = Vector2.zero;
        var tmp = tg.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 16; tmp.color = Color.white;
        var inp = go.AddComponent<TMP_InputField>();
        inp.textComponent = tmp; inp.textViewport = taRT;
        return go;
    }

    static TextMeshProUGUI ValTMP(Transform p, string name, string text,
        int row, Color? col = null, int fs = 16)
    {
        var go = new GameObject(name); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
        rt.sizeDelta = new Vector2(0,30); rt.anchoredPosition = new Vector2(0,-8 - row*35f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = col ?? Color.white;
        tmp.margin = new Vector4(6,0,0,0);
        return tmp;
    }

    static void AddLabel(Transform p, string text, int fs)
    {
        var go = new GameObject("Label"); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    static LevelData[] Levels()
    {
        var ids = new[]{ "Level_01","Level_02","Level_03","Level_04",
                         "Level_05","Level_06","Level_07","Level_08" };
        var list = new System.Collections.Generic.List<LevelData>();
        foreach (var id in ids)
        {
            var ld = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Resources/Levels/{id}.asset");
            if (ld != null) list.Add(ld);
        }
        return list.ToArray();
    }

    static GameObject P(string n) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{n}.prefab");

    static void Save(Scene scene, string name)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[AllSceneBuilder] {name} built.");
    }
}
