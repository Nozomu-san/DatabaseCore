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
        Status is DataProviderStatus.Success &&
        Value is not null;

    public static DataProviderResult<T> Success(T value, string source) =>
        new(
            DataProviderStatus.Success,
            value,
            NormalizeSource(source),
            []);

    public static DataProviderResult<T> Missing(string source) =>
        new(
            DataProviderStatus.Missing,
            default,
            NormalizeSource(source),
            []);

    public static DataProviderResult<T> Failure(
        DataProviderStatus status,
        string source,
        params DataValidationIssue[] issues)
    {
        if (status is DataProviderStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new(
            status,
            default,
            NormalizeSource(source),
            issues ?? []);
    }

    private static string NormalizeSource(string source) =>
        string.IsNullOrWhiteSpace(source)
            ? "Unknown"
            : source.Trim();
}

public readonly record struct DataProviderReport(
    string Source,
    DataProviderStatus Status,
    IReadOnlyList<DataValidationIssue> Issues);

public readonly record struct DataPipelineResult<T>(
    T Value,
    IReadOnlyList<string> AppliedSources,
    IReadOnlyList<DataProviderReport> Providers,
    IReadOnlyList<DataValidationIssue> Issues,
    bool Completed)
{
    public bool IsValid =>
        !Issues.Any(static issue =>
            issue.Severity is DataValidationSeverity.Error);
}