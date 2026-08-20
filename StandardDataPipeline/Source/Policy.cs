namespace StandardDataPipeline.Source;

public enum ProviderFailureMode
{
    Stop,
    Skip
}

public sealed record DataPipelinePolicy
{
    public bool StartWithDefault { get; init; } = true;

    public ProviderFailureMode MissingProvider { get; init; } =
        ProviderFailureMode.Skip;

    public ProviderFailureMode InvalidProvider { get; init; } =
        ProviderFailureMode.Stop;

    public ProviderFailureMode UnavailableProvider { get; init; } =
        ProviderFailureMode.Stop;
}