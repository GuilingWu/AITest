using Godot;

public partial class ThemeSelectView : Control
{
    [Signal] public delegate void ThemeSelectedEventHandler(string themeId);
    [Signal] public delegate void PickFromAlbumRequestedEventHandler();

    public override void _Ready()
    {
        ConnectButton("SafeArea/ContentColumn/ThemeList/AnimalsButton", "animals");
        ConnectButton("SafeArea/ContentColumn/ThemeList/SceneryButton", "scenery");
        ConnectButton("SafeArea/ContentColumn/ThemeList/ArchitectureButton", "architecture");

        var albumButton = GetNodeOrNull<Button>("SafeArea/ContentColumn/AlbumButton");
        if (albumButton != null)
        {
            albumButton.Pressed += () => EmitSignal(SignalName.PickFromAlbumRequested);
        }
    }

    private void ConnectButton(string path, string themeId)
    {
        var button = GetNodeOrNull<Button>(path);
        if (button != null)
        {
            button.Pressed += () => EmitSignal(SignalName.ThemeSelected, themeId);
        }
    }
}