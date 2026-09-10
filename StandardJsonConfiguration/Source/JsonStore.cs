using System.Buffers;

namespace StandardJsonConfiguration.Source;

public static class JsonStore
{
    private const int BufferSize = 64 * 1024;
    private static readonly byte[] TrailingNewLine = [(byte)'\n'];

    public static async Task<bool> WriteIfChangedAsync<T>(
        string path,
        T value,
        JsonContract<T> contract,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contract);

        string target = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            await Task.Run(
                () => Directory.CreateDirectory(directory),
                cancellationToken).ConfigureAwait(false);
        }

        string fileName = Path.GetFileName(target);
        string temporary = Path.Combine(
            directory ?? Directory.GetCurrentDirectory(),
            $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (FileStream output = new(
                temporary,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    BufferSize = BufferSize,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                }))
            {
                await JsonSerialization.SerializeAsync(
                    output,
                    value,
                    contract,
                    cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(
                    TrailingNewLine.AsMemory(),
                    cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (await ContentsEqualAsync(
                target,
                temporary,
                cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Run(
                () => File.Move(temporary, target, overwrite: true),
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
            catch
            {
            }
        }
    }

    private static async Task<bool> ContentsEqualAsync(
        string leftPath,
        string rightPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream left = OpenSequentialRead(leftPath);
            await using FileStream right = OpenSequentialRead(rightPath);

            if (left.Length != right.Length)
            {
                return false;
            }

            byte[] leftBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            byte[] rightBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                while (true)
                {
                    int leftRead = await left.ReadAsync(
                        leftBuffer.AsMemory(0, BufferSize),
                        cancellationToken).ConfigureAwait(false);
                    int rightRead = await right.ReadAsync(
                        rightBuffer.AsMemory(0, BufferSize),
                        cancellationToken).ConfigureAwait(false);

                    if (leftRead != rightRead)
                    {
                        return false;
                    }
                    if (leftRead == 0)
                    {
                        return true;
                    }
                    if (!leftBuffer.AsSpan(0, leftRead)
                        .SequenceEqual(rightBuffer.AsSpan(0, rightRead)))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(leftBuffer);
                ArrayPool<byte>.Shared.Return(rightBuffer);
            }
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static FileStream OpenSequentialRead(string path) =>
        new(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read | FileShare.Delete,
                BufferSize = BufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
}