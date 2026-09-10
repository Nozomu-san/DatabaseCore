using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

internal static class JsonProviderCatalog
{
    internal static async Task<IReadOnlyList<IDataProvider<T>>> BuildAsync<T>(
        JsonLayout layout,
        JsonContract<T> contract,
        JsonConfigurationPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(policy);

        List<IDataProvider<T>> providers = [];
        if (layout.PrimaryPath is string primaryPath)
        {
            providers.Add(new JsonProvider<T>(
                primaryPath,
                contract,
                policy.MaximumFileBytes));
        }

        if (layout.AddonDirectory is not string addonDirectory)
        {
            return providers.AsReadOnly();
        }

        string[] files;
        try
        {
            files = await DiscoverFilesAsync(
                addonDirectory,
                layout.SearchPattern,
                policy.RecursiveAddons,
                cancellationToken).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException)
        {
            providers.Add(new FixedJsonProvider<T>(
                DataProviderResult<T>.Missing(addonDirectory)));
            return providers.AsReadOnly();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            providers.Add(new FixedJsonProvider<T>(
                DataProviderResult<T>.Failure(
                    DataProviderStatus.Unavailable,
                    addonDirectory,
                    new DataValidationIssue(
                        DataValidationSeverity.Error,
                        $"JSON addon directory '{addonDirectory}' could not be enumerated: {exception.Message}"))));
            return providers.AsReadOnly();
        }

        string? normalizedPrimary = layout.PrimaryPath is string primary
            ? Path.GetFullPath(primary)
            : null;

        foreach (string file in files)
        {
            if (normalizedPrimary is not null &&
                file.Equals(
                    normalizedPrimary,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            providers.Add(new JsonProvider<T>(
                file,
                contract,
                policy.MaximumFileBytes));
        }

        return providers.AsReadOnly();
    }

    private static Task<string[]> DiscoverFilesAsync(
        string directory,
        string pattern,
        bool recursive,
        CancellationToken cancellationToken) =>
        Task.Run(
            () =>
            {
                SearchOption option = recursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;
                List<string> files = [];

                foreach (string file in Directory.EnumerateFiles(
                    directory,
                    pattern,
                    option))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    files.Add(Path.GetFullPath(file));
                }

                files.Sort(StringComparer.OrdinalIgnoreCase);
                return files.ToArray();
            },
            cancellationToken);
}
