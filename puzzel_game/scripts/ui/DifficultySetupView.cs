using Godot;

public partial class DifficultySetupView : Control
{
    [Signal] public delegate void StartRequestedEventHandler();

    public override void _Ready()
    {
        var rows = GetNodeOrNull<SpinBox>("SafeArea/ContentColumn/RowsBox");
        var cols = GetNodeOrNull<SpinBox>("SafeArea/ContentColumn/ColumnsBox");
        var rotation = GetNodeOrNull<CheckButton>("SafeArea/ContentColumn/RotationToggle");
        var storage = GetNodeOrNull<CheckButton>("SafeArea/ContentColumn/StorageToggle");
        var start = GetNodeOrNull<Button>("SafeArea/ContentColumn/StartButton");

        if (rows != null) rows.ValueChanged += _ => RefreshStars();
        if (cols != null) cols.ValueChanged += _ => RefreshStars();
        if (rotation != null) rotation.Toggled += _ => RefreshStars();
        if (storage != null) storage.Toggled += _ => RefreshStars();
        if (start != null) start.Pressed += () => EmitSignal(SignalName.StartRequested);

        RefreshStars();
    }

    public PuzzleConfig BuildConfig()
    {
        var rows = GetNode<SpinBox>("SafeArea/ContentColumn/RowsBox");
        var cols = GetNode<SpinBox>("SafeArea/ContentColumn/ColumnsBox");
        var rotation = GetNode<CheckButton>("SafeArea/ContentColumn/RotationToggle");
        var storage = GetNode<CheckButton>("SafeArea/ContentColumn/StorageToggle");

        return new PuzzleConfig
        {
            Rows = (int)rows.Value,
            Columns = (int)cols.Value,
            Thickness = 0.2f,
            ShuffleRotation = rotation.ButtonPressed,
            StorageMode = storage.ButtonPressed ? StorageMode.HorizontalList : StorageMode.Stack,
        };
    }

    public void RefreshStars()
    {
        var config = BuildConfig();
        var largest = Mathf.Max(config.Rows, config.Columns);
        var stars = largest <= 3 ? 1 : largest == 4 ? 2 : 3;
        if (config.ShuffleRotation) stars += 1;
        if (config.StorageMode == StorageMode.Stack) stars += 1;
        stars = Mathf.Min(stars, 5);

        var label = GetNodeOrNull<Label>("SafeArea/ContentColumn/StarsPanel/StarsMargin/StarsLabel")
            ?? GetNodeOrNull<Label>("SafeArea/ContentColumn/StarsLabel");
        if (label != null)
        {
            label.Text = $"Current Difficulty: {stars} / 5";
        }
    }
}