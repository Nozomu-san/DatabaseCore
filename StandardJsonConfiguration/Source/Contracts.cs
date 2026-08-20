using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

public sealed record JsonContract<T>
{
    public JsonSerializerOptions Options { get; init; } =
        new(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

    public JsonTypeInfo<T>? TypeInfo { get; init; }
}

public readonly record struct JsonConfigurationResult<T>(
    T Value,
    IReadOnlyList<string> AppliedFiles,
    IReadOnlyList<DataValidationIssue> Issues)
{
    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity == DataValidationSeverity.Error);
}