namespace StandardDataPipeline.Source;

public static class DataPipeline
{
    public static async Task<DataPipelineResult<T>> RunAsync<T>(
        IDataModule<T> module,
        IEnumerable<IDataProvider<T>> providers,
        DataPipelinePolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(providers);

        DataPipelinePolicy effectivePolicy = policy ?? new DataPipelinePolicy();
        ValidatePolicy(effectivePolicy);

        IDataProvider<T>[] orderedProviders = [.. providers];
        for (int index = 0; index < orderedProviders.Length; ++index)
        {
            if (orderedProviders[index] is null)
            {
                throw new ArgumentException(
                    $"Provider at index {index} is null.",
                    nameof(providers));
            }
        }

        T current = module.CreateDefault();
        bool hasCurrent = effectivePolicy.StartWithDefault;
        bool completed = true;
        List<string> applied = [];
        List<DataValidationIssue> issues = [];
        List<DataProviderReport> reports = [];

        if (hasCurrent)
        {
            applied.Add("BuiltInDefault");
        }

        if (orderedProviders.Length != 0)
        {
            int concurrency = Math.Min(
                orderedProviders.Length,
                ResolveConcurrency(effectivePolicy.MaximumConcurrentProviders));
            Dictionary<Task<DataProviderResult<T>>, int> inFlight =
                new(concurrency);
            DataProviderResult<T>?[] ready =
                new DataProviderResult<T>?[orderedProviders.Length];
            using CancellationTokenSource pipelineCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            int nextProvider = 0;
            int nextToApply = 0;

            void StartProvider(int index)
            {
                Task<DataProviderResult<T>> task = LoadProviderAsync(
                    orderedProviders[index],
                    effectivePolicy.ProviderTimeout,
                    pipelineCancellation.Token);
                inFlight.Add(task, index);
            }

            for (; nextProvider < concurrency; ++nextProvider)
            {
                StartProvider(nextProvider);
            }

            try
            {
                while (inFlight.Count != 0 && completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task<DataProviderResult<T>> finished =
                        await Task.WhenAny(inFlight.Keys).ConfigureAwait(false);
                    int finishedIndex = inFlight[finished];
                    inFlight.Remove(finished);

                    ready[finishedIndex] = NormalizeResult(
                        orderedProviders[finishedIndex],
                        await finished.ConfigureAwait(false));

                    if (nextProvider < orderedProviders.Length)
                    {
                        StartProvider(nextProvider);
                        ++nextProvider;
                    }

                    while (nextToApply < ready.Length &&
                           ready[nextToApply] is DataProviderResult<T> result)
                    {
                        ready[nextToApply] = null;
                        reports.Add(new(
                            result.Source,
                            result.Status,
                            result.Issues));
                        issues.AddRange(result.Issues);

                        if (!result.HasValue)
                        {
                            if (ResolveFailureMode(
                                    effectivePolicy,
                                    result.Status) is ProviderFailureMode.Stop)
                            {
                                completed = false;
                                pipelineCancellation.Cancel();
                                break;
                            }
                        }
                        else
                        {
                            T incoming = result.Value!;
                            if (!hasCurrent)
                            {
                                current = incoming;
                                hasCurrent = true;
                            }
                            else
                            {
                                current = module.Merge(current, incoming);
                            }

                            applied.Add(result.Source);
                        }

                        ++nextToApply;
                    }
                }
            }
            finally
            {
                pipelineCancellation.Cancel();
                foreach (Task<DataProviderResult<T>> pending in inFlight.Keys)
                {
                    ObserveIncomplete(pending);
                }
            }
        }

        if (!hasCurrent)
        {
            current = module.CreateDefault();
        }

        DataValidationResult validation = module.Validate(current);
        issues.AddRange(validation.Issues);

        return new(
            current,
            applied.AsReadOnly(),
            reports.AsReadOnly(),
            issues.AsReadOnly(),
            completed);
    }

