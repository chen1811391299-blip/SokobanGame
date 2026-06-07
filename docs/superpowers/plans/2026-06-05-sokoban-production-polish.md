# 推箱子产品级完善 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将推箱子原型提升至产品级：修复关键 Bug（parMoves 序列化、编辑器缺失按钮），完善游戏流程（ESC 暂停菜单、关卡顺序解锁、全通关结局），打磨 UI（★ 星级、HUD 目标步数提示），并为编辑器增加撤销（Ctrl+Z）和 AudioManager 增加 BGM 支持。

**Architecture:** 所有修改在现有类上原地扩展，无新场景、无架构重组。P0 → P1 → P2 顺序推进，每个 Task 独立可测试。纯数据层（LevelDataJson）用 Edit Mode 单元测试覆盖；Unity MonoBehaviour 行为用 Play Mode 手动验证。

**Tech Stack:** Unity 2D/3D，C#，TextMeshPro，Unity Test Framework（Edit Mode），PlayerPrefs，ScriptableObject

---

## 文件修改一览

| 文件 | 改动摘要 |
|------|---------|
| `Assets/Scripts/Data/LevelDataJson.cs` | 增加 `parMoves` 字段及序列化读写 |
| `Assets/Scripts/LevelEditor/LevelEditorUI.cs` | 增加 `btnBack` + `parMovesInput` |
| `Assets/Scripts/LevelEditor/LevelEditorManager.cs` | 编辑器 Undo 栈 + BGM 调用 |
| `Assets/Scripts/Managers/GameManager.cs` | 暂停逻辑、NextLevel 全通关检测、传递 parMoves、BGM 调用 |
| `Assets/Scripts/UI/GameHUD.cs` | parMoves 显示、★ 星级、pausePanel、allCompletePanel |
| `Assets/Scripts/UI/LevelSelectUI.cs` | 关卡顺序解锁、★ 星级 |
| `Assets/Scripts/UI/MainMenuUI.cs` | BGM 调用 |
| `Assets/Scripts/Managers/AudioManager.cs` | BGM 循环播放支持 |
| `Assets/Tests/EditMode/SokobanGridTests.cs` | 追加 LevelDataJson.parMoves 序列化测试 |

---

### Task 1：修复 parMoves 序列化 Bug（P0）

**Files:**
- Modify: `Assets/Scripts/Data/LevelDataJson.cs`
- Modify: `Assets/Tests/EditMode/SokobanGridTests.cs`

- [ ] **Step 1：写失败测试**

在 `SokobanGridTests.cs` 最后一个 `}` 之前追加：

```csharp
[Test]
public void LevelDataJson_PreservesParMoves()
{
    var src        = ScriptableObject.CreateInstance<LevelData>();
    src.levelId    = "test-par";
    src.levelName  = "Par Test";
    src.width      = 3;
    src.height     = 3;
    src.tiles      = new int[9];
    src.parMoves   = 7;

    var json = LevelDataJson.From(src);
    Assert.AreEqual(7, json.parMoves, "From() must copy parMoves");

    var restored = json.ToLevelData();
    Assert.AreEqual(7, restored.parMoves, "ToLevelData() must restore parMoves");
}
```

- [ ] **Step 2：运行测试确认失败**

Unity Editor → Window → General → Test Runner → EditMode → 运行 `LevelDataJson_PreservesParMoves`  
期望：**FAIL**（`json.parMoves` 为 0，字段不存在）

- [ ] **Step 3：修复 LevelDataJson.cs**

将 `Assets/Scripts/Data/LevelDataJson.cs` 替换为：

