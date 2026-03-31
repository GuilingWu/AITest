阶段一：环境准备（让 Codex 知道项目根目录和规范）
1. 创建项目根目录并进入
-     bash
-     mkdir 3d-puzzle-godot
-     cd 3d-puzzle-godot

2. 初始化 Git 仓库（可选，但推荐）
-     bash
-     git init
3. 创建 AGENTS.md（核心规范文件）
   在根目录创建 AGENTS.md，内容是 Codex 理解项目的“地图”。我们只写约 50 行，包含项目类型、技术栈、目录结构、关键约束。
   操作：用任意编辑器新建文件 AGENTS.md，粘贴以下内容（可适当精简）：

markdown
# AGENTS.md - 3D 拼图游戏 (Godot 4 / C#)

## 项目类型
- 引擎: Godot 4.2+ Mono 版
- 语言: C# (.NET 6+)
- 玩法: 将图片切成 M×N×H 的立体块，在存放区与拼图区间拖拽、旋转、吸附拼合

## 目录结构 (规范)
res://
├── scenes/          # 场景文件 (.tscn)
├── scripts/         # C# 脚本
│   ├── core/        # 核心逻辑
│   ├── ui/          # UI 逻辑
│   └── utils/       # 工具类
├── assets/          # 图片、字体、主题等资源
│   ├── textures/
│   ├── fonts/
│   └── themes/
└── shaders/         # 着色器

## 代码规范
- 类名: PascalCase
- 公开字段/属性: 添加 [Export]
- 私有字段: _camelCase
- 文件路径: 使用 res:// 相对路径
- 物理层: 拼图块放在 layer 2，相机射线检测 mask 包含 layer 2

## 关键类（初步设计，后续由 Codex 填充）
- Piece: 拼图块 (RigidBody3D)
- CombinedGroup: 组合体 (Node3D)
- AreaManager: 区域管理
- GameManager: 全局游戏状态
- ControlPanel: UI 控制面板


4. 创建 docs/ 目录（存放更详细的设计文档）
- bash
- mkdir docs
- 并在 docs/ 中放入未来的详细设计文档（Codex 会在需要时自动读取）。

5. 创建 skills/ 目录（存放自定义 Skill）
-     bash
-     mkdir skills
-     我们将之前设计的 7 个 Skill 放入此目录，每个 Skill 是一个子文件夹。
   操作：手动创建 skills/requirement-analyst/、skills/tech-architect/、skills/godot-ui-designer/、skills/godot-csharp-generator/、skills/resource-organizer/、skills/test-engineer/、skills/integration-deployer/。每个文件夹内至少有一个 prompt.md 文件（内容参照前文设计的 Skill 说明）。
   （如果不想手动创建所有 Skill，可以先创建需求分析师和技术架构师两个，后面按需创建。）

阶段二：需求 → 结构化 Spec（使用需求分析师 Skill）
6. 编写需求描述文件
- 创建 docs/requirements_raw.txt，粘贴最初的拼图需求：

text
我想做一个3d拼图游戏，将一张图片根据设置参数m行n列h厚度，生成对应的3d拼图块，并且能打乱顺序，任意旋转摆放，可重叠的堆放在屏幕选取rect内，通过拖拽图块到拼图区rect2，可以通过点击一次顺时针旋转90度，将相邻图块在相同旋转角度情况下拼在一起，拼在一起的图块可以和单独图块一样进行拖拽点击操作。
7. 激活需求分析师 Skill，生成 Spec
- bash
- codex skill activate requirement-analyst --input docs/requirements_raw.txt --output docs/spec.md
- Codex 会读取 skills/requirement-analyst/prompt.md，理解角色设定，输出符合格式的 Spec 文档。
   预期产出：docs/spec.md，内容类似之前我们写的结构化 Spec（用户故事、Gherkin 场景、技术约束等）。

8. 检查 Spec 并微调（可选）
- 用编辑器打开 docs/spec.md，确认没有明显遗漏。如果感觉某些点不够清晰，可以手动补充一句话，或者在对话中要求 Codex 修改。

阶段三：技术架构设计（使用技术架构师 Skill）
9. 激活技术架构师 Skill，生成技术设计文档
-  bash
-   codex skill activate tech-architect --input docs/spec.md --output docs/design.md
-  Codex 会基于 Spec 和 AGENTS.md 中的规范，输出包含类设计、算法、场景树结构的技术设计文档。
-  预期产出：docs/design.md，包含 Piece、CombinedGroup 等类的详细设计，以及关键算法的伪代码。

10. 审查设计文档（可选）
- 打开 docs/design.md，确认核心类和方法符合预期。如果需要调整，可以手动修改，或要求 Codex 重新生成。

