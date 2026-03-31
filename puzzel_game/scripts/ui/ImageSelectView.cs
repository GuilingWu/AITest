using Godot;

public partial class ImageSelectView : Control
{
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void ImageSelectedEventHandler(string imageId);

    public override void _Ready()
    {
        var backButton = GetNodeOrNull<Button>("SafeArea/ContentColumn/HeaderRow/BackButton");
        if (backButton != null)
        {
            backButton.Pressed += () => EmitSignal(SignalName.BackRequested);
        }

        ConnectImageButton("SafeArea/ContentColumn/Scroll/ImageList/ImageCard01", "scenery_01");
        ConnectImageButton("SafeArea/ContentColumn/Scroll/ImageList/ImageCard02", "scenery_02");
        ConnectImageButton("SafeArea/ContentColumn/Scroll/ImageList/ImageCard03", "scenery_03");
        ConnectImageButton("SafeArea/ContentColumn/Scroll/ImageList/ImageCard04", "scenery_04");
    }

    public void SetThemeTitle(string title)
    {
        var label = GetNodeOrNull<Label>("SafeArea/ContentColumn/HeaderRow/Title");
        if (label != null)
        {
            label.Text = title;
        }
    }

    private void ConnectImageButton(string path, string imageId)
    {
        var button = GetNodeOrNull<Button>(path);
        if (button != null)
        {
            button.Pressed += () => EmitSignal(SignalName.ImageSelected, imageId);
        }
    }
}