# 推箱子游戏

基于 Unity 2022.3 开发的 3D 推箱子益智游戏，包含完整的玩家流程、内置关卡、评分系统、玩法修改器和运行时关卡编辑器，支持 PC 与安卓双平台。

## 功能亮点

- 经典推箱子移动，支持撤销、重启、暂停和最佳步数记录。
- 特殊机制：冰块滑行、压力板、门控、传送门、战争迷雾。
- 30 个内置关卡，每关设有标准步数和三星评分。
- Roguelike 风格可选修改器：全冰面、随机传送门、镜像控制、战争迷雾。
- 关卡编辑器：支持格子绘制、传送门配对、门连接、合法性验证、存取档和游戏内试玩。
- NUnit EditMode 单元测试，覆盖核心玩法和编辑器验证。

## 操作说明

### PC 键盘

| 按键 | 功能 |
|---|---|
| WASD / 方向键 | 移动 |
| Ctrl+Z | 撤销 |
| R | 重新开始 |
| Escape | 暂停 |

### 安卓触屏

屏幕底部虚拟方向键（上下左右四个按钮）控制移动。

## 运行方式

用 Unity 2022.3 打开项目。若资源或场景需要重新生成，按顺序执行以下菜单：

1. `SokobanGame/1. Create Materials & Prefabs`
2. `SokobanGame/2. Create Level Data Assets`
3. `SokobanGame/3. Create All Scenes`
4. `SokobanGame/4. Build All Scene Hierarchies`
5. `SokobanGame/5. Fix Level Names (English)`

完成后打开 `Assets/Scenes/MainMenu.unity`，点击 Play 即可运行。

## 项目结构

```text
Assets/
  Editor/          Unity 编辑器工具和场景构建脚本
  Materials/       游戏材质
  Prefabs/         格子、实体和 UI 预制体
  Resources/
    Levels/        内置关卡 ScriptableObject 资源
  Scenes/          MainMenu、LevelSelect、Gameplay、LevelEditor
  Scripts/
    Core/          SokobanGrid、UndoManager、SceneTransition
    Controllers/   PlayerController、GridRenderer
    Data/          LevelData、LevelDataJson、TileType、GameState
    Managers/      GameManager、LevelManager、ModifierManager、SaveManager、AudioManager
    UI/            MainMenuUI、LevelSelectUI、GameHUD
    LevelEditor/   运行时关卡编辑器与验证逻辑
  Tests/           EditMode 单元测试
```