阶段四：代码生成（并行使用多个代码生成 Skill）
现在我们有技术设计，可以并行生成代码和资源。注意 Codex 支持同时运行多个会话，我们分别执行。

11. 生成 UI 场景（Godot UI 设计师 Skill）
-  bash
-  codex skill activate godot-ui-designer --input docs/design.md --output scenes/main.tscn
-  Codex 会生成 main.tscn 文本，包含 CanvasLayer、控制面板、按钮、输入框等，并应用主题规范。
    产出：scenes/main.tscn。

12. 生成核心 C# 脚本（Godot C# 代码生成器 Skill）
    这个 Skill 需要生成多个文件，我们可以用一条命令指向一个输出目录，Codex 会自动创建多个文件：

- bash
- codex skill activate godot-csharp-generator --input docs/design.md --output-dir scripts/core/
- 预期产出：scripts/core/Piece.cs、scripts/core/CombinedGroup.cs、scripts/core/AreaManager.cs、scripts/core/GameManager.cs 等。

13. 生成 UI 逻辑脚本
-    bash
-    codex skill activate godot-csharp-generator --input docs/design.md --output-dir scripts/ui/ --filter "ControlPanel"
-    （这里用 filter 指定只生成 UI 相关类，或者简单地把所有脚本都放一起，但规范是分开目录，所以可以分别执行。）

14. 生成工具类
-    bash
-    codex skill activate godot-csharp-generator --input docs/design.md --output-dir scripts/utils/ --filter "GeometryHelper,ImageProcessor"
15. 生成资源规范（资源规范师 Skill）
-     bash
-     codex skill activate resource-organizer --project-type godot-3d --output assets/
-     这个 Skill 会创建 assets/themes/game_theme.tres、assets/fonts/ 占位等，并输出目录树说明。
-     产出：assets/ 下的主题文件、字体文件（占位）等。

16. 生成测试用例（测试工程师 Skill）
-     bash
-     codex skill activate test-engineer --input scripts/core/ --output tests/
-     Codex 会为 Piece、CombinedGroup、GeometryHelper 等生成单元测试代码（使用 NUnit 或 GdUnit4）。
-     产出：tests/GeometryHelperTest.cs 等。

阶段五：集成与验证
17. 整合项目并生成运行指南（集成部署师 Skill）
-     bash
-     codex skill activate integration-deployer --output README.md
-     Codex 会读取所有已生成的文件，检查路径规范，并生成一份 README.md，包含如何用 Godot 打开项目、运行、导出等说明。
-     产出：README.md。

18. 启动 Codex 的自动迭代优化（可选）
-     此时项目已经可以运行，但可能还有 bug 或未完全实现的功能。你可以要求 Codex 进入迭代模式：

-     bash
-     codex run "打开 Godot 编辑器，加载项目，运行主场景，观察拖拽和旋转行为，如果发现 bug，自动修复并重新测试。"
-     Codex 会自主打开编辑器（需配置）、模拟操作、发现问题、生成修复 diff 并应用。这个能力依赖于 Codex 桌面应用的自动化功能，具体配置可参考官方文档。
-     
总结：全程操作清单
步骤	操作	产出
1-2	创建目录、初始化	项目文件夹
3	创建 AGENTS.md	项目规范
4	创建 docs/	文档目录
5	创建 skills/ 并放入自定义 Skill	Skill 定义
6	准备原始需求文件	docs/requirements_raw.txt
7	激活需求分析师 → Spec	docs/spec.md
8	（可选）微调	-
9	激活技术架构师 → 技术设计	docs/design.md
10	（可选）微调	-
11	UI 场景生成	scenes/main.tscn
12	核心 C# 脚本生成	scripts/core/*.cs
13	UI 脚本生成	scripts/ui/ControlPanel.cs
14	工具类生成	scripts/utils/*.cs
15	资源规范生成	assets/ 下主题、字体等
16	测试生成	tests/*.cs
17	集成部署 → README	README.md
18	（可选）自动迭代优化	修复 bug，完善功能
完成这 18 步后，你将得到一个完全由 Codex 生成的 Godot C# 3D 拼图游戏项目，可以直接在 Godot 编辑器中打开运行。

注意事项
Codex CLI 命令示例基于当前（2026年）的假设，实际命令可能随版本变化，请参考官方文档。

Skill 的 prompt.md 需要提前写好，我们之前已经提供了内容，你可以直接复制到对应文件夹。

AGENTS.md 是核心，Codex 会优先读取它。所以务必保证内容准确、简洁。

如果遇到权限问题或需要沙箱绕过，可以使用 --dangerously-bypass-approvals-and-sandbox 参数（仅限信任环境）。

对于大型项目，Codex 可能一次生成不完美，需要多次迭代。你可以用 codex revise 命令针对某个文件要求改进。