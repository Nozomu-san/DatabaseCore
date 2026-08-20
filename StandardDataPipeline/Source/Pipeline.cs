namespace StandardDataPipeline.Source;

public static class DataPipeline
{
    public static async ValueTask<DataPipelineResult<T>> RunAsync<T>(
        IDataModule<T> module,
        IEnumerable<IDataProvider<T>> providers,
        DataPipelineContext context,
        DataPipelinePolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(providers);

        DataPipelinePolicy effectivePolicy =
            policy ?? new DataPipelinePolicy();

        T current = module.CreateDefault();
        List<string> applied = [];
        List<DataValidationIssue> issues = [];

        if (!effectivePolicy.StartWithDefault)
        {
            applied.Clear();
        }
        else
        {
            applied.Add("BuiltInDefault");
        }

        foreach (IDataProvider<T> provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DataProviderResult<T> result =
                await provider.LoadAsync(
                    context,
                    cancellationToken).ConfigureAwait(false);

            issues.AddRange(result.Issues);

            if (!result.HasValue)
            {
                ProviderFailureMode mode = result.Status switch
                {
                    DataProviderStatus.Missing =>
                        effectivePolicy.MissingProvider,
                    DataProviderStatus.Invalid =>
                        effectivePolicy.InvalidProvider,
                    _ =>
                        effectivePolicy.UnavailableProvider
                };

                if (mode == ProviderFailureMode.Stop &&
                    result.Status != DataProviderStatus.Missing)
                {
                    break;
                }

                continue;
            }

            current = module.Merge(current, result.Value!);
            applied.Add(result.Source);
        }

        DataValidationResult validation = module.Validate(current);
        issues.AddRange(validation.Issues);

        return new(
            current,
            applied.AsReadOnly(),
            issues.AsReadOnly());
    }
}