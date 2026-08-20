namespace StandardDataPipeline.Source;

public enum DataValidationSeverity
{
    Information,
    Warning,
    Error
}

public readonly record struct DataValidationIssue(
    DataValidationSeverity Severity,
    string Message);

public sealed record DataValidationResult(
    IReadOnlyList<DataValidationIssue> Issues)
{
    public static DataValidationResult Valid { get; } = new([]);

    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity == DataValidationSeverity.Error);
}