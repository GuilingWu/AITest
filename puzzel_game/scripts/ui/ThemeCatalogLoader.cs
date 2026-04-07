using System;
using System.Collections.Generic;
using Godot;

public sealed class ThemeCatalog
{
    public List<ThemeDefinition> Themes { get; } = new();
}

public sealed class ThemeDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Folder { get; init; } = string.Empty;
    public List<ThemeImageDefinition> Images { get; } = new();
}

public sealed class ThemeImageDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string File { get; init; } = string.Empty;
}

public static class ThemeCatalogLoader
{
    public const string CatalogPath = "res://assets/themes/theme_catalog.json";

    public static ThemeCatalog Load()
    {
        var catalog = new ThemeCatalog();
        if (!FileAccess.FileExists(CatalogPath))
        {
            return catalog;
        }

        using var file = FileAccess.Open(CatalogPath, FileAccess.ModeFlags.Read);
        var jsonText = file.GetAsText();
        var parsed = Json.ParseString(jsonText);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            return catalog;
        }

        var root = parsed.AsGodotDictionary();
        if (!root.TryGetValue("themes", out var themesVariant) || themesVariant.VariantType != Variant.Type.Array)
        {
            return catalog;
        }

        foreach (var themeVariant in themesVariant.AsGodotArray())
        {
            if (themeVariant.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }

            var themeDict = themeVariant.AsGodotDictionary();
            var theme = new ThemeDefinition
            {
                Id = ReadString(themeDict, "id"),
                Title = ReadString(themeDict, "title"),
                Folder = ReadString(themeDict, "folder"),
            };

            if (themeDict.TryGetValue("images", out var imagesVariant) && imagesVariant.VariantType == Variant.Type.Array)
            {
                foreach (var imageVariant in imagesVariant.AsGodotArray())
                {
                    if (imageVariant.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var imageDict = imageVariant.AsGodotDictionary();
                    theme.Images.Add(new ThemeImageDefinition
                    {
                        Id = ReadString(imageDict, "id"),
                        Title = ReadString(imageDict, "title"),
                        File = ReadString(imageDict, "file"),
                    });
                }
            }

            catalog.Themes.Add(theme);
        }

        return catalog;
    }

    public static ThemeDefinition? FindTheme(ThemeCatalog catalog, string themeId)
    {
        foreach (var theme in catalog.Themes)
        {
            if (string.Equals(theme.Id, themeId, StringComparison.OrdinalIgnoreCase))
            {
                return theme;
            }
        }

        return null;
    }

    private static string ReadString(Godot.Collections.Dictionary dictionary, string key)
    {
        return dictionary.TryGetValue(key, out var value) ? value.AsString() : string.Empty;
    }
}