```csharp
using System;
using UnityEngine;

[Serializable]
public class LevelDataJson
{
    public string id;
    public string name;
    public string author;
    public int    width;
    public int    height;
    public int    parMoves;            // ← 新增
    public int[]  tiles;
    public PortalPair[] portalPairs;
    public DoorLink[]   doorLinks;

    public LevelData ToLevelData()
    {
        var data         = ScriptableObject.CreateInstance<LevelData>();
        data.levelId     = id;
        data.levelName   = name    ?? "未命名";
        data.author      = author  ?? "unknown";
        data.width       = width;
        data.height      = height;
        data.parMoves    = parMoves > 0 ? parMoves : 20;  // ← 新增，旧 JSON 缺失时保底 20
        data.tiles       = tiles;
        data.portalPairs = portalPairs ?? Array.Empty<PortalPair>();
        data.doorLinks   = doorLinks   ?? Array.Empty<DoorLink>();
        return data;
    }

    public static LevelDataJson From(LevelData d) => new LevelDataJson
    {
        id          = d.levelId,
        name        = d.levelName,
        author      = d.author,
        width       = d.width,
        height      = d.height,
        parMoves    = d.parMoves,      // ← 新增
        tiles       = d.tiles,
        portalPairs = d.portalPairs,
        doorLinks   = d.doorLinks
    };
}
```

- [ ] **Step 4：运行测试确认通过**

Unity Test Runner → 运行 `LevelDataJson_PreservesParMoves`  
期望：**PASS**

- [ ] **Step 5：提交**

```
git add Assets/Scripts/Data/LevelDataJson.cs Assets/Tests/EditMode/SokobanGridTests.cs
git commit -m "fix: LevelDataJson 序列化增加 parMoves 字段"
```

---

### Task 2：编辑器增加返回主菜单按钮和目标步数输入框（P0）

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorUI.cs`

- [ ] **Step 1：修改 LevelEditorUI.cs**

将文件完整替换为：

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditorUI : MonoBehaviour
{
    [Header("工具栏")]
    public Button         btnBack;          // 返回主菜单
    public Button         btnNew, btnSave, btnTestPlay;
    public TMP_InputField levelNameInput;
    public TMP_InputField parMovesInput;    // 目标步数

    [Header("画笔按钮（按TileType枚举顺序排列）")]
    public Button[] brushButtons;

    [Header("验证显示")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI boxCountText;
    public TextMeshProUGUI goalCountText;
    public TextMeshProUGUI portalStatusText;
    public TextMeshProUGUI doorStatusText;
    public TextMeshProUGUI errorText;

    [Header("地图尺寸")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button         btnResize;

    private LevelEditorManager _editor;

    void Start()
    {
        _editor = LevelEditorManager.Instance;

        btnBack?.onClick.AddListener(() =>
        {
            AudioManager.PlayMenu();
            SceneTransition.LoadScene("MainMenu");
        });

        btnNew?.onClick.AddListener(() => _editor?.NewLevel(8, 8));

        btnSave?.onClick.AddListener(() =>
        {
            if (_editor?.editingLevel == null) return;
            if (levelNameInput != null)
                _editor.editingLevel.levelName = levelNameInput.text;
            if (parMovesInput != null && int.TryParse(parMovesInput.text, out int par))
                _editor.editingLevel.parMoves = Mathf.Max(1, par);
            _editor.Save();
        });

        btnTestPlay?.onClick.AddListener(() => _editor?.TestPlay());

        btnResize?.onClick.AddListener(() =>
        {
            if (int.TryParse(widthInput?.text,  out int w) &&
                int.TryParse(heightInput?.text, out int h))
                _editor?.NewLevel(Mathf.Clamp(w, 3, 20), Mathf.Clamp(h, 3, 20));
        });

        for (int i = 0; i < brushButtons.Length; i++)
        {
            int idx = i;
            brushButtons[i]?.onClick.AddListener(() =>
            {
                if (_editor != null) _editor.SelectedBrush = (TileType)idx;
            });
        }

        if (parMovesInput != null && _editor?.editingLevel != null)
            parMovesInput.text = _editor.editingLevel.parMoves.ToString();

        InvokeRepeating(nameof(RefreshValidation), 0f, 0.3f);
    }

    void RefreshValidation()
    {
        if (_editor?.editingLevel == null) return;
        var v = _editor.GetValidation();
        if (playerCountText)  playerCountText.text  = $"玩家: {v.PlayerCount}";
        if (boxCountText)     boxCountText.text      = $"箱子: {v.BoxCount}";
        if (goalCountText)    goalCountText.text     = $"目标: {v.GoalCount}";
        if (portalStatusText) portalStatusText.text  = v.PortalsPaired ? "✓ 传送门已配对" : "✗ 传送门未配对";
        if (doorStatusText)   doorStatusText.text    = v.DoorsLinked   ? "✓ 压力板已关联" : "✗ 压力板未关联";
        if (errorText)        errorText.text         = v.IsValid ? "" : v.ErrorMessage;
    }

    public void ShowValidationError(string msg)
    {
        if (errorText) errorText.text = msg;
    }
}
```

