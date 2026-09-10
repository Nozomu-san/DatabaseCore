namespace StandardDataPipeline.Source;

public enum ProviderFailureMode
{
    Stop,
    Skip
}

public sealed class DataPipelinePolicy
{
    public bool StartWithDefault { get; set; } = true;
    public ProviderFailureMode MissingProvider { get; set; } = ProviderFailureMode.Skip;
    public ProviderFailureMode InvalidProvider { get; set; } = ProviderFailureMode.Stop;
    public ProviderFailureMode UnavailableProvider { get; set; } = ProviderFailureMode.Stop;
    public int? MaximumConcurrentProviders { get; set; }
    public TimeSpan? ProviderTimeout { get; set; }
}