namespace StandardDataPipeline.Source;

public enum DataProviderStatus
{
    Success,
    Missing,
    Invalid,
    Unavailable
}

public readonly record struct DataProviderResult<T>(
    DataProviderStatus Status,
    T? Value,
    string Source,
    IReadOnlyList<DataValidationIssue> Issues)
{
    public bool HasValue =>
        Status == DataProviderStatus.Success &&
        Value is not null;

    public static DataProviderResult<T> Success(
        T value,
        string source) =>
        new(DataProviderStatus.Success, value, source, []);

    public static DataProviderResult<T> Missing(string source) =>
        new(DataProviderStatus.Missing, default, source, []);

    public static DataProviderResult<T> Failure(
        DataProviderStatus status,
        string source,
        params DataValidationIssue[] issues) =>
        new(status, default, source, issues);
}

public readonly record struct DataPipelineResult<T>(
    T Value,
    IReadOnlyList<string> AppliedSources,
    IReadOnlyList<DataValidationIssue> Issues)
{
    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity == DataValidationSeverity.Error);
}