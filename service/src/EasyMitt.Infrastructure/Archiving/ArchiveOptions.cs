namespace EasyMitt.Infrastructure.Archiving;

public sealed class ArchiveOptions
{
    public string Backend { get; init; } = "Local";

    public string LocalRoot { get; init; } = "";

    public S3ArchiveOptions S3 { get; init; } = new();
}

public sealed class S3ArchiveOptions
{
    public string BucketName { get; init; } = "";

    public string Region { get; init; } = "eu-central-1";

    public string AccessKeyId { get; init; } = "";

    public string SecretAccessKey { get; init; } = "";

    public int ObjectLockRetentionDays { get; init; } = 3650;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BucketName);
}
