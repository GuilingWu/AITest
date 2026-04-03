using System;
using System.Collections.Generic;
using Godot;

public partial class GameManager : Node
{
    [Export] public NodePath ScreenRouterPath { get; set; } = new("UIRoot/ScreenRouter");
    [Export] public string ThemeSelectScenePath { get; set; } = "res://scenes/ui/theme_select.tscn";
    [Export] public string ImageSelectScenePath { get; set; } = "res://scenes/ui/image_select.tscn";
    [Export] public string DifficultySetupScenePath { get; set; } = "res://scenes/ui/difficulty_setup.tscn";
    [Export] public string PuzzleGameScenePath { get; set; } = "res://scenes/ui/puzzle_game.tscn";
    [Export] public string ResultScenePath { get; set; } = "res://scenes/ui/result_popup.tscn";

    private Control? _screenRouter;
    private ThemeSelectView? _themeSelectView;
    private ImageSelectView? _imageSelectView;
    private DifficultySetupView? _difficultySetupView;
    private ControlPanel? _controlPanel;
    private ResultView? _resultView;

    private PuzzleSession? _currentSession;
    private PuzzleImageInfo? _selectedImage;
    private PuzzleConfig? _lastConfig;
    private string _selectedThemeId = "scenery";
    private ThemeCatalog _themeCatalog = new();
    private Texture2D? _activePuzzleTexture;
    private readonly ProgressRepository _progressRepository = new();
    private ProgressData _progressData = new();
    private readonly PieceFactory _pieceFactory = new();

    public override void _Ready()
    {
        Initialize();
    }

    public void Initialize()
    {
        _screenRouter = GetNodeOrNull<Control>(ScreenRouterPath);
        _progressData = _progressRepository.Load();
        _themeCatalog = ThemeCatalogLoader.Load();
        EnsureUiScenes();
        ResolveViews();
        ConnectSignals();
        ShowThemeSelect();
    }

    public void ShowThemeSelect()
    {
        ShowScreen("ThemeSelectUI");
    }

    public void ShowImageSelect(string themeId)
    {
        if (!string.IsNullOrEmpty(themeId))
        {
            _selectedThemeId = themeId.ToLowerInvariant();
            _imageSelectView?.SetTheme(_selectedThemeId);
        }

        ShowScreen("ImageSelectUI");
    }

    public void ShowDifficultySetup()
    {
        _difficultySetupView?.RefreshStars();
        ShowScreen("DifficultySetupUI");
    }

    public void StartPuzzle(PuzzleImageInfo image, PuzzleConfig config)
    {
        _selectedImage = image;
        _lastConfig = config;

        _currentSession = new PuzzleSession(image, config);
        _currentSession.Completed += HandleSessionCompleted;
        _currentSession.Build();

        BuildPuzzleScene(config);
        RefreshPuzzleHud();
        ShowScreen("PuzzleGameUI");
    }

    public void CompletePuzzle(PuzzleResult result)
    {
        _progressRepository.UpdateBestResult(_progressData, result.ImageId, result.Stars, result.Completed);
        _progressRepository.Save(_progressData);
        _resultView?.SetResult(result);
        ShowScreen("ResultUI");
    }

    private void EnsureUiScenes()
    {
        if (_screenRouter == null)
        {
            return;
        }

        EnsureSceneChild(ThemeSelectScenePath, "ThemeSelectUI");
        EnsureSceneChild(ImageSelectScenePath, "ImageSelectUI");
        EnsureSceneChild(DifficultySetupScenePath, "DifficultySetupUI");
        EnsureSceneChild(PuzzleGameScenePath, "PuzzleGameUI");
        EnsureSceneChild(ResultScenePath, "ResultUI");
    }

    private void EnsureSceneChild(string scenePath, string nodeName)
    {
        if (_screenRouter == null || _screenRouter.HasNode(nodeName))
        {
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            GD.PushWarning($"Failed to load scene: {scenePath}");
            return;
        }

        var instance = scene.Instantiate<Control>();
        instance.Name = nodeName;
        _screenRouter.AddChild(instance);
    }

    private void ResolveViews()
    {
        if (_screenRouter == null)
        {
            return;
        }

        _themeSelectView = _screenRouter.GetNodeOrNull<ThemeSelectView>("ThemeSelectUI");
        _imageSelectView = _screenRouter.GetNodeOrNull<ImageSelectView>("ImageSelectUI");
        _difficultySetupView = _screenRouter.GetNodeOrNull<DifficultySetupView>("DifficultySetupUI");
        _controlPanel = _screenRouter.GetNodeOrNull<ControlPanel>("PuzzleGameUI");
        _resultView = _screenRouter.GetNodeOrNull<ResultView>("ResultUI");
    }

