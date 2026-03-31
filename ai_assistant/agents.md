# agents.md - 3D 拼图游戏 (Godot 4 / C#)

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