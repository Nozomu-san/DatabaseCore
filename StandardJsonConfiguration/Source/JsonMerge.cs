using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

internal static class JsonProviderCatalog
{
    internal static IReadOnlyList<IDataProvider<T>> Build<T>(
        JsonLayout layout,
        JsonContract<T> contract,
        JsonConfigurationPolicy policy)
    {
        List<IDataProvider<T>> providers = [];

        if (policy.LoadPrimary)
        {
            providers.Add(new JsonProvider<T>(
                layout.PrimaryPath,
                contract));
        }

        string? addonDirectory = layout.AddonDirectory;
        if (!policy.LoadAddons ||
            string.IsNullOrWhiteSpace(addonDirectory) ||
            !Directory.Exists(addonDirectory))
        {
            return providers.AsReadOnly();
        }

        SearchOption searchOption = policy.RecursiveAddons
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        string[] files = [.. Directory
            .EnumerateFiles(
                addonDirectory,
                layout.SearchPattern,
                searchOption)
            .Select(Path.GetFullPath)
            .Where(path =>
                !path.Equals(
                    Path.GetFullPath(layout.PrimaryPath),
                    StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)];

        foreach (string file in files)
        {
            providers.Add(new JsonProvider<T>(file, contract));
        }

        return providers.AsReadOnly();
    }
}