    private void ConnectSignals()
    {
        if (_themeSelectView != null)
        {
            _themeSelectView.ThemeSelected -= OnThemeSelected;
            _themeSelectView.PickFromAlbumRequested -= OnPickFromAlbumRequested;
            _themeSelectView.ThemeSelected += OnThemeSelected;
            _themeSelectView.PickFromAlbumRequested += OnPickFromAlbumRequested;
        }

        if (_imageSelectView != null)
        {
            _imageSelectView.BackRequested -= ShowThemeSelect;
            _imageSelectView.ImageSelected -= OnImageSelected;
            _imageSelectView.BackRequested += ShowThemeSelect;
            _imageSelectView.ImageSelected += OnImageSelected;
        }

        if (_difficultySetupView != null)
        {
            _difficultySetupView.StartRequested -= OnStartRequested;
            _difficultySetupView.StartRequested += OnStartRequested;
        }

        if (_controlPanel != null)
        {
            _controlPanel.ExitRequested -= ShowThemeSelect;
            _controlPanel.ExitRequested += ShowThemeSelect;
        }

        if (_resultView != null)
        {
            _resultView.RetryRequested -= RetryLastPuzzle;
            _resultView.BackToThemeRequested -= ShowThemeSelect;
            _resultView.RetryRequested += RetryLastPuzzle;
            _resultView.BackToThemeRequested += ShowThemeSelect;
        }
    }

    private void OnThemeSelected(string themeId)
    {
        ShowImageSelect(themeId);
    }

    private void OnPickFromAlbumRequested()
    {
        _selectedThemeId = "custom";
        _selectedImage = CreatePlaceholderImageInfo("custom_album", "custom", "Album Image");
        ShowDifficultySetup();
    }

    private void OnImageSelected(string imageId)
    {
        _selectedImage = ResolvePuzzleImageInfo(_selectedThemeId, imageId)
            ?? CreatePlaceholderImageInfo(imageId, _selectedThemeId, imageId);
        ShowDifficultySetup();
    }

    private void OnStartRequested()
    {
        var image = _selectedImage ?? CreatePlaceholderImageInfo("default_image", "default", "Default Image");
        var config = _difficultySetupView?.BuildConfig() ?? new PuzzleConfig();
        StartPuzzle(image, config);
    }

    private void RetryLastPuzzle()
    {
        if (_selectedImage != null && _lastConfig != null)
        {
            StartPuzzle(_selectedImage, _lastConfig);
            return;
        }

        ShowDifficultySetup();
    }

    private void BuildPuzzleScene(PuzzleConfig config)
    {
        if (_controlPanel == null || _currentSession == null)
        {
            return;
        }

        var puzzleRoot = _controlPanel.GetNode<Node3D>("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot");
        var piecesRoot = puzzleRoot.GetNode<Node3D>("PiecesRoot");
        var groupsRoot = puzzleRoot.GetNode<Node3D>("GroupsRoot");

        foreach (Node child in piecesRoot.GetChildren())
        {
            child.QueueFree();
        }

        foreach (Node child in groupsRoot.GetChildren())
        {
            child.QueueFree();
        }

        var areaManager = GetOrCreateChild<AreaManager>(_controlPanel, "AreaManager");
        areaManager.CameraPath = new NodePath("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/Camera3D");

        var mergeSystem = GetOrCreateChild<MergeSystem>(_controlPanel, "MergeSystem");
        mergeSystem.GroupsRootPath = new NodePath("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/GroupsRoot");

        var inputController = GetOrCreateChild<InputController>(_controlPanel, "InputController");
        inputController.CameraPath = new NodePath("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot/Camera3D");
        inputController.ViewportContainerPath = new NodePath("SafeArea/RootColumn/PuzzleViewportContainer");
        inputController.PuzzleViewportPath = new NodePath("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport");
        inputController.AreaManagerPath = new NodePath("../AreaManager");
        inputController.MergeSystemPath = new NodePath("../MergeSystem");
        inputController.InteractionCommitted -= OnPuzzleInteractionCommitted;
        inputController.InteractionCommitted += OnPuzzleInteractionCommitted;

        var texture = LoadPuzzleTexture(_currentSession.ImageInfo);
        _activePuzzleTexture = texture;
        var descriptors = _pieceFactory.BuildDescriptors(texture, config);
        _currentSession.Pieces.Clear();
        _currentSession.CombinedGroups.Clear();

        for (var index = 0; index < descriptors.Count; index++)
        {
            var piece = _pieceFactory.CreatePieceNode(descriptors[index], texture, config);
            piece.Position = areaManager.GetRandomStoragePosition(index, config.StorageMode);
            piece.SetArea(PieceArea.Storage);
            piecesRoot.AddChild(piece);
            _currentSession.Pieces.Add(piece);
        }

        _controlPanel.SetStorageMode(config.StorageMode);
        _controlPanel.PopulatePieceList(_currentSession.Pieces, texture);
    }