- [ ] **Step 2：Unity Inspector 配置**

1. 打开 **LevelEditor** 场景
2. 找到挂有 `LevelEditorUI` 组件的 GameObject
3. 在编辑器 Canvas 工具栏区域创建：
   - 一个 **Button**，Label 文字设为 `← 主菜单`，将其拖入 `LevelEditorUI.btnBack` Slot
   - 一个 **TMP_InputField**，Content Type = `Integer Number`，默认文字 `20`，将其拖入 `LevelEditorUI.parMovesInput` Slot
4. 保存场景（Ctrl+S）

- [ ] **Step 3：手动测试**

Play Mode → LevelEditor 场景：
- 点击 `← 主菜单` → 应跳转回 MainMenu 场景 ✓
- 将目标步数改为 `8` → 点保存 → 用文本编辑器打开 `StreamingAssets/Levels/*.json`，确认有 `"parMoves":8` ✓

- [ ] **Step 4：提交**

```
git add Assets/Scripts/LevelEditor/LevelEditorUI.cs
git commit -m "feat: 编辑器增加返回主菜单按钮和目标步数输入框"
```

---

### Task 3：暂停菜单（ESC 键）（P1）

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/UI/GameHUD.cs`

- [ ] **Step 1：修改 GameHUD.cs — 增加 pausePanel 引用和控制方法**

在 `GameHUD.cs` 中做以下修改：

**① 增加字段**（放在 `[Header("Panels")]` 下）：

```csharp
[Header("Panels")]
public GameObject levelCompletePanel;
public GameObject backToEditorButton;
public GameObject pausePanel;           // 新增

[Header("Pause Buttons")]
public Button pauseResumeButton;
public Button pauseRestartButton;
public Button pauseMenuButton;
```

**② 在 `Start()` 末尾追加**：

```csharp
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
```

**③ 增加公开方法**（放在 `HideLevelComplete()` 之后）：

```csharp
public void ShowPause(bool show) => pausePanel?.SetActive(show);
```

- [ ] **Step 2：修改 GameManager.cs — 增加暂停逻辑**

**① 增加字段**（放在 `private SokobanGrid _grid` 之后）：

```csharp
private bool _isPaused = false;
```

**② 增加 `TogglePause()` 方法**（放在 `RestartLevel()` 之前）：

```csharp
public void TogglePause()
{
    _isPaused          = !_isPaused;
    Time.timeScale     = _isPaused ? 0f : 1f;
    hud?.ShowPause(_isPaused);
    if (playerController != null)
        playerController.InputEnabled = !_isPaused;
}
```

**③ 替换 `Update()` 方法**：

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.R) && !_isPaused) RestartLevel();
    if (Input.GetKeyDown(KeyCode.Escape))          TogglePause();
}
```

**④ 在 `RestartLevel()` 开头插入**（紧跟方法体第一行前）：

```csharp
if (_isPaused) TogglePause();
```

**⑤ 替换 `GoToMainMenu()` 方法**：

```csharp
public void GoToMainMenu()
{
    Time.timeScale = 1f;
    _isPaused      = false;
    Unsubscribe();
    SceneTransition.LoadScene("MainMenu");
}
```

- [ ] **Step 3：Unity Inspector 配置**

1. 打开 **Gameplay** 场景
2. 在 HUD Canvas 下新建 Panel `PausePanel`（默认 SetActive = false）：
   - 半透明黑色背景 `Image`（Color alpha ≈ 0.7）
   - 三个子 Button：`BtnResume`（继续游戏）、`BtnRestart`（重新开始）、`BtnMenu`（主菜单）
