namespace StandardJsonConfiguration.Source;

public enum PrimaryWriteBackMode
{
    Never,
    MissingOnly,
    Explicit
}

public enum JsonProviderFailureMode
{
    Stop,
    Skip
}

public sealed class JsonConfigurationPolicy
{
    public bool RecursiveAddons { get; set; }
    public PrimaryWriteBackMode PrimaryWriteBack { get; set; } =
        PrimaryWriteBackMode.Never;
    public bool StartWithDefault { get; set; } = true;
    public JsonProviderFailureMode MissingProvider { get; set; } =
        JsonProviderFailureMode.Skip;
    public JsonProviderFailureMode InvalidProvider { get; set; } =
        JsonProviderFailureMode.Stop;
    public JsonProviderFailureMode UnavailableProvider { get; set; } =
        JsonProviderFailureMode.Stop;
    public int? MaximumConcurrentFiles { get; set; }
    public TimeSpan? FileTimeout { get; set; }
    public long? MaximumFileBytes { get; set; }
}
