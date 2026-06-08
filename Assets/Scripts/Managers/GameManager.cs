using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    public GridRenderer     gridRenderer;
    public PlayerController playerController;
    public UndoManager      undoManager;
    public GameHUD          hud;

    private SokobanGrid _grid = new();
    private LevelData   _currentLevel;
    private bool        _isPaused = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void Start()
    {
        LevelData level;
        if (LevelManager.IsTestMode && LevelManager.TestLevel != null)
        {
            level = LevelManager.TestLevel;
            hud?.ShowBackToEditorButton(true);
        }
        else
        {
            level = LevelManager.Instance?.CurrentLevel;
            hud?.ShowBackToEditorButton(false);
        }

        if (level == null && LevelManager.Instance != null && LevelManager.Instance.LevelCount > 0)
        {
            LevelManager.Instance.SetCurrent(0);
            level = LevelManager.Instance.CurrentLevel;
        }

        if (level != null) LoadLevel(level);
        else Debug.LogError("[GameManager] No level data available.");
    }

    public void LoadLevel(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[GameManager] Cannot load a null level.");
            return;
        }

        Unsubscribe();
        Time.timeScale = 1f;
        _isPaused = false;

        _currentLevel = data;
        _grid.LoadLevel(data);
        ModifierManager.Instance?.ApplyToGrid(_grid);

        undoManager.Clear();
        playerController.Init(_grid, undoManager);
        playerController.ResetMoves();
        playerController.InputEnabled = true;

        playerController.OnStateChanged     += OnStateChanged;
        playerController.OnLevelComplete    += OnLevelComplete;
        if (hud != null)
            playerController.OnMoveCountChanged += hud.UpdateMoveCount;

        gridRenderer.Init(_grid);
        FrameCameraToLevel();
        ModifierManager.Instance?.ApplyToRenderer(gridRenderer, _grid);

        string modName = ModifierManager.Instance?.GetDisplayName() ?? "";
        hud?.SetModifierText(modName);
        hud?.SetLevelName(data.levelName);
        hud?.SetParMoves(data.parMoves);
        hud?.HideLevelComplete();
        hud?.UpdateMoveCount(0);
        AudioManager.PlayBGM(AudioManager.Instance?.bgmGameplay);

        if (data.levelId == "Level_04" && hud != null)
        {
            playerController.InputEnabled = false;
            hud.ShowPortalTutorial(() =>
            {
                if (playerController != null)
                    playerController.InputEnabled = true;
            });
        }
        else if (data.levelId == "Level_05" && hud != null)
        {
            playerController.InputEnabled = false;
            hud.ShowIceTutorial(() =>
            {
                if (playerController != null)
                    playerController.InputEnabled = true;
            });
        }
        else if (data.levelId == "Level_06" && hud != null)
        {
            playerController.InputEnabled = false;
            hud.ShowPressureGateTutorial(() =>
            {
                if (playerController != null)
                    playerController.InputEnabled = true;
            });
        }
    }

    void OnStateChanged()
    {
        gridRenderer.Rebuild();
        if (ModifierManager.Instance?.ActiveModifier == ModifierType.FogOfWar)
            gridRenderer.ApplyFogOfWar(_grid.PlayerPos, 3);
        else
            gridRenderer.ClearFog();
    }

    void OnLevelComplete()
    {
        playerController.InputEnabled = false;
        int moves = playerController.MoveCount;

        if (_currentLevel != null)
        {
            SaveManager.RecordCompletion(_currentLevel.levelId, moves);
            int best  = SaveManager.GetBestMoves(_currentLevel.levelId);
            int stars = SaveManager.GetStars(_currentLevel.levelId, _currentLevel.parMoves);
            hud?.ShowLevelComplete(moves, stars, best);
        }
        else
        {
            hud?.ShowLevelComplete(moves, 1, int.MaxValue);
        }

        AudioManager.PlayComplete();
    }

    public void TogglePause()
    {
        _isPaused          = !_isPaused;
        Time.timeScale     = _isPaused ? 0f : 1f;
        hud?.ShowPause(_isPaused);
        if (playerController != null)
            playerController.InputEnabled = !_isPaused;
    }

    public void RestartLevel()
    {
        if (_isPaused) TogglePause();
        Unsubscribe();
        LevelData level = LevelManager.IsTestMode
            ? LevelManager.TestLevel
            : LevelManager.Instance?.CurrentLevel;
        if (level != null) LoadLevel(level);
    }

    public void NextLevel()
    {
        if (LevelManager.IsTestMode) { BackToEditor(); return; }
        Unsubscribe();
        var lm = LevelManager.Instance;

        if (lm.CurrentIndex >= lm.LevelCount - 1)
        {
            int total = 0, max = lm.LevelCount * 3;
            for (int i = 0; i < lm.LevelCount; i++)
            {
                var d = lm.GetLevel(i);
                total += SaveManager.GetStars(d.levelId, d.parMoves);
            }
            playerController.InputEnabled = false;
            hud?.HideLevelComplete();
            hud?.ShowAllComplete(total, max);
            return;
        }

        lm.SetCurrent(lm.CurrentIndex + 1);
        LoadLevel(lm.CurrentLevel);
    }

    public void ReplayAll()
    {
        var lm = LevelManager.Instance;
        lm.SetCurrent(0);
        hud?.HideAllComplete();
        LoadLevel(lm.CurrentLevel);
    }

    public void BackToEditor()
    {
        // IsTestMode intentionally NOT cleared here — LevelEditorManager.Start() uses it to restore the level
        Unsubscribe();
        SceneTransition.LoadScene("LevelEditor");
    }

    public void SaveTestLevel()
    {
        if (!LevelManager.IsTestMode || LevelManager.TestLevel == null) return;
        LevelSerializer.Save(LevelManager.TestLevel);
        LevelManager.Instance?.Refresh();
        hud?.SetLevelName("Saved!");
    }

    public void MovePlayer(Vector2Int dir) => playerController?.MoveInDirection(dir);

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        _isPaused      = false;
        LevelManager.IsTestMode = false;
        Unsubscribe();
        SceneTransition.LoadScene("MainMenu");
    }

    private void FrameCameraToLevel()
    {
        if (gridRenderer == null || Camera.main == null) return;

        var center = new Vector3(
            (_grid.Width - 1) * gridRenderer.tileSize * 0.5f,
            0f,
            (_grid.Height - 1) * gridRenderer.tileSize * 0.5f);

        float span = Mathf.Max(_grid.Width, _grid.Height) * gridRenderer.tileSize;
        float height = Mathf.Clamp(span * 1.35f, 8f, 18f);
        float back = Mathf.Clamp(span * 0.85f, 4f, 12f);

        var cam = Camera.main;
        cam.fieldOfView = 50f;
        cam.transform.position = center + new Vector3(0f, height, -back);
        cam.transform.LookAt(center);
    }

    private void Unsubscribe()
    {
        if (playerController == null) return;
        playerController.OnStateChanged  -= OnStateChanged;
        playerController.OnLevelComplete -= OnLevelComplete;
        if (hud != null)
            playerController.OnMoveCountChanged -= hud.UpdateMoveCount;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !_isPaused) RestartLevel();
        if (Input.GetKeyDown(KeyCode.Escape))          TogglePause();
    }

    void OnDestroy() => Unsubscribe();
}
