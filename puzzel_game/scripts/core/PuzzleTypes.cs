using System;
using System.Collections.Generic;
using Godot;

public enum StorageMode
{
    HorizontalList = 0,
    Stack = 1,
}

public enum PieceArea
{
    Unknown = 0,
    Storage = 1,
    Puzzle = 2,
}

[GlobalClass]
public partial class PuzzleConfig : Resource
{
    [Export] public int Rows { get; set; } = 3;
    [Export] public int Columns { get; set; } = 3;
    [Export] public float Thickness { get; set; } = 0.2f;
    [Export] public bool ShuffleRotation { get; set; } = true;
    [Export] public StorageMode StorageMode { get; set; } = StorageMode.Stack;
}

public sealed class PuzzleImageInfo
{
    public string Id { get; init; } = string.Empty;
    public string ThemeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
}

public sealed class PieceDescriptor
{
    public int PieceId { get; init; }
    public Vector2I GridIndex { get; init; }
    public Rect2 UvRect { get; init; }
    public Vector3 SolvedLocalPosition { get; init; }
}

public sealed class PuzzleResult
{
    public string ImageId { get; init; } = string.Empty;
    public bool Completed { get; init; }
    public int Stars { get; init; }
    public long DurationMs { get; init; }
}

public sealed class ImageProgress
{
    public bool Completed { get; set; }
    public int BestStars { get; set; }
    public long LastPlayedTicks { get; set; }
}

public sealed class ThemeProgress
{
    public int CompletedCount { get; set; }
    public Dictionary<string, ImageProgress> Images { get; init; } = new();
}

public sealed class CustomImageProgress
{
    public string Path { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int BestStars { get; set; }
}

public sealed class ProgressData
{
    public int Version { get; set; } = 1;
    public Dictionary<string, ThemeProgress> Themes { get; init; } = new();
    public Dictionary<string, CustomImageProgress> CustomImages { get; init; } = new();
}
