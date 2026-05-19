namespace EasyMitt.Application.Abstractions.Jobs;

public interface IJobRegistry
{
    IReadOnlyList<JobInfo> List();

    Task<JobInfo?> RunNowAsync(string name, CancellationToken ct = default);
}

public sealed record JobInfo(
    string Name,
    string Description,
    string Schedule,
    bool Enabled,
    DateTime? LastRunAtUtc,
    DateTime? NextRunAtUtc,
    string? LastStatus,
    string? LastError);
