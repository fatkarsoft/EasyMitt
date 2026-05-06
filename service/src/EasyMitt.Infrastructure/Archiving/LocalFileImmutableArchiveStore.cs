using System.Globalization;
using EasyMitt.Application.Abstractions.Archiving;

namespace EasyMitt.Infrastructure.Archiving;

/// <summary>
/// Geliştirme ve tek düğüm senaryoları için yerel dosya tabanlı değiştirilemez yazım (S3 Object Lock yerine).
/// </summary>
public sealed class LocalFileImmutableArchiveStore(string rootDirectory) : IImmutableArchiveStore
{
    public async Task<ArchiveWriteResult> WriteAsync(byte[] payload, string contentSha256Hex, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootDirectory);
        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var prefix = contentSha256Hex.Length >= 2 ? contentSha256Hex[..2] : "xx";
        var relative = Path.Combine(year, prefix, $"{contentSha256Hex}.json");
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
}
