using StandardDataPipeline.Source;

namespace StandardJsonConfiguration.Source;

internal sealed class JsonProvider<T> : IDataProvider<T>
{
    private readonly JsonContract<T> _contract;
    private readonly long? _maximumFileBytes;

    internal JsonProvider(
        string path,
        JsonContract<T> contract,
        long? maximumFileBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Name = Path.GetFullPath(path);
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
        _maximumFileBytes = maximumFileBytes;
    }

    public string Name { get; }

    public async Task<DataProviderResult<T>> LoadAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            FileStream opened = await Task.Run(
                () => new FileStream(
                    Name,
                    new FileStreamOptions
                    {
                        Mode = FileMode.Open,
                        Access = FileAccess.Read,
                        Share = FileShare.Read | FileShare.Delete,
                        BufferSize = 64 * 1024,
                        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                    }),
                cancellationToken).ConfigureAwait(false);
            await using FileStream stream = opened;

            if (_maximumFileBytes is long maximum &&
                stream.Length > maximum)
            {
                return TooLarge(stream.Length, maximum);
            }

            T? value = await JsonSerialization.DeserializeAsync(
                stream,
                _contract,
                cancellationToken).ConfigureAwait(false);

            if (_maximumFileBytes is long limit &&
                stream.Position > limit)
            {
                return TooLarge(stream.Position, limit);
            }

            return value is null
                ? DataProviderResult<T>.Failure(
                    DataProviderStatus.Invalid,
                    Name,
                    new DataValidationIssue(
                        DataValidationSeverity.Error,
                        $"JSON '{Name}' produced no value."))
                : DataProviderResult<T>.Success(value, Name);
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or
            DirectoryNotFoundException)
        {
            return DataProviderResult<T>.Missing(Name);
        }
        catch (Exception exception) when (
            exception is System.Text.Json.JsonException or NotSupportedException)
        {
            return DataProviderResult<T>.Failure(
                DataProviderStatus.Invalid,
                Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"JSON '{Name}' is invalid: {exception.Message}"));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return DataProviderResult<T>.Failure(
                DataProviderStatus.Unavailable,
                Name,
                new DataValidationIssue(
                    DataValidationSeverity.Error,
                    $"JSON '{Name}' is unavailable: {exception.Message}"));
        }
    }

    private DataProviderResult<T> TooLarge(long bytes, long maximum) =>
        DataProviderResult<T>.Failure(
            DataProviderStatus.Invalid,
            Name,
            new DataValidationIssue(
                DataValidationSeverity.Error,
                $"JSON '{Name}' is {bytes} bytes and exceeds the configured limit of {maximum} bytes."));
}

internal sealed class FixedJsonProvider<T> : IDataProvider<T>
{
    private readonly DataProviderResult<T> _result;

    internal FixedJsonProvider(DataProviderResult<T> result)
    {
        _result = result;
        Name = result.Source;
    }

    public string Name { get; }

    public Task<DataProviderResult<T>> LoadAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_result);
    }
}