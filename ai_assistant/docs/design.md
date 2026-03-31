# 竖屏 3D 拼图游戏 Technical Design

## 1. 设计目标
- 基于 [spec.md](D:\test\AITest\ai_assistant\docs\spec.md) 将需求落地为可实现的 Godot 4.2 + C# 技术方案。
- 优先保证移动端竖屏体验、拖拽与旋转反馈、拼图块自动拼合、组合体可持续操作。
- 设计结果应可直接指导后续 `scenes/`、`scripts/core/`、`scripts/ui/`、`scripts/utils/` 的代码生成。

## 2. 总体架构

### 2.1 分层
- `scenes/`
  - 场景组织与节点装配。
- `scripts/core/`
  - 拼图块、组合体、区域管理、状态流转、拼合与完成检测。
- `scripts/ui/`
  - 主题选择、图片选择、难度设置、拼图 HUD、完成界面。~~~~
- `scripts/utils/`
  - 图片切片、UV 计算、网格/坐标转换、存档读写、星级计算。
- `assets/`
  - 主题图、字体、主题资源、占位图与音效。

### 2.2 运行时模块
- `GameManager`
  - 全局状态入口，负责流程切换、关卡初始化、进度保存。
- `PuzzleSession`
  - 单局拼图上下文，持有当前图片、难度、块集合、组合体集合、完成状态。
- `AreaManager`
  - 管理拼图区与堆放区的边界、坐标转换、合法放置检测。
- `PieceFactory`
  - 根据原图与参数 `M/N/H` 生成 `Piece` 数据与节点。
- `MergeSystem`
  - 判断块/组合体是否满足拼合条件并执行重组。
- `ProgressRepository`
  - 负责本地 JSON 存取。

## 3. 场景结构

### 3.1 启动场景
- `puzzel_game/scenes/Main.tscn`
  - `Main (Node)`
  - `GameManager`
  - `UIRoot (CanvasLayer)`
  - `ScreenRouter (Control)`

`GameManager` 启动后加载主题数据与本地进度，再切换到主题选择界面。

### 3.2 UI 场景拆分
- `puzzel_game/scenes/ui/theme_select.tscn`
- `puzzel_game/scenes/ui/image_select.tscn`
- `puzzel_game/scenes/ui/difficulty_setup.tscn`
- `puzzel_game/scenes/ui/puzzle_game.tscn`
- `puzzel_game/scenes/ui/result_popup.tscn`

### 3.3 拼图主场景树
```text
PuzzleGame (Control)
├── SafeArea (MarginContainer)
│   ├── TopBar (Control)
│   ├── PuzzleViewportContainer (SubViewportContainer)
│   │   └── PuzzleViewport (SubViewport)
│   │       └── PuzzleRoot (Node3D)
│   │           ├── Camera3D
│   │           ├── DirectionalLight3D
│   │           ├── PuzzleAreaRoot (Node3D)
│   │           ├── StorageAreaRoot (Node3D)
│   │           ├── PiecesRoot (Node3D)
│   │           ├── GroupsRoot (Node3D)
│   │           └── EffectsRoot (Node3D)
│   ├── BottomPanel (Control)
│   │   ├── StorageModeTabs
│   │   ├── HorizontalPieceList
│   │   └── StackHintOverlay
│   └── OverlayLayer (CanvasLayer)
│       ├── DragGhost
│       ├── ToastLabel
│       └── CompletionPanel
```

### 3.4 3D 空间约定
- 世界坐标的 `XZ` 平面作为拼图平面。
- `Y` 轴仅用于厚度与轻微层级偏移。
- 拼图区和堆放区在同一 `SubViewport` 中表现，便于统一射线拾取与拖拽。
- 竖屏 UI 使用 `Control` 负责安全区适配，3D 部分集中在中部可视区域。

## 4. 核心数据模型

