using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed class PuzzleSession
{
    public event Action<PuzzleResult>? Completed;

    public PuzzleImageInfo ImageInfo { get; }
    public PuzzleConfig Config { get; }
    public List<Piece> Pieces { get; } = new();
    public List<CombinedGroup> CombinedGroups { get; } = new();
    public DateTimeOffset StartTime { get; private set; }

    public PuzzleSession(PuzzleImageInfo imageInfo, PuzzleConfig config)
    {
        ImageInfo = imageInfo;
        Config = config;
    }

    public void Build()
    {
        StartTime = DateTimeOffset.UtcNow;
        Pieces.Clear();
        CombinedGroups.Clear();
    }

    public int SolvedCount => Pieces.Count(piece => piece.CurrentArea == PieceArea.Puzzle);

    public float GetCurrentProgress()
    {
        var total = Math.Max(1, Pieces.Count);
        return (float)SolvedCount / total;
    }

    public int CalculateStars()
    {
        var largestDimension = Math.Max(Config.Rows, Config.Columns);
        var baseStars = largestDimension <= 3 ? 1 : largestDimension == 4 ? 2 : 3;

        if (Config.ShuffleRotation)
        {
            baseStars += 1;
        }

        if (Config.StorageMode == StorageMode.Stack)
        {
            baseStars += 1;
        }

        return Math.Min(baseStars, 5);
    }

    public bool TryComplete()
    {
        var total = Config.Rows * Config.Columns;
        var largestGroupSize = CombinedGroups.Count == 0 ? 0 : CombinedGroups.Max(group => group.Pieces.Count);
        var completed = largestGroupSize == total || (Pieces.Count == total && Pieces.All(piece => piece.CurrentArea == PieceArea.Puzzle));

        if (!completed)
        {
            return false;
        }

        var duration = DateTimeOffset.UtcNow - StartTime;
        Completed?.Invoke(new PuzzleResult
        {
            ImageId = ImageInfo.Id,
            Completed = true,
            Stars = CalculateStars(),
            DurationMs = (long)duration.TotalMilliseconds,
        });
        return true;
    }
}
