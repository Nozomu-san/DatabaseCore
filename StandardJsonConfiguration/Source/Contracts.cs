using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace StandardJsonConfiguration.Source;

public sealed class JsonContract<T>
{
    private JsonSerializerOptions _options =
        new(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

    public JsonSerializerOptions Options
    {
        get => _options;
        set => _options = value ?? throw new ArgumentNullException(nameof(value));
    }

    public JsonTypeInfo<T>? TypeInfo { get; set; }
}

public enum JsonConfigurationSeverity
{
    Information,
    Warning,
    Error
}

public readonly record struct JsonConfigurationIssue(
    JsonConfigurationSeverity Severity,
    string Message);

public interface IJsonConfigurationModule<T>
{
    string DataType { get; }
    T CreateDefault();
    T Merge(T current, T incoming);
    IReadOnlyList<JsonConfigurationIssue> Validate(T value);
}

public readonly record struct JsonConfigurationResult<T>(
    T Value,
    IReadOnlyList<string> AppliedSources,
    IReadOnlyList<JsonConfigurationIssue> Issues,
    bool Completed)
{
    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity is JsonConfigurationSeverity.Error);
}