3. 将 `PausePanel` GameObject 拖入 `GameHUD.pausePanel`
4. 将三个按钮拖入 `GameHUD.pauseResumeButton / pauseRestartButton / pauseMenuButton`
5. 保存场景（Ctrl+S）

- [ ] **Step 4：手动测试**

Play Mode → Gameplay：
- 移动几步后按 **ESC** → 暂停面板出现，角色无法移动 ✓
- 点 **继续游戏** → 面板关闭，游戏恢复 ✓
- 暂停状态点 **重新开始** → 关卡重置，面板关闭 ✓
- 暂停状态点 **主菜单** → 返回主菜单，`Time.timeScale` 正常（场景可正常运行）✓
- 按 **R** 键重开时不会在暂停状态下触发（暂停时 R 无效）✓

- [ ] **Step 5：提交**

```
git add Assets/Scripts/Managers/GameManager.cs Assets/Scripts/UI/GameHUD.cs
git commit -m "feat: 添加 ESC 暂停菜单，含继续/重开/主菜单三按钮"
```

---

### Task 4：关卡选择界面——顺序解锁 + ★ 星级显示（P1）

**Files:**
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`

- [ ] **Step 1：替换 `PopulateLevels()` 方法**

将 `LevelSelectUI.cs` 中的 `PopulateLevels()` 方法完整替换为：

```csharp
void PopulateLevels()
{
    var lm = LevelManager.Instance;
    if (!lm) return;

    for (int i = 0; i < lm.LevelCount; i++)
    {
        int idx  = i;
        var data = lm.GetLevel(i);
        var go   = Instantiate(levelButtonPrefab, levelListParent);

        bool unlocked = (i == 0) || SaveManager.IsCompleted(lm.GetLevel(i - 1).levelId);
        int  stars    = SaveManager.GetStars(data.levelId, data.parMoves);
        string starStr = stars switch { 3 => " ★★★", 2 => " ★★☆", 1 => " ★☆☆", _ => "" };

        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        var btn   = go.GetComponent<Button>();

        if (!unlocked)
        {
            if (label) label.text = $"🔒 {i + 1}. {data.levelName}";
            if (btn)
            {
                btn.interactable = false;
                var colors = btn.colors;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                btn.colors = colors;
            }
        }
        else
        {
            if (label) label.text = $"{i + 1}. {data.levelName}{starStr}";
            btn?.onClick.AddListener(() =>
            {
                AudioManager.PlayMenu();
                lm.SetCurrent(idx);
                LevelManager.IsTestMode = false;
                SceneTransition.LoadScene("Gameplay");
            });
        }
    }
}
```

- [ ] **Step 2：手动测试**

Play Mode → LevelSelect 场景（确保存档干净，用 `SaveManager.ResetAll()` 或 Unity Edit → Clear All PlayerPrefs）：
- 全部未通关：第 1 关可点击，其余显示 🔒 不可点 ✓
- 通关第 1 关 → 返回关卡选择 → 第 2 关解锁 ✓
- 第 1 关已通关并达到 3 星 → 显示 `★★★` ✓

- [ ] **Step 3：提交**

```
git add Assets/Scripts/UI/LevelSelectUI.cs
git commit -m "feat: 关卡选择增加顺序解锁逻辑和★星级显示"
```

---

### Task 5：GameHUD 改进——parMoves 实时提示 + ★ 星级修复（P1）

**Files:**
- Modify: `Assets/Scripts/UI/GameHUD.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs`

- [ ] **Step 1：修改 GameHUD.cs**

**① 增加字段**（放在 `[Header("Text")]` 下）：

```csharp
public TextMeshProUGUI parMovesText;   // 显示 "目标 ≤ N 步"
```

**② 增加私有字段**（放在类成员变量区域）：

```csharp
private int _parMoves = 0;
```

**③ 增加 `SetParMoves()` 方法**（放在 `SetModifierText()` 之后）：

```csharp
public void SetParMoves(int par)
{
    _parMoves = par;
    if (parMovesText)
        parMovesText.text = par > 0 ? $"目标 ≤ {par} 步" : "";
}
```

**④ 替换 `UpdateMoveCount()` 方法**：

```csharp
public void UpdateMoveCount(int count)
{
    if (moveCountText) moveCountText.text = $"步数：{count}";
    if (parMovesText && _parMoves > 0)
        parMovesText.color = count > _parMoves
            ? Color.red
            : new Color(0.6f, 1f, 0.6f);
}
```

**⑤ 替换 `ShowLevelComplete()` 中星级显示**（仅改 starsText 那行）：

```csharp
if (starsText)
    starsText.text = stars switch { 3 => "★★★", 2 => "★★☆", _ => "★☆☆" };