### 4.1 PuzzleConfig
```csharp
public partial class PuzzleConfig : Resource
{
    [Export] public int Rows { get; set; } = 3;
    [Export] public int Columns { get; set; } = 3;
    [Export] public float Thickness { get; set; } = 0.2f;
    [Export] public bool ShuffleRotation { get; set; } = true;
    [Export] public StorageMode StorageMode { get; set; } = StorageMode.Stack;
}
```

### 4.2 PuzzleImageInfo
```csharp
public sealed class PuzzleImageInfo
{
    public string Id { get; init; } = string.Empty;
    public string ThemeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
}
```

### 4.3 PieceDescriptor
```csharp
public sealed class PieceDescriptor
{
    public int PieceId { get; init; }
    public Vector2I GridIndex { get; init; }
    public Rect2 UvRect { get; init; }
    public Vector3 SolvedLocalPosition { get; init; }
}
```

### 4.4 ProgressData
```csharp
public sealed class ProgressData
{
    public Dictionary<string, ThemeProgress> Themes { get; init; } = new();
    public List<CustomImageProgress> CustomImages { get; init; } = new();
}
```

## 5. 核心类设计

### 5.1 GameManager
- 责任
  - 管理应用级流程与界面切换。
  - 持有当前 `PuzzleSession`。
  - 调用存档、主题数据、图片导入、结算保存。
- 关键字段
  - `_progressRepository`
  - `_currentSession`
  - `_screenRouter`
- 关键方法
  - `Initialize()`
  - `ShowThemeSelect()`
  - `ShowImageSelect(string themeId)`
  - `StartPuzzle(PuzzleImageInfo image, PuzzleConfig config)`
  - `CompletePuzzle(PuzzleResult result)`

### 5.2 PuzzleSession
- 责任
  - 持有单局运行态。
  - 提供完成度、当前星级、块数量、模式等信息。
- 关键字段
  - `ImageInfo`
  - `Config`
  - `Pieces`
  - `CombinedGroups`
  - `SolvedCount`
  - `StartTime`
- 关键方法
  - `Build()`
  - `TryComplete()`
  - `GetCurrentProgress()`

### 5.3 Piece
- 继承：`RigidBody3D` 或 `Node3D`
- 结论：建议实现为 `Node3D + Area3D + CollisionShape3D + MeshInstance3D`
  - 原因：拼图块不需要真实物理模拟，只需要稳定的拖拽、射线检测、旋转和拼合。
  - 可以避免刚体在移动端上带来的抖动与额外物理成本。
- 责任
  - 表示单个拼图块。
  - 记录网格坐标、当前旋转状态、所属组合体、当前区域。
  - 响应选中、拖拽、旋转、吸附与拼合查询。
- 关键字段
  - `Descriptor`
  - `CurrentQuarterTurns`
  - `CurrentArea`
  - `GroupId`
  - `_meshInstance`
  - `_area3D`
- 关键方法
  - `Initialize(PieceDescriptor descriptor, Material material)`
  - `RotateClockwise()`
  - `SetDragState(bool dragging)`
  - `GetSolvedWorldPosition()`
  - `CanMergeWith(Piece other)`

### 5.4 CombinedGroup
- 继承：`Node3D`
- 责任
  - 持有多个 `Piece` 的组合关系。
  - 提供整体拖拽、整体旋转、整体拼合入口。
  - 维护组内相对局部变换。
- 关键字段
  - `GroupId`
  - `Pieces`
  - `CurrentQuarterTurns`
- 关键方法
  - `AddPiece(Piece piece)`
  - `AbsorbGroup(CombinedGroup other)`
  - `RotateClockwise()`
  - `RefreshLocalLayout()`

### 5.5 AreaManager
- 责任
  - 管理拼图区与堆放区的逻辑边界。
  - 将屏幕触点投射到 3D 平面。
  - 提供区域进入、离开、回弹、堆放区随机布局。
- 关键字段
  - `PuzzleRectWorld`
  - `StorageRectWorld`
  - `Camera3D`
