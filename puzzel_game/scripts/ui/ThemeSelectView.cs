using Godot;

public partial class ThemeSelectView : Control
{
    [Signal] public delegate void ThemeSelectedEventHandler(string themeId);
    [Signal] public delegate void PickFromAlbumRequestedEventHandler();

    public override void _Ready()
    {
        PopulateThemes();

        var albumButton = GetNodeOrNull<Button>("SafeArea/ContentColumn/AlbumButton");
        if (albumButton != null)
        {
            albumButton.Pressed += () => EmitSignal(SignalName.PickFromAlbumRequested);
        }
    }

    private void PopulateThemes()
    {
        var themeList = GetNodeOrNull<VBoxContainer>("SafeArea/ContentColumn/ThemeList");
        if (themeList == null)
        {
            return;
        }

        foreach (Node child in themeList.GetChildren())
        {
            child.QueueFree();
        }

        var catalog = ThemeCatalogLoader.Load();
        foreach (var theme in catalog.Themes)
        {
            var button = new Button
            {
                Text = $"{theme.Title}  {theme.Images.Count} images",
                CustomMinimumSize = new Vector2(0, 56),
            };
            button.Pressed += () => EmitSignal(SignalName.ThemeSelected, theme.Id);
            themeList.AddChild(button);
        }
    }
}