if (bestMovesText)
{
    bestMovesText.text = best == int.MaxValue
        ? $"步数：{moves}"
        : $"步数：{moves}   最佳：{best}";
}
```

- [ ] **Step 2：修改 GameManager.LoadLevel() 传递 parMoves**

在 `LoadLevel()` 中 `hud?.SetLevelName(data.levelName)` 一行之后追加：

```csharp
hud?.SetParMoves(data.parMoves);
```

- [ ] **Step 3：Unity Inspector 配置**

1. 打开 **Gameplay** 场景
2. 在 HUD Canvas 中创建 TextMeshPro 文字节点 `ParMovesText`（位置：HUD 右上角或 moveCountText 旁边）
3. 将其拖入 `GameHUD.parMovesText` Slot
4. 保存场景（Ctrl+S）

- [ ] **Step 4：手动测试**

Play Mode → Gameplay：
- HUD 显示 `目标 ≤ N 步`，颜色为浅绿 ✓
- 步数超出 parMoves 后 parMovesText 变红 ✓
- 通关面板显示 `★★★` / `★★☆` / `★☆☆`（不再是 `***`）✓

- [ ] **Step 5：提交**

```
git add Assets/Scripts/UI/GameHUD.cs Assets/Scripts/Managers/GameManager.cs
git commit -m "feat: HUD 显示目标步数实时提示，修复星级显示为★符号"
```

---

### Task 6：全通关结局画面（P1）

**Files:**
- Modify: `Assets/Scripts/UI/GameHUD.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs`

- [ ] **Step 1：修改 GameHUD.cs — 增加 allCompletePanel**

**① 在 `[Header("Panels")]` 下增加字段**：

```csharp
public GameObject allCompletePanel;    // 全通关面板

[Header("All Complete")]
public TextMeshProUGUI totalStarsText;
public Button          allCompleteBtnReplay;
public Button          allCompleteBtnMenu;
```

**② 在 `Start()` 末尾追加**：

```csharp
allCompletePanel?.SetActive(false);
allCompleteBtnReplay?.onClick.AddListener(() => GameManager.Instance?.ReplayAll());
allCompleteBtnMenu?.onClick.AddListener(()   => GameManager.Instance?.GoToMainMenu());
```

**③ 增加公开方法**：

```csharp
public void ShowAllComplete(int totalStars, int maxStars)
{
    if (totalStarsText) totalStarsText.text = $"总星数：{totalStars} / {maxStars}";
    allCompletePanel?.SetActive(true);
}