    private void OnPuzzleInteractionCommitted()
    {
        if (_currentSession == null || _controlPanel == null)
        {
            return;
        }

        SyncCombinedGroups();
        if (_activePuzzleTexture != null)
        {
            _controlPanel.PopulatePieceList(_currentSession.Pieces, _activePuzzleTexture);
        }
        RefreshPuzzleHud();
        _currentSession.TryComplete();
    }

    private void SyncCombinedGroups()
    {
        if (_currentSession == null || _controlPanel == null)
        {
            return;
        }

        var puzzleRoot = _controlPanel.GetNode<Node3D>("SafeArea/RootColumn/PuzzleViewportContainer/PuzzleViewport/PuzzleRoot");
        var groupsRoot = puzzleRoot.GetNode<Node3D>("GroupsRoot");
        var groups = new List<CombinedGroup>();
        foreach (Node child in groupsRoot.GetChildren())
        {
            if (child is CombinedGroup group)
            {
                groups.Add(group);
            }
        }

        _currentSession.CombinedGroups.Clear();
        _currentSession.CombinedGroups.AddRange(groups);
    }

    private void RefreshPuzzleHud()
    {
        if (_currentSession == null || _controlPanel == null)
        {
            return;
        }

        var total = _currentSession.Config.Rows * _currentSession.Config.Columns;
        _controlPanel.RefreshStars(_currentSession.CalculateStars());
        _controlPanel.RefreshProgress(_currentSession.SolvedCount, total);
    }

    private PuzzleImageInfo? ResolvePuzzleImageInfo(string themeId, string imageId)
    {
        var theme = ThemeCatalogLoader.FindTheme(_themeCatalog, themeId);
        if (theme == null)
        {
            return null;
        }

        foreach (var image in theme.Images)
        {
            if (!string.Equals(image.Id, imageId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new PuzzleImageInfo
            {
                Id = image.Id,
                ThemeId = themeId,
                DisplayName = string.IsNullOrWhiteSpace(image.Title) ? image.Id : image.Title,
                SourcePath = $"res://assets/textures/{theme.Folder}/{image.File}",
                IsCustom = themeId == "custom",
            };
        }

        return null;
    }

    private Texture2D LoadPuzzleTexture(PuzzleImageInfo imageInfo)
    {
        if (!string.IsNullOrWhiteSpace(imageInfo.SourcePath) && ResourceLoader.Exists(imageInfo.SourcePath))
        {
            var loaded = ResourceLoader.Load<Texture2D>(imageInfo.SourcePath);
            if (loaded != null)
            {
                return loaded;
            }
        }

        return CreatePlaceholderTexture();
    }

    private static T GetOrCreateChild<T>(Node parent, string name) where T : Node, new()
    {
        var existing = parent.GetNodeOrNull<T>(name);
        if (existing != null)
        {
            return existing;
        }

        var created = new T { Name = name };
        parent.AddChild(created);
        return created;
    }

    private static PuzzleImageInfo CreatePlaceholderImageInfo(string id, string themeId, string displayName)
    {
        return new PuzzleImageInfo
        {
            Id = id,
            ThemeId = themeId,
            DisplayName = displayName,
            SourcePath = string.Empty,
            IsCustom = themeId == "custom",
        };
    }

    private static Texture2D CreatePlaceholderTexture()
    {
        var image = Image.Create(16, 16, false, Image.Format.Rgba8);
        image.Fill(new Color(0.75f, 0.82f, 0.93f));
        return ImageTexture.CreateFromImage(image);
    }

    private void HandleSessionCompleted(PuzzleResult result)
    {
        CompletePuzzle(result);
    }

    private void ShowScreen(string screenName)
    {
        if (_screenRouter == null)
        {
            GD.PushWarning("ScreenRouter not found.");
            return;
        }

        foreach (Node child in _screenRouter.GetChildren())
        {
            if (child is CanvasItem item)
            {
                item.Visible = string.Equals(item.Name.ToString(), screenName, StringComparison.Ordinal);
            }
        }
    }
}
