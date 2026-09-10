using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

public static class StandardJsonConfiguration
{
    public static async Task<JsonConfigurationResult<T>> LoadAsync<T>(
        JsonLayout layout,
        IJsonConfigurationModule<T> module,
        JsonContract<T>? contract = null,
        JsonConfigurationPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(module);

        JsonContract<T> effectiveContract = contract ?? new JsonContract<T>();
        JsonConfigurationPolicy effectivePolicy =
            policy ?? new JsonConfigurationPolicy();
        ValidatePolicy(effectivePolicy);

        IReadOnlyList<IDataProvider<T>> providers =
            await JsonProviderCatalog.BuildAsync(
                layout,
                effectiveContract,
                effectivePolicy,
                cancellationToken).ConfigureAwait(false);

        DataPipelineResult<T> result = await DataPipeline.RunAsync(
            new ModuleAdapter<T>(module),
            providers,
            ToPipelinePolicy(effectivePolicy),
            cancellationToken).ConfigureAwait(false);

        if (result.IsValid &&
            effectivePolicy.PrimaryWriteBack is
                PrimaryWriteBackMode.MissingOnly &&
            layout.PrimaryPath is string primaryPath &&
            PrimaryWasMissing(result, primaryPath))
        {
            await JsonStore.WriteIfChangedAsync(
                primaryPath,
                module.CreateDefault(),
                effectiveContract,
                cancellationToken).ConfigureAwait(false);
        }

        return new(
            result.Value,
            result.AppliedSources,
            [.. result.Issues.Select(ConvertIssue)],
            result.Completed);
    }

    public static Task<bool> WritePrimaryAsync<T>(
        JsonLayout layout,
        T value,
        JsonContract<T>? contract = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.PrimaryPath is not string primaryPath)
        {
            throw new InvalidOperationException(
                "This JSON layout does not contain a primary configuration file.");
        }

        return JsonStore.WriteIfChangedAsync(
            primaryPath,
            value,
            contract ?? new JsonContract<T>(),
            cancellationToken);
    }

    private static bool PrimaryWasMissing<T>(
        DataPipelineResult<T> result,
        string primaryPath)
    {
        string normalized = Path.GetFullPath(primaryPath);
        foreach (DataProviderReport provider in result.Providers)
        {
            if (provider.Status is DataProviderStatus.Missing &&
                provider.Source.Equals(
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static DataPipelinePolicy ToPipelinePolicy(
        JsonConfigurationPolicy policy) =>
        new()
        {
            StartWithDefault = policy.StartWithDefault,
            MissingProvider = Convert(policy.MissingProvider),
            InvalidProvider = Convert(policy.InvalidProvider),
            UnavailableProvider = Convert(policy.UnavailableProvider),
            MaximumConcurrentProviders = policy.MaximumConcurrentFiles,
            ProviderTimeout = policy.FileTimeout
        };

    private static ProviderFailureMode Convert(
        JsonProviderFailureMode value) =>
        value is JsonProviderFailureMode.Stop
            ? ProviderFailureMode.Stop
            : ProviderFailureMode.Skip;

    private static JsonConfigurationIssue ConvertIssue(
        DataValidationIssue issue) =>
        new(
            issue.Severity switch
            {
                DataValidationSeverity.Information =>
                    JsonConfigurationSeverity.Information,
                DataValidationSeverity.Warning =>
                    JsonConfigurationSeverity.Warning,
                _ => JsonConfigurationSeverity.Error
            },
            issue.Message);

    private static void ValidatePolicy(JsonConfigurationPolicy policy)
    {
        if (policy.MaximumConcurrentFiles is int configured &&
            configured <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                configured,
                $"{nameof(policy.MaximumConcurrentFiles)} must be greater than zero when specified.");
        }

        if (policy.FileTimeout is TimeSpan timeout &&
            timeout != Timeout.InfiniteTimeSpan &&
            timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                timeout,
                $"{nameof(policy.FileTimeout)} must be positive or Timeout.InfiniteTimeSpan when specified.");
        }

        if (policy.MaximumFileBytes is long maximum && maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                maximum,
                $"{nameof(policy.MaximumFileBytes)} must be greater than zero when specified.");
        }
    }

    private sealed class ModuleAdapter<T> : IDataModule<T>
    {
        private readonly IJsonConfigurationModule<T> _inner;

        internal ModuleAdapter(IJsonConfigurationModule<T> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string DataType => _inner.DataType;
        public T CreateDefault() => _inner.CreateDefault();
        public T Merge(T current, T incoming) => _inner.Merge(current, incoming);

        public DataValidationResult Validate(T value)
        {
            IReadOnlyList<JsonConfigurationIssue> issues =
                _inner.Validate(value) ?? [];
            return new([.. issues.Select(ConvertValidationIssue)]);
        }

        private static DataValidationIssue ConvertValidationIssue(
            JsonConfigurationIssue issue) =>
            new(
                issue.Severity switch
                {
                    JsonConfigurationSeverity.Information =>
                        DataValidationSeverity.Information,
                    JsonConfigurationSeverity.Warning =>
                        DataValidationSeverity.Warning,
                    _ => DataValidationSeverity.Error
                },
                issue.Message);
    }
}