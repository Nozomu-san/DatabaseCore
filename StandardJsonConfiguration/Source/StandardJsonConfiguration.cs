using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

public static class StandardJsonConfiguration
{
    public static async ValueTask<JsonConfigurationResult<T>> LoadAsync<T>(
        JsonLayout layout,
        IDataModule<T> module,
        JsonContract<T>? contract = null,
        JsonConfigurationPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(module);

        JsonContract<T> effectiveContract =
            contract ?? new JsonContract<T>();
        JsonConfigurationPolicy effectivePolicy =
            policy ?? new JsonConfigurationPolicy();

        IReadOnlyList<IDataProvider<T>> providers =
            JsonProviderCatalog.Build(
                layout,
                effectiveContract,
                effectivePolicy);

        DataPipelineResult<T> result =
            await DataPipeline.RunAsync(
                module,
                providers,
                new DataPipelineContext(layout.RootDirectory),
                effectivePolicy.Pipeline,
                cancellationToken).ConfigureAwait(false);

        if (effectivePolicy.PrimaryWriteBack ==
                PrimaryWriteBackMode.MissingOnly &&
            !File.Exists(layout.PrimaryPath))
        {
            await JsonStore.WriteIfChangedAsync(
                layout.PrimaryPath,
                result.Value,
                effectiveContract,
                cancellationToken).ConfigureAwait(false);
        }

        return new(
            result.Value,
            result.AppliedSources,
            result.Issues);
    }

    public static ValueTask<bool> WritePrimaryAsync<T>(
        JsonLayout layout,
        T value,
        JsonContract<T>? contract = null,
        CancellationToken cancellationToken = default) =>
        JsonStore.WriteIfChangedAsync(
            layout.PrimaryPath,
            value,
            contract ?? new JsonContract<T>(),
            cancellationToken);
}