namespace HonsenToolbox.Models;

public sealed class ToolEntry
{
    public required string Id { get; init; }
    public required string ChineseName { get; init; }
    public required string EnglishName { get; init; }
    public required string FrenchName { get; init; }
    public required string ChineseDescription { get; init; }
    public required string EnglishDescription { get; init; }
    public required string FrenchDescription { get; init; }
    public required string Url { get; init; }
    public required string Icon { get; init; }
    public string? AppId { get; init; }
    public bool IsWeb { get; init; }
    public bool IsInstallable { get; init; }
    public bool IsFavourite { get; set; }
    public int SortOrder { get; set; }
    public DateTime? LastOpenedUtc { get; set; }
    public string DisplayName { get; set; } = "";
    public string DisplayDescription { get; set; } = "";
    public string DisplayType { get; set; } = "";
    public string DisplayFavouriteSymbol { get; set; } = "☆";
    public string DisplayActionText { get; set; } = "打开 ↗";
    public string DisplayVersionInfo { get; set; } = "";
    public string DisplayToolTip { get; set; } = "";
    public string DisplayFavouriteToolTip { get; set; } = "";
    public string? InstalledVersion { get; set; }
    public string? LauncherPath { get; set; }
    public string? UpdateManifestUrl { get; set; }
    public string? LatestVersion { get; set; }
    public bool HasConnectedRunner => !string.IsNullOrWhiteSpace(LauncherPath) && System.IO.File.Exists(LauncherPath);

    public string Name(string language) => language switch
    {
        "en" => EnglishName,
        "fr" => FrenchName,
        _ => ChineseName,
    };

    public string Description(string language) => language switch
    {
        "en" => EnglishDescription,
        "fr" => FrenchDescription,
        _ => ChineseDescription,
    };
}
