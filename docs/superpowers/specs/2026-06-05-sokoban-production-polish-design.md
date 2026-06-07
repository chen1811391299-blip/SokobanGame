# 推箱子游戏 — 产品级完善设计文档

**日期**：2026-06-05  
**项目**：d:/MyGame/SokobanGame（Unity）  
**目标**：将现有原型提升至产品级，满足技术策划岗笔试要求  
**截止**：5 天内完成  
**推进节奏**：顺序推进（Plan A），每阶段完成后再进入下一阶段

---

## 背景与现状

游戏已具备完整架构：SokobanGrid 核心逻辑、关卡编辑器、存档系统、4 种修饰符、场景切换淡入淡出。  
主要缺口：无内置关卡内容、若干数据 Bug、流程节点缺失、UI/UX 有待打磨。

---

## 阶段一：P0 — 基础可玩（优先级最高）

### 1.1 Bug 修复

#### Bug 1：`parMoves` 未被序列化
**文件**：`Assets/Scripts/Data/LevelDataJson.cs`  
**问题**：`LevelDataJson` 缺少 `parMoves` 字段，自定义关卡保存/加载后目标步数丢失，始终按默认值 20 计算星级。  
**修复**：
- `LevelDataJson` 增加 `public int parMoves;` 字段
- `From(LevelData d)` 写入 `parMoves = d.parMoves`
- `ToLevelData()` 读取 `data.parMoves = parMoves`

#### Bug 2：关卡编辑器无法设置 `parMoves`
**文件**：`Assets/Scripts/LevelEditor/LevelEditorUI.cs`，`LevelEditorManager.cs`  
**问题**：编辑器 UI 没有目标步数输入框，设计师无法设定星级标准。  
**修复**：
- `LevelEditorUI` 增加 `TMP_InputField parMovesInput`（默认值 20）
- 保存时将输入值写入 `editingLevel.parMoves`

#### Bug 3：关卡编辑器无返回主菜单按钮
**文件**：`Assets/Scripts/LevelEditor/LevelEditorUI.cs`  
**问题**：`LevelEditorUI` 脚本无 `btnBack` 引用，进入编辑器后无法退出。  
**修复**：
- 增加 `public Button btnBack`
- `Start()` 中注册：`btnBack?.onClick.AddListener(() => SceneTransition.LoadScene("MainMenu"))`

---

### 1.2 内置关卡（12–15 关）

**设计原则**：
- 每关有明确的核心训练目标（单一机制 or 特定组合）
- 难度梯度平滑，新机制首次出现时独立成关，留足学习空间
- 每关必须有可达最优解（parMoves = 最优步数），已验证可通关
- 地图尺寸随关卡推进适度增大

**分段规划**：

| 关卡 | 机制 | 设计要点 |
|------|------|----------|
| 1–3 | 纯推箱 | 引导定位推、转角推、双箱协调 |
| 4–5 | 冰块 | 滑行控制，利用墙壁制动 |
| 6–7 | 传送门 | 空间跳跃，跨区域推箱 |
| 8–9 | 压力板 + 门 | 顺序操作，先开门再进箱 |
| 10–12 | 双机制组合 | 冰+门 / 门+传送门 / 冰+传送门 |
| 13–15 | 全机制综合 | Boss 关，需要长远规划 |

**关卡以迭代方式逐一设计**，每关包含：网格布局（坐标）、最优解步骤、设计意图、parMoves 值。

**技术实现**：关卡以 ScriptableObject（`LevelData`）形式存放于 `Assets/Resources/Levels/`，通过 Editor 工具脚本批量创建资产，并自动将它们填入 `LevelManager` 的 `builtinLevels` 数组（Inspector 序列化字段）。编辑器工具脚本放于 `Assets/Editor/LevelSetupUtility.cs`（项目中已存在此文件，可扩展）。

---

## 阶段二：P1 — 流程完整

### 2.1 暂停菜单
- ESC 键触发，`Time.timeScale = 0` 冻结游戏
- 面板含三个按钮：继续 / 重开 / 返回主菜单
- **文件**：`GameManager.cs`（新增 `TogglePause()`），`GameHUD.cs`（新增 `pausePanel` 引用）

### 2.2 关卡进度锁定
- 规则：第 1 关默认解锁，通关后解锁下一关
- `LevelSelectUI` 对未解锁关卡显示锁定样式（灰色 + 🔒），禁用点击
- 解锁判断：`SaveManager.IsCompleted(levelId)` 或 `index == 0`

### 2.3 「全部通关」结局画面
- 通关最后一关后，不再循环，而是显示专属完成界面
- 界面展示：总关卡数、总步数统计、返回主菜单按钮

### 2.4 关卡选择 UI 打磨
- 已通关关卡：显示星级（★ 数）
- 锁定关卡：灰色 + 🔒 图标
- 当前关卡：高亮边框

---

## 阶段三：P2/P3 — 编辑器完善与 UI 打磨

### 3.1 编辑器撤销（Ctrl+Z）
- `LevelEditorManager` 内维护操作栈 `Stack<int[]>`，每次 `PlaceTile` / `EraseTile` 前 push 当前 tiles 快照
- Ctrl+Z 弹出栈顶，恢复 tiles 并刷新视图

### 3.2 自定义关卡管理
- 编辑器界面新增已保存关卡列表（读取 `StreamingAssets/Levels/*.json`）
- 支持：打开已有关卡继续编辑、删除关卡（同步删除文件）

### 3.3 HUD 目标步数提示
- 游戏 HUD 显示「目标：≤ N 步」
- 当前步数接近 parMoves 时变色提示

### 3.4 星级显示优化
- 将 `"*   " "**  " "*** "` 改为 `"☆☆☆" "★☆☆" "★★☆" "★★★"` 格式

### 3.5 机制提示系统
- 每种新机制（冰块、传送门、压力板）首次出现时，在屏幕角落显示 2 秒提示文字
- 通过 `LevelData` 的 tag 或关卡序号判断是否为该机制首关

### 3.6 BGM 支持
- `AudioManager` 增加 `AudioSource bgmSource`，支持循环播放 BGM
- 新增接口：`PlayBGM(AudioClip)` / `StopBGM()` / `SetBGMVolume(float)`
- 主菜单、游戏场景、编辑器场景各用不同 BGM（资源可先留空）

---

## 关卡实现路径

1. 设计文档确认后，用关卡编辑器逐关制作（编辑器修复完成后）
2. 每关保存为 `StreamingAssets/Levels/{id}.json`
3. 或通过 Editor 脚本一次性生成所有 `LevelData` ScriptableObject 并批量关联

---

## 不在本次范围内

- 音量设置界面（`AudioManager` 留接口，UI 暂不做）
- 关卡 hint/解法提示（关卡难度已通过设计控制）
- 成就系统、排行榜

---

*文档状态：待用户审阅*
