namespace StandardDataPipeline.Source;

public sealed class BuiltInDefaultDataProvider<T>(
    IDataModule<T> module) : IDataProvider<T>
{
    private readonly IDataModule<T> _module =
        module ?? throw new ArgumentNullException(nameof(module));

    public string Name => "BuiltInDefault";

    public ValueTask<DataProviderResult<T>> LoadAsync(
        DataPipelineContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            DataProviderResult<T>.Success(
                _module.CreateDefault(),
                Name));
    }
}

public sealed class RuntimeMemoryDataProvider<T>(
    string name,
    Func<CancellationToken, ValueTask<T?>> reader) :
    IDataProvider<T>
{
    private readonly Func<CancellationToken, ValueTask<T?>> _reader =
        reader ?? throw new ArgumentNullException(nameof(reader));

    public string Name { get; } =
        string.IsNullOrWhiteSpace(name)
            ? "RuntimeMemory"
            : name.Trim();

    public async ValueTask<DataProviderResult<T>> LoadAsync(
        DataPipelineContext context,
        CancellationToken cancellationToken)
    {
        _ = context;
        T? value = await _reader(cancellationToken).ConfigureAwait(false);

        return value is null
            ? DataProviderResult<T>.Missing(Name)
            : DataProviderResult<T>.Success(value, Name);
    }
}