using Godot;

public partial class ResultView : Control
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void BackToThemeRequestedEventHandler();

    public override void _Ready()
    {
        var retry = GetNodeOrNull<Button>("Center/Card/Content/RetryButton");
        var back = GetNodeOrNull<Button>("Center/Card/Content/BackToThemeButton");

        if (retry != null)
        {
            retry.Pressed += () => EmitSignal(SignalName.RetryRequested);
        }

        if (back != null)
        {
            back.Pressed += () => EmitSignal(SignalName.BackToThemeRequested);
        }
    }

    public void SetResult(PuzzleResult result)
    {
        var stars = GetNodeOrNull<Label>("Center/Card/Content/Stars");
        var summary = GetNodeOrNull<Label>("Center/Card/Content/Summary");
        if (stars != null)
        {
            stars.Text = $"{result.Stars} / 5 Stars";
        }

        if (summary != null)
        {
            summary.Text = result.Completed
                ? $"Completed in {result.DurationMs / 1000.0f:0.0}s"
                : "Puzzle not completed.";
        }
    }
}