- 关键方法
  - `TryProjectPointerToBoard(Vector2 screenPoint, out Vector3 worldPoint)`
  - `IsInsidePuzzleArea(Vector3 worldPoint)`
  - `IsInsideStorageArea(Vector3 worldPoint)`
  - `GetRandomStoragePosition(int index, StorageMode mode)`
  - `ClampToPuzzleArea(Vector3 worldPoint)`

### 5.6 MergeSystem
- 责任
  - 在块移动、旋转、释放后检查是否满足拼合条件。
  - 合并 `Piece-Piece`、`Piece-Group`、`Group-Group`。
- 关键方法
  - `TryMerge(Node3D candidate)`
  - `FindMergeTargets(Node3D candidate)`
  - `CanMerge(Piece a, Piece b)`
  - `ExecuteMerge(MergePair pair)`

### 5.7 PieceFactory
- 责任
  - 切图、创建材质、生成网格、生成 `PieceDescriptor`。
- 关键方法
  - `BuildDescriptors(Texture2D texture, PuzzleConfig config)`
  - `CreatePieceNode(PieceDescriptor descriptor, Texture2D texture, PuzzleConfig config)`
  - `CreatePuzzleMaterial(Rect2 uvRect, Texture2D texture)`

### 5.8 ProgressRepository
- 责任
  - 本地 JSON 的读取、写入、兼容升级。
- 关键方法
  - `Load()`
  - `Save(ProgressData data)`
  - `UpdateBestResult(string imageId, int stars, bool completed)`

### 5.9 ControlPanel
- 责任
  - 拼图界面的顶部状态栏和底部堆放模式切换 UI。
- 关键方法
  - `Bind(PuzzleSession session)`
  - `RefreshProgress(int solved, int total)`
  - `RefreshStars(int stars)`

## 6. 关键算法设计

### 6.1 切图与 UV 映射
1. 读取原始 `Texture2D` 宽高。
2. 将图像逻辑上划分为 `Rows x Columns`。
3. 对每个网格生成：
   - `GridIndex`
   - `UvRect`
   - `SolvedLocalPosition`
4. 使用统一 BoxMesh 或自定义 Mesh，前表面采用对应 `UvRect`，侧面/背面可使用统一材质。

说明：
- 为减少节点和材质数量，可共用一个 ShaderMaterial，并通过实例参数传入 `UvRect`。
- 若 Godot C# 对实例材质参数管理复杂，可退回每块独立 `StandardMaterial3D`，先保证正确性。

### 6.2 拖拽流程
1. 玩家按下屏幕。
2. `InputController` 通过 `PhysicsRayQueryParameters3D` 射线命中 `Piece` 或 `CombinedGroup`。
3. 若命中单块且其属于组合体，则提升选择对象为对应 `CombinedGroup`。
4. 将屏幕坐标投影到拼图平面，记录抓取偏移。
5. 拖拽过程中持续更新对象世界坐标。
6. 抬手后：
   - 若目标落点在拼图区内，执行吸附与拼合检查。
   - 若目标落点非法，回弹到上一个合法位置或堆放区位置。

### 6.3 点击与旋转判定
- 使用按下和抬起时间差、移动阈值区分“点击”和“拖拽”。
- 推荐阈值：
  - 按压时长 `< 180ms`
  - 位移 `< 12px`
- 判定为点击时，调用 `RotateClockwise()`，然后触发拼合检查。

### 6.4 拼合判定
拼合条件：
- 两个块在逻辑网格上相邻。
- 当前旋转四分之一圈数一致。
- 世界空间相对位移与理论解位置偏差小于阈值。

推荐阈值：
- 位置误差：`0.15f ~ 0.25f`
- 角度误差：因只允许 90 度增量，可直接比较整型旋转状态。

