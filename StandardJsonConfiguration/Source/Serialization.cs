using System.Text.Json;

namespace StandardJsonConfiguration.Source;

internal static class JsonSerialization
{
    internal static ValueTask<T?> DeserializeAsync<T>(
        Stream utf8Json,
        JsonContract<T> contract,
        CancellationToken cancellationToken) =>
        contract.TypeInfo is not null
            ? JsonSerializer.DeserializeAsync(
                utf8Json,
                contract.TypeInfo,
                cancellationToken)
            : JsonSerializer.DeserializeAsync<T>(
                utf8Json,
                contract.Options,
                cancellationToken);

    internal static Task SerializeAsync<T>(
        Stream utf8Json,
        T value,
        JsonContract<T> contract,
        CancellationToken cancellationToken) =>
        contract.TypeInfo is not null
            ? JsonSerializer.SerializeAsync(
                utf8Json,
                value,
                contract.TypeInfo,
                cancellationToken)
            : JsonSerializer.SerializeAsync(
                utf8Json,
                value,
                contract.Options,
                cancellationToken);
}