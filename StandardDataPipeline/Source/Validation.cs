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

public sealed class DataValidationResult(IReadOnlyList<DataValidationIssue> issues)
{
    public static DataValidationResult Valid { get; } = new([]);

    public IReadOnlyList<DataValidationIssue> Issues { get; } = issues ?? throw new ArgumentNullException(nameof(issues));

    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity is DataValidationSeverity.Error);
}