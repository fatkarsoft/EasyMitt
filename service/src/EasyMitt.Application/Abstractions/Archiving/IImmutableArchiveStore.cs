namespace EasyMitt.Application.Abstractions.Archiving;

/// <summary>
/// GoBD uyumlu değiştirilemez arşiv (S3 Object Lock vb. için soyutlama).
/// </summary>
public interface IImmutableArchiveStore
{
    Task<ArchiveWriteResult> WriteAsync(byte[] payload, string contentSha256Hex, CancellationToken cancellationToken, string fileExtension = ".json");

    Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed record ArchiveWriteResult(string ObjectKey, Uri? Location);