伪代码：
```csharp
bool CanMerge(Piece a, Piece b)
{
    if (a.CurrentQuarterTurns != b.CurrentQuarterTurns)
        return false;

    if (!GridHelper.AreAdjacent(a.Descriptor.GridIndex, b.Descriptor.GridIndex))
        return false;

    var expectedDelta = a.GetSolvedWorldPosition() - b.GetSolvedWorldPosition();
    var currentDelta = a.GlobalPosition - b.GlobalPosition;

    return currentDelta.DistanceTo(expectedDelta) <= MergeDistanceThreshold;
}
```

### 6.5 组合体构建
- `Piece + Piece`
  - 新建 `CombinedGroup`，将两个块重挂载到组合体下。
- `Piece + Group`
  - 将单块挂到现有组，并刷新局部位置。
- `Group + Group`
  - 合并为一个主组，销毁从组。

实现原则：
- 组合体的根节点位置取组内一个基准块的理论位置。
- 组内块保留相对于组合体根节点的局部变换。
- 整体旋转时只旋转 `CombinedGroup` 根节点，不逐块计算动画。

### 6.6 完成检测
- 条件 1：所有块均在同一个组合体内，或每块都位于正确位置并互相可连通。
- 条件 2：该组合体的逻辑网格覆盖所有 `Rows x Columns`。
- 条件 3：旋转状态为统一值，且与最终朝向兼容。

推荐实现：
- 每次拼合成功后检查最大组合体的块数。
- 若块数 == 总块数，则立即触发完成流程，避免全量扫描。

## 7. 输入系统设计

### 7.1 InputController
- 建议新增 `scripts/core/InputController.cs`
- 责任
  - 统一处理触屏/鼠标输入。
  - 区分点击与拖拽。
  - 向 `AreaManager` 请求世界坐标映射。

### 7.2 平台兼容
- 桌面调试使用鼠标左键模拟单指操作。
- 移动端正式版仅支持单指拖拽与点击。
- 多指手势本期不纳入范围，避免与旋转/拖拽冲突。

## 8. UI 设计映射

### 8.1 ThemeSelectView
- 数据源：主题配置 + `ProgressData`
- 展示项：主题名、封面图、完成数量、总数
- 事件：
  - `ThemeSelected(themeId)`
  - `PickFromAlbumRequested()`

### 8.2 ImageSelectView
- 数据源：指定主题下的图片列表 + 图片进度
- 展示项：缩略图、已完成状态、历史最高星级
- 事件：
  - `ImageSelected(imageId)`
  - `BackRequested()`

### 8.3 DifficultySetupView
- 输入项：
  - 行数
  - 列数
  - 旋转打乱开关
  - 堆放模式开关
- 输出：
  - `PuzzleConfig`
- 逻辑：
  - 每次参数变更时调用 `DifficultyCalculator.CalculateStars()`

### 8.4 PuzzleHudView
- 展示当前星级、已完成块数、返回按钮。
- 监听 `PuzzleSession` 事件刷新。

### 8.5 ResultView
- 展示本局星级、历史最好结果是否被刷新。
- 按钮：
  - `Retry`
  - `BackToTheme`

## 9. 资源与文件组织

### 9.1 建议目录
```text
ai_assistant/
├── scenes/
│   ├── main.tscn
│   ├── ui/
│   └── puzzle/
├── scripts/
│   ├── core/
│   ├── ui/
│   └── utils/
├── assets/
│   ├── textures/
│   ├── fonts/
│   ├── audio/
│   └── themes/
└── docs/
```

### 9.2 首批代码文件
- `scripts/core/GameManager.cs`
- `scripts/core/PuzzleSession.cs`
- `scripts/core/Piece.cs`
- `scripts/core/CombinedGroup.cs`
- `scripts/core/AreaManager.cs`
- `scripts/core/MergeSystem.cs`
- `scripts/core/PieceFactory.cs`
- `scripts/core/InputController.cs`
- `scripts/ui/ThemeSelectView.cs`
- `scripts/ui/ImageSelectView.cs`
- `scripts/ui/DifficultySetupView.cs`
- `scripts/ui/ControlPanel.cs`
- `scripts/ui/ResultView.cs`
- `scripts/utils/DifficultyCalculator.cs`
- `scripts/utils/ProgressRepository.cs`
- `scripts/utils/GridHelper.cs`
- `scripts/utils/ImageSliceHelper.cs`

