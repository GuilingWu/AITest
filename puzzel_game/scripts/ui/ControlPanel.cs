using Godot;

public partial class ControlPanel : Control
{
    [Signal] public delegate void ExitRequestedEventHandler();

    public override void _Ready()
    {
        var backButton = GetNodeOrNull<Button>("SafeArea/RootColumn/TopBarPanel/TopBar/BackButton")
            ?? GetNodeOrNull<Button>("SafeArea/RootColumn/TopBar/BackButton");
        if (backButton != null)
        {
            backButton.Pressed += () => EmitSignal(SignalName.ExitRequested);
        }
    }

    public void RefreshProgress(int solved, int total)
    {
        var label = GetNodeOrNull<Label>("SafeArea/RootColumn/TopBarPanel/TopBar/ProgressLabel")
            ?? GetNodeOrNull<Label>("SafeArea/RootColumn/TopBar/ProgressLabel");
        if (label != null)
        {
            label.Text = $"Progress {solved} / {total}";
        }
    }

    public void RefreshStars(int stars)
    {
        var label = GetNodeOrNull<Label>("SafeArea/RootColumn/TopBarPanel/TopBar/StarLabel")
            ?? GetNodeOrNull<Label>("SafeArea/RootColumn/TopBar/StarLabel");
        if (label != null)
        {
            label.Text = $"{stars} Stars";
        }
    }
}