    private static async Task<DataProviderResult<T>> LoadProviderAsync<T>(
        IDataProvider<T> provider,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? providerCancellation = null;
        CancellationToken providerToken = cancellationToken;

        if (timeout is TimeSpan duration &&
            duration != Timeout.InfiniteTimeSpan)
        {
            providerCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            providerCancellation.CancelAfter(duration);
            providerToken = providerCancellation.Token;
        }

        Task<DataProviderResult<T>>? providerTask = null;

        try
        {
            providerTask = provider.LoadAsync(providerToken);
            if (providerTask is null)
            {
                return DataProviderResult<T>.Failure(
                    DataProviderStatus.Invalid,
                    provider.Name,
                    new DataValidationIssue(
                        DataValidationSeverity.Error,
                        $"Data provider '{provider.Name}' returned no task."));
            }

            if (timeout is TimeSpan wait &&
                wait != Timeout.InfiniteTimeSpan)
            {
                return await providerTask
                    .WaitAsync(wait, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await providerTask
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            providerCancellation?.Cancel();
            if (providerTask is not null)
            {
                ObserveIncomplete(providerTask);
            }

            return DataProviderResult<T>.Failure(
                DataProviderStatus.Unavailable,
                provider.Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"Data provider '{provider.Name}' exceeded its configured timeout."));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                  providerCancellation?.IsCancellationRequested is true)
        {
            if (providerTask is not null)
            {
                ObserveIncomplete(providerTask);
            }

            return DataProviderResult<T>.Failure(
                DataProviderStatus.Unavailable,
                provider.Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"Data provider '{provider.Name}' exceeded its configured timeout."));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            if (providerTask is not null)
            {
                ObserveIncomplete(providerTask);
            }

            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return DataProviderResult<T>.Failure(
                DataProviderStatus.Unavailable,
                provider.Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"Data provider '{provider.Name}' failed: {exception.Message}"));
        }
        finally
        {
            providerCancellation?.Dispose();
        }
    }

    private static void ObserveIncomplete<T>(Task<DataProviderResult<T>> task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
            return;
        }

        _ = task.ContinueWith(
            static completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static DataProviderResult<T> NormalizeResult<T>(
        IDataProvider<T> provider,
        DataProviderResult<T> result)
    {
        string source = string.IsNullOrWhiteSpace(result.Source)
            ? provider.Name
            : result.Source;

        if (result.Status is DataProviderStatus.Success && !result.HasValue)
        {
            return DataProviderResult<T>.Failure(
                DataProviderStatus.Invalid,
                source,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"Data provider '{provider.Name}' reported success without a value."));
        }

        return result with
        {
            Source = string.IsNullOrWhiteSpace(source)
                ? "Unknown"
                : source.Trim(),
            Issues = result.Issues ?? []
        };
    }

    private static ProviderFailureMode ResolveFailureMode(
        DataPipelinePolicy policy,
        DataProviderStatus status) =>
        status switch
        {
            DataProviderStatus.Missing => policy.MissingProvider,
            DataProviderStatus.Invalid => policy.InvalidProvider,
            _ => policy.UnavailableProvider
        };

    private static int ResolveConcurrency(int? configured)
    {
        if (configured is int explicitValue)
        {
            return explicitValue;
        }

        return Math.Max(1, Environment.ProcessorCount);
    }

    private static void ValidatePolicy(DataPipelinePolicy policy)
    {
        if (policy.MaximumConcurrentProviders is int configured &&
            configured <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                configured,
                $"{nameof(policy.MaximumConcurrentProviders)} must be greater than zero when specified.");
        }

        if (policy.ProviderTimeout is TimeSpan timeout &&
            timeout != Timeout.InfiniteTimeSpan &&
            timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                timeout,
                $"{nameof(policy.ProviderTimeout)} must be positive or Timeout.InfiniteTimeSpan when specified.");
        }
    }
}