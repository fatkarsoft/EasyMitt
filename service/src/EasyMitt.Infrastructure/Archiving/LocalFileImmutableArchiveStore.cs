using System.Globalization;
using EasyMitt.Application.Abstractions.Archiving;

namespace EasyMitt.Infrastructure.Archiving;

/// <summary>
/// Geliştirme ve tek düğüm senaryoları için yerel dosya tabanlı değiştirilemez yazım (S3 Object Lock yerine).
/// </summary>
public sealed class LocalFileImmutableArchiveStore(string rootDirectory) : IImmutableArchiveStore
{
    public async Task<ArchiveWriteResult> WriteAsync(byte[] payload, string contentSha256Hex, CancellationToken cancellationToken, string fileExtension = ".json")
    {
        Directory.CreateDirectory(rootDirectory);
        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var prefix = contentSha256Hex.Length >= 2 ? contentSha256Hex[..2] : "xx";
        var safeExtension = string.IsNullOrWhiteSpace(fileExtension) ? ".bin" : fileExtension.Trim();
        if (!safeExtension.StartsWith('.'))
        {
            safeExtension = $".{safeExtension}";
        }

        var relative = Path.Combine(year, prefix, $"{contentSha256Hex}{safeExtension}");
        var fullPath = Path.Combine(rootDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (!File.Exists(fullPath))
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.WriteThrough | FileOptions.Asynchronous);
            await stream.WriteAsync(payload, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        var key = relative.Replace('\\', '/');
        return new ArchiveWriteResult(key, new Uri(fullPath, UriKind.Absolute));
    }

    public async Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var normalizedKey = objectKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, normalizedKey));
        var rootPath = Path.GetFullPath(rootDirectory);
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }
}
