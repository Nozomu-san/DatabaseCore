using System.Text;

namespace StandardJsonConfiguration.Source;

public static class JsonStore
{
    public static async ValueTask<bool> WriteIfChangedAsync<T>(
        string path,
        T value,
        JsonContract<T> contract,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contract);

        string serialized = JsonSerialization.Serialize(value, contract);
        string normalized = serialized.EndsWith('\n')
            ? serialized
            : serialized + Environment.NewLine;

        if (File.Exists(path))
        {
            string existing =
                await File.ReadAllTextAsync(
                    path,
                    Encoding.UTF8,
                    cancellationToken).ConfigureAwait(false);

            if (string.Equals(existing, normalized, StringComparison.Ordinal))
            {
                return false;
            }
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporary = path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(
                temporary,
                normalized,
                new UTF8Encoding(false),
                cancellationToken).ConfigureAwait(false);

            File.Move(temporary, path, overwrite: true);
            return true;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}