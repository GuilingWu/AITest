using System;
using Godot;

public sealed class ProgressRepository
{
    private const string SavePath = "user://progress.json";

    public ProgressData Load()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return new ProgressData();
        }

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            var jsonText = file.GetAsText();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return new ProgressData();
            }

            return new ProgressData();
        }
        catch (Exception)
        {
            return new ProgressData();
        }
    }

    public void Save(ProgressData data)
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        var payload = new Godot.Collections.Dictionary
        {
            { "version", data.Version },
        };
        file.StoreString(Json.Stringify(payload));
    }

    public void UpdateBestResult(ProgressData data, string imageId, int stars, bool completed)
    {
        if (!data.Themes.TryGetValue("default", out var themeProgress))
        {
            themeProgress = new ThemeProgress();
            data.Themes["default"] = themeProgress;
        }

        if (!themeProgress.Images.TryGetValue(imageId, out var imageProgress))
        {
            imageProgress = new ImageProgress();
            themeProgress.Images[imageId] = imageProgress;
        }

        imageProgress.Completed = completed;
        imageProgress.BestStars = Math.Max(imageProgress.BestStars, stars);
        imageProgress.LastPlayedTicks = DateTime.UtcNow.Ticks;
        themeProgress.CompletedCount = CountCompleted(themeProgress);
    }

    private static int CountCompleted(ThemeProgress themeProgress)
    {
        var total = 0;
        foreach (var pair in themeProgress.Images)
        {
            if (pair.Value.Completed)
            {
                total += 1;
            }
        }

        return total;
    }
}