## 10. 持久化设计

### 10.1 存档路径
- `user://progress.json`

### 10.2 JSON 结构
```json
{
  "version": 1,
  "themes": {
    "animals": {
      "completedCount": 3,
      "images": {
        "animal_01": {
          "completed": true,
          "bestStars": 4,
          "lastPlayedTicks": 638474112000000000
        }
      }
    }
  },
  "customImages": {
    "custom_01": {
      "path": "user://imports/custom_01.png",
      "completed": false,
      "bestStars": 0
    }
  }
}
```

### 10.3 兼容策略
- 预留 `version` 字段。
- 新字段追加时采用默认值兜底，不破坏旧存档读取。

## 11. 难度计算

### 11.1 DifficultyCalculator
```csharp
public static int CalculateStars(PuzzleConfig config)
{
    var baseStars = Math.Max(config.Rows, config.Columns) switch
    {
        <= 3 => 1,
        4 => 2,
        _ => 3
    };

    if (config.ShuffleRotation)
        baseStars += 1;

    if (config.StorageMode == StorageMode.Stack)
        baseStars += 1;

    return Math.Min(baseStars, 5);
}
```

### 11.2 说明
- `Stack` 模式天然对应更高操作复杂度。
- `6x6` 仍封顶 5 星，不再继续增加 UI 星级。

## 12. 性能策略
- 优先使用 `Node3D` 而非真实刚体。
- 控制每帧射线检测次数，仅在输入活跃时执行。
- 合并检查只围绕当前被操作对象和邻接候选对象，不做全场扫描。
- 纹理导入统一限制尺寸，如超过 `2048x2048` 则缩放。
- 组合体完成后减少无用碰撞体启停次数。

## 13. 风险与取舍

### 13.1 主要风险
- 自定义图片切片和 UV 映射容易出现缝隙。
- 堆叠模式下对象重叠会提升选中歧义。
- 组合体重挂载时若局部变换处理错误，容易造成拼合跳动。

### 13.2 对策
- 网格尺寸与 UV 边缘加入微小内缩，减少采样缝隙。
- 堆叠区选中优先最近摄像机且最后交互对象。
- 所有合并操作基于统一的“理论解坐标”重算局部位置。

## 14. 未决问题与默认决策
- 厚度 `H`
  - 默认固定 `0.2f`，首版不在 UI 暴露。
- 拼图区吸附
  - 采用弱吸附，只在接近正确关系时自动拼合，不做全局网格吸附。
- 重玩历史记录
  - 仅在本次星级更高时刷新 `bestStars`。
- 堆叠区选中优先级
  - 默认选择射线最近且当前可见的块；若块属于组合体，提升为组合体。

## 15. 实施顺序建议
1. 先建立 `main.tscn`、`PuzzleGame.tscn`、基础 UI 场景。
2. 实现 `PuzzleConfig`、`DifficultyCalculator`、`ProgressRepository`。
3. 实现 `PieceDescriptor`、`PieceFactory`、`Piece`。
4. 实现 `AreaManager` 与 `InputController`，打通拖拽和点击旋转。
5. 实现 `MergeSystem` 与 `CombinedGroup`。
6. 接入 `GameManager` 与 `PuzzleSession`。
7. 接入主题/图片/结果界面与存档。
8. 最后补音效、粒子和移动端权限流程。

## 16. 产出说明
- 当前 `tech-architect/prompt.md` 为空文件，因此本设计文档直接依据需求规格和 [agents.md](D:\test\AITest\ai_assistant\agents.md) 约束生成。
- 若后续补充 architect skill prompt，可在此文档基础上再做一次结构化收敛，而无需重写整体架构。