public void HideAllComplete() => allCompletePanel?.SetActive(false);
```

- [ ] **Step 2：修改 GameManager.cs — NextLevel() 全通关检测 + ReplayAll()**

**① 替换 `NextLevel()` 方法**：

```csharp
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
        hud?.ShowAllComplete(total, max);
        return;
    }

    lm.SetCurrent(lm.CurrentIndex + 1);
    LoadLevel(lm.CurrentLevel);
}
```

**② 增加 `ReplayAll()` 方法**（放在 `NextLevel()` 之后）：

```csharp
public void ReplayAll()
{
    var lm = LevelManager.Instance;
    lm.SetCurrent(0);
    hud?.HideAllComplete();
    LoadLevel(lm.CurrentLevel);
}
```

- [ ] **Step 3：Unity Inspector 配置**

1. 打开 **Gameplay** 场景
2. 在 HUD Canvas 下新建 Panel `AllCompletePanel`（默认 SetActive = false），内容：
   - TextMeshPro 文字 `TotalStarsText`（例：`总星数：38 / 45`）
   - Button `BtnReplayAll`（文字：重玩全部）
   - Button `BtnBackMenu`（文字：主菜单）
3. 将 `AllCompletePanel` 拖入 `GameHUD.allCompletePanel`
4. 将 `TotalStarsText` 拖入 `GameHUD.totalStarsText`
5. 将两个按钮拖入 `GameHUD.allCompleteBtnReplay / allCompleteBtnMenu`
6. 保存场景（Ctrl+S）

- [ ] **Step 4：手动测试（用单关快速测试法）**

临时将 `LevelManager.builtinLevels` 只保留 1 个关卡 → Play Mode 通关 → 应显示全通关面板而非跳到第 1 关  
点 **重玩全部** → 从第 1 关重新开始 ✓  
点 **主菜单** → 返回主菜单 ✓  
测试完毕后恢复 builtinLevels 原有内容

- [ ] **Step 5：提交**

```
git add Assets/Scripts/Managers/GameManager.cs Assets/Scripts/UI/GameHUD.cs
git commit -m "feat: 通关最后一关后显示全通关结局画面而非循环"
```

---

### Task 7：编辑器撤销（Ctrl+Z）（P2）

**Files:**
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`

- [ ] **Step 1：修改 LevelEditorManager.cs**

**① 在类成员变量区域增加撤销栈**（放在 `public TileType SelectedBrush` 之前）：

```csharp
private readonly Stack<int[]> _undoStack = new Stack<int[]>();
private const int MaxUndo = 50;
```

**② 增加 `PushUndo()` 辅助方法**（放在 `ApplyMechanicData()` 之前）：

```csharp
private void PushUndo()
{
    if (_undoStack.Count >= MaxUndo) return;
    _undoStack.Push((int[])editingLevel.tiles.Clone());
}
```

**③ 在 `PlaceTile()` 方法的 `if (!editingLevel.IsInBounds(x, y)) return;` 之后第一行插入**：

```csharp
PushUndo();
```

**④ 在 `EraseTile()` 方法第一行插入**：

```csharp
PushUndo();
```

**⑤ 在 `NewLevel()` 中 `_portalPairs.Clear()` 之后追加**：

```csharp
_undoStack.Clear();
```

**⑥ 增加 `Update()` 方法**（LevelEditorManager 目前无 Update，新增）：

```csharp
void Update()
{
    if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        && Input.GetKeyDown(KeyCode.Z))
    {
        UndoLastAction();
    }
}

private void UndoLastAction()
{
    if (_undoStack.Count == 0) return;
    editingLevel.tiles = _undoStack.Pop();
    ApplyMechanicData();
    GetComponent<EditorGridView>()?.Rebuild(editingLevel);
}
```

- [ ] **Step 2：手动测试**

Play Mode → LevelEditor 场景：
- 放置 3 个 Tile → Ctrl+Z 三次 → 每次撤销一个格子 ✓
- 撤销到栈空后继续按 Ctrl+Z → 不报错 ✓
- 点新建后按 Ctrl+Z → 不报错，栈已清空 ✓

- [ ] **Step 3：提交**

```
git add Assets/Scripts/LevelEditor/LevelEditorManager.cs
git commit -m "feat: 编辑器支持 Ctrl+Z 撤销画格操作，最多 50 步"
```

---

### Task 8：BGM 循环播放支持（P2）

**Files:**
- Modify: `Assets/Scripts/Managers/AudioManager.cs`
- Modify: `Assets/Scripts/UI/MainMenuUI.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/LevelEditor/LevelEditorManager.cs`

- [ ] **Step 1：替换 AudioManager.cs**

