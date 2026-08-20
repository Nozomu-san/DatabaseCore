using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

internal sealed class JsonProvider<T>(
    string path,
    JsonContract<T> contract) : IDataProvider<T>
{
    private readonly JsonContract<T> _contract =
        contract ?? throw new ArgumentNullException(nameof(contract));

    public string Name { get; } = Path.GetFullPath(path);

    public async ValueTask<DataProviderResult<T>> LoadAsync(
        DataPipelineContext context,
        CancellationToken cancellationToken)
    {
        _ = context;

        if (!File.Exists(Name))
        {
            return DataProviderResult<T>.Missing(Name);
        }

        try
        {
            string json =
                await File.ReadAllTextAsync(
                    Name,
                    cancellationToken).ConfigureAwait(false);

            T? value = JsonSerialization.Deserialize(json, _contract);
            return value is null
                ? DataProviderResult<T>.Failure(
                    DataProviderStatus.Invalid,
                    Name,
                    new DataValidationIssue(
                        DataValidationSeverity.Error,
                        $"JSON '{Name}' produced no configuration value."))
                : DataProviderResult<T>.Success(value, Name);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            System.Text.Json.JsonException or
            NotSupportedException)
        {
            return DataProviderResult<T>.Failure(
                DataProviderStatus.Invalid,
                Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"JSON '{Name}' could not be loaded: {exception.Message}"));
        }
    }
}