namespace StandardDataPipeline.Source;

public sealed class InMemoryDataProvider<T>(string name, T? value) : IDataProvider<T>
{
    public string Name { get; } = NormalizeName(name, "InMemory");

    public Task<DataProviderResult<T>> LoadAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            value is null
                ? DataProviderResult<T>.Missing(Name)
                : DataProviderResult<T>.Success(value!, Name));
    }

    private static string NormalizeName(string name, string fallback) =>
        string.IsNullOrWhiteSpace(name)
            ? fallback
            : name.Trim();
}

public sealed class RuntimeMemoryDataProvider<T>(
    string name,
    Func<CancellationToken, Task<T?>> reader) : IDataProvider<T>
{
    private readonly Func<CancellationToken, Task<T?>> _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public string Name { get; } = string.IsNullOrWhiteSpace(name)
            ? "RuntimeMemory"
            : name.Trim();

    public async Task<DataProviderResult<T>> LoadAsync(
        CancellationToken cancellationToken)
    {
        T? value = await _reader(cancellationToken).ConfigureAwait(false);
        return value is null
            ? DataProviderResult<T>.Missing(Name)
            : DataProviderResult<T>.Success(value!, Name);
    }
}