using Godot;

public partial class ImageSelectView : Control
{
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void ImageSelectedEventHandler(string imageId);

    private ThemeCatalog _catalog = new();
    private string _currentThemeId = "scenery";

    public override void _Ready()
    {
        _catalog = ThemeCatalogLoader.Load();

        var backButton = GetNodeOrNull<Button>("SafeArea/ContentColumn/HeaderRow/BackButton");
        if (backButton != null)
        {
            backButton.Pressed += () => EmitSignal(SignalName.BackRequested);
        }

        SetTheme(_currentThemeId);
    }

    public void SetTheme(string themeId)
    {
        _currentThemeId = string.IsNullOrWhiteSpace(themeId) ? "scenery" : themeId.ToLowerInvariant();
        var theme = ThemeCatalogLoader.FindTheme(_catalog, _currentThemeId);
        SetThemeTitle(theme?.Title ?? _currentThemeId);
        PopulateImages(theme);
    }

    private void SetThemeTitle(string title)
    {
        var label = GetNodeOrNull<Label>("SafeArea/ContentColumn/HeaderRow/Title");
        if (label != null)
        {
            label.Text = title;
        }
    }

    private void PopulateImages(ThemeDefinition? theme)
    {
        var imageList = GetNodeOrNull<VBoxContainer>("SafeArea/ContentColumn/Scroll/ImageList");
        if (imageList == null)
        {
            return;
        }

        foreach (Node child in imageList.GetChildren())
        {
            child.QueueFree();
        }

        if (theme == null)
        {
            return;
        }

        HBoxContainer? currentRow = null;
        for (var index = 0; index < theme.Images.Count; index++)
        {
            if (index % 3 == 0)
            {
                currentRow = new HBoxContainer();
                currentRow.AddThemeConstantOverride("separation", 12);
                imageList.AddChild(currentRow);
            }

            currentRow?.AddChild(CreateImageCard(theme, theme.Images[index]));
        }
    }

    private Button CreateImageCard(ThemeDefinition theme, ThemeImageDefinition image)
    {
        var button = new Button
        {
            Flat = true,
            Text = string.Empty,
            CustomMinimumSize = new Vector2(160, 100),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            TooltipText = $"res://assets/textures/{theme.Folder}/{image.File}",
        };

        var placeholder = CreateFillColorRect(new Color(0.101961f, 0.219608f, 0.309804f));
        button.AddChild(placeholder);

        var preview = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        button.AddChild(preview);

        var caption = new Label
        {
            Text = image.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorTop = 1.0f,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            OffsetTop = -28.0f,
            GrowHorizontal = Control.GrowDirection.Both,
        };
        button.AddChild(caption);

        var texturePath = $"res://assets/textures/{theme.Folder}/{image.File}";
        if (ResourceLoader.Exists(texturePath))
        {
            preview.Texture = ResourceLoader.Load<Texture2D>(texturePath);
        }

        button.Pressed += () => EmitSignal(SignalName.ImageSelected, image.Id);
        return button;
    }

    private static ColorRect CreateFillColorRect(Color color)
    {
        return new ColorRect
        {
            Color = color,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
    }
}