```csharp
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip sfxMove;
    public AudioClip sfxPush;
    public AudioClip sfxUndo;
    public AudioClip sfxComplete;
    public AudioClip sfxMenu;

    [Header("BGM Clips（可先留空）")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmGameplay;
    public AudioClip bgmEditor;

    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;

    private AudioSource _sfxSrc;
    private AudioSource _bgmSrc;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSrc             = gameObject.AddComponent<AudioSource>();
        _sfxSrc.playOnAwake = false;

        _bgmSrc             = gameObject.AddComponent<AudioSource>();
        _bgmSrc.loop        = true;
        _bgmSrc.playOnAwake = false;
        _bgmSrc.volume      = bgmVolume;
    }

    // SFX
    public static void PlayMove()     => Instance?._PlaySFX(Instance.sfxMove,     0.55f);
    public static void PlayPush()     => Instance?._PlaySFX(Instance.sfxPush,     0.80f);
    public static void PlayUndo()     => Instance?._PlaySFX(Instance.sfxUndo,     0.50f);
    public static void PlayComplete() => Instance?._PlaySFX(Instance.sfxComplete, 1.00f);
    public static void PlayMenu()     => Instance?._PlaySFX(Instance.sfxMenu,     0.70f);

    // BGM
    public static void PlayBGM(AudioClip clip)  => Instance?._PlayBGM(clip);
    public static void StopBGM()                => Instance?._bgmSrc?.Stop();
    public static void SetBGMVolume(float vol)
    {
        if (Instance != null) Instance._bgmSrc.volume = Mathf.Clamp01(vol);
    }

    private void _PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSrc.clip == clip && _bgmSrc.isPlaying) return;
        _bgmSrc.clip = clip;
        _bgmSrc.Play();
    }

    private void _PlaySFX(AudioClip clip, float vol)
    {
        if (clip) _sfxSrc.PlayOneShot(clip, vol);
    }
}
```

- [ ] **Step 2：在 MainMenuUI.Start() 末尾追加 BGM 调用**

```csharp
AudioManager.PlayBGM(AudioManager.Instance?.bgmMainMenu);
```

- [ ] **Step 3：在 GameManager.LoadLevel() 末尾追加 BGM 调用**

在 `hud?.UpdateMoveCount(0)` 之后追加：

```csharp
AudioManager.PlayBGM(AudioManager.Instance?.bgmGameplay);
```

- [ ] **Step 4：在 LevelEditorManager.Start() 末尾追加 BGM 调用**

在 `if (editingLevel == null) NewLevel(8, 8)` 之后追加：

```csharp
AudioManager.PlayBGM(AudioManager.Instance?.bgmEditor);
```

- [ ] **Step 5：手动测试**

- BGM Clip 全部留空时：无控制台报错，各场景正常运行 ✓
- 在 AudioManager Inspector 中给 `bgmGameplay` 指定一个测试音频 → 进入 Gameplay → 循环播放 ✓
- 在同一 Gameplay 场景重开关卡：同一 BGM 不会重新播放（防止重复切歌）✓
- 切换到主菜单：若 bgmMainMenu 与 bgmGameplay 不同，切换播放 ✓

- [ ] **Step 6：提交**

```
git add Assets/Scripts/Managers/AudioManager.cs Assets/Scripts/UI/MainMenuUI.cs Assets/Scripts/Managers/GameManager.cs Assets/Scripts/LevelEditor/LevelEditorManager.cs
git commit -m "feat: AudioManager 增加 BGM 循环播放和跨场景不重播保护"
```

---

## 实施顺序总结

| Task | 阶段 | 估时 | 前置依赖 |
|------|------|------|---------|
| 1 parMoves 序列化 | P0 | 15 min | 无 |
| 2 编辑器按钮 + parMoves 输入 | P0 | 20 min | Task 1（JSON 结构已对齐）|
| 3 暂停菜单 | P1 | 30 min | 无 |
| 4 关卡锁定 + 星级 | P1 | 20 min | 无 |
| 5 HUD parMoves + 星级修复 | P1 | 20 min | 无 |
| 6 全通关结局 | P1 | 30 min | 无 |
| 7 编辑器撤销 | P2 | 20 min | 无 |
| 8 BGM 支持 | P2 | 20 min | 无 |
