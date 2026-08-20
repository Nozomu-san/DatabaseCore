using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

public enum PrimaryWriteBackMode
{
    Never,
    MissingOnly,
    Explicit
}

public sealed record JsonConfigurationPolicy
{
    public bool LoadPrimary { get; init; } = true;
    public bool LoadAddons { get; init; } = true;
    public bool RecursiveAddons { get; init; }
    public PrimaryWriteBackMode PrimaryWriteBack { get; init; } =
        PrimaryWriteBackMode.Never;

    public DataPipelinePolicy Pipeline { get; init; } = new();
}