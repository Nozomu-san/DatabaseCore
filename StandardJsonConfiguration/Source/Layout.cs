namespace StandardJsonConfiguration.Source;

public enum JsonSourceKind
{
    Configuration,
    Addon,
    Hybrid
}

public abstract class JsonLayout(JsonSourceKind kind)
{
    public JsonSourceKind Kind { get; } = kind;

    internal abstract string? PrimaryPath { get; }
    internal abstract string? AddonDirectory { get; }
    internal abstract string SearchPattern { get; }
}

public sealed class JsonConfigurationLayout : JsonLayout
{
    public JsonConfigurationLayout(string filePath) :
        base(JsonSourceKind.Configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    internal override string? PrimaryPath => FilePath;
    internal override string? AddonDirectory => null;
    internal override string SearchPattern => "*.json";
}

public sealed class JsonAddonLayout : JsonLayout
{
    public JsonAddonLayout(
        string directoryPath,
        string searchPattern = "*.json") :
        base(JsonSourceKind.Addon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        DirectoryPath = Path.GetFullPath(directoryPath);
        Pattern = searchPattern.Trim();
    }

    public string DirectoryPath { get; }
    public string Pattern { get; }

    internal override string? PrimaryPath => null;
    internal override string? AddonDirectory => DirectoryPath;
    internal override string SearchPattern => Pattern;
}

public sealed class JsonHybridLayout : JsonLayout
{
    public JsonHybridLayout(
        string primaryFilePath,
        string addonDirectoryPath,
        string searchPattern = "*.json") :
        base(JsonSourceKind.Hybrid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(addonDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        PrimaryFilePath = Path.GetFullPath(primaryFilePath);
        AddonDirectoryPath = Path.GetFullPath(addonDirectoryPath);
        Pattern = searchPattern.Trim();
    }

    public string PrimaryFilePath { get; }
    public string AddonDirectoryPath { get; }
    public string Pattern { get; }

    internal override string? PrimaryPath => PrimaryFilePath;
    internal override string? AddonDirectory => AddonDirectoryPath;
    internal override string SearchPattern => Pattern;
}
