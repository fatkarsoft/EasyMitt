using System.Globalization;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EasyMitt.Application.Abstractions.Archiving;
using Microsoft.Extensions.Logging;

namespace EasyMitt.Infrastructure.Archiving;

/// <summary>
/// S3 Object Lock COMPLIANCE modunda PUT eden, GoBD uyumlu immutable arşiv adaptörü.
/// </summary>
public sealed class S3ObjectLockArchiveStore : IImmutableArchiveStore, IDisposable
{
    private readonly S3ArchiveOptions _options;
    private readonly IAmazonS3 _client;
    private readonly ILogger<S3ObjectLockArchiveStore> _logger;
    private bool _disposed;

    public S3ObjectLockArchiveStore(S3ArchiveOptions options, ILogger<S3ObjectLockArchiveStore> logger)
    {
        _options = options;
        _logger = logger;
        var region = RegionEndpoint.GetBySystemName(string.IsNullOrWhiteSpace(options.Region) ? "eu-central-1" : options.Region);
        var config = new AmazonS3Config
        {
            RegionEndpoint = region,
            ForcePathStyle = false,
        };
        _client = !string.IsNullOrWhiteSpace(options.AccessKeyId)
            ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey), config)
            : new AmazonS3Client(config);
    }

    public async Task<ArchiveWriteResult> WriteAsync(byte[] payload, string contentSha256Hex, CancellationToken cancellationToken, string fileExtension = ".json")
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
            throw new InvalidOperationException("Archive:S3:BucketName tanımlı değil.");

        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var prefix = contentSha256Hex.Length >= 2 ? contentSha256Hex[..2] : "xx";
        var safeExtension = string.IsNullOrWhiteSpace(fileExtension) ? ".bin" : fileExtension.Trim();
        if (!safeExtension.StartsWith('.'))
            safeExtension = $".{safeExtension}";

        var key = $"{year}/{prefix}/{contentSha256Hex}{safeExtension}";

        try
        {
            var head = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = key };
            await _client.GetObjectMetadataAsync(head, cancellationToken);
            return new ArchiveWriteResult(key, new Uri($"s3://{_options.BucketName}/{key}", UriKind.Absolute));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // beklenen — yeni nesne yazılacak
        }

        await using var stream = new MemoryStream(payload, writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = ContentTypeFor(safeExtension),
            ChecksumSHA256 = Convert.ToBase64String(Convert.FromHexString(contentSha256Hex)),
            ObjectLockMode = ObjectLockMode.Compliance,
            ObjectLockRetainUntilDate = DateTime.UtcNow.AddDays(Math.Max(_options.ObjectLockRetentionDays, 1)),
        };

        try
        {
            await _client.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "InvalidRequest" && ex.Message.Contains("Object Lock", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "S3 bucket'ta Object Lock aktif değil; nesne lock olmadan yazıldı. Bucket: {Bucket}", _options.BucketName);
            request.ObjectLockMode = null;
            request.ObjectLockRetainUntilDate = null;
            await _client.PutObjectAsync(request, cancellationToken);
        }

        return new ArchiveWriteResult(key, new Uri($"s3://{_options.BucketName}/{key}", UriKind.Absolute));
    }

    public async Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || string.IsNullOrWhiteSpace(_options.BucketName))
            return null;

        try
        {
            using var response = await _client.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };

    public void Dispose()
    {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
    }
}
