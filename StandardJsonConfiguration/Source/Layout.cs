namespace StandardJsonConfiguration.Source;

public sealed record JsonLayout(
    string RootDirectory,
    string PrimaryFileName,
    string? AddonDirectoryName = null,
    string SearchPattern = "*.json")
{
    public string PrimaryPath =>
        Path.Combine(RootDirectory, PrimaryFileName);

    public string? AddonDirectory =>
        string.IsNullOrWhiteSpace(AddonDirectoryName)
            ? null
            : Path.Combine(RootDirectory, AddonDirectoryName);
}