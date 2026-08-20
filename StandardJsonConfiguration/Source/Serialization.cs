using System.Text.Json;

namespace StandardJsonConfiguration.Source;

internal static class JsonSerialization
{
    internal static T? Deserialize<T>(
        string json,
        JsonContract<T> contract) =>
        contract.TypeInfo is not null
            ? JsonSerializer.Deserialize(json, contract.TypeInfo)
            : JsonSerializer.Deserialize<T>(json, contract.Options);

    internal static string Serialize<T>(
        T value,
        JsonContract<T> contract) =>
        contract.TypeInfo is not null
            ? JsonSerializer.Serialize(value, contract.TypeInfo)
            : JsonSerializer.Serialize(value, contract.Options);
}