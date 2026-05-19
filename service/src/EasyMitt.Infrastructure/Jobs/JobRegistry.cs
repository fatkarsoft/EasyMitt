using System.Collections.Concurrent;
using EasyMitt.Application.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace EasyMitt.Infrastructure.Jobs;

public sealed record JobRegistration(
    string Name,
    string Description,
    string Schedule,
    bool Enabled,
    Type JobType);

public sealed class JobRunHistory
{
    private readonly ConcurrentDictionary<string, RunRecord> _runs = new();

    public void RecordSuccess(string name, DateTime atUtc) =>
        _runs[name] = new RunRecord(atUtc, "Success", null);

    public void RecordFailure(string name, DateTime atUtc, string error) =>
        _runs[name] = new RunRecord(atUtc, "Failed", error);

    public RunRecord? Get(string name) => _runs.TryGetValue(name, out var v) ? v : null;

    public sealed record RunRecord(DateTime AtUtc, string Status, string? Error);
}

public sealed class JobRegistry(
    IReadOnlyList<JobRegistration> registrations,
    ISchedulerFactory schedulerFactory,
    JobRunHistory history,
    IServiceProvider serviceProvider,
    ILogger<JobRegistry> logger) : IJobRegistry
{
    public IReadOnlyList<JobInfo> List()
    {
        var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
        var keys = scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).GetAwaiter().GetResult();
        var schedulerByName = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var triggers = scheduler.GetTriggersOfJob(key).GetAwaiter().GetResult();
            DateTime? next = null;
            foreach (var trigger in triggers)
            {
                var nextFire = trigger.GetNextFireTimeUtc();
                if (nextFire is null) continue;
                var nextUtc = nextFire.Value.UtcDateTime;
                if (next is null || nextUtc < next.Value) next = nextUtc;
            }
            schedulerByName[key.Name] = next;
        }

        return registrations
            .Select(reg =>
            {
                var run = history.Get(reg.Name);
                schedulerByName.TryGetValue(reg.Name, out var next);
                return new JobInfo(
                    reg.Name,
                    reg.Description,
                    reg.Schedule,
                    reg.Enabled,
                    run?.AtUtc,
                    next,
                    run?.Status,
                    run?.Error);
            })
            .ToArray();
    }

    public async Task<JobInfo?> RunNowAsync(string name, CancellationToken ct = default)
    {
        var registration = registrations.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        if (registration is null) return null;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var jobInstance = (IJob)ActivatorUtilities.CreateInstance(scope.ServiceProvider, registration.JobType);
            var executionContext = new ManualJobExecutionContext(scope.ServiceProvider);
            await jobInstance.Execute(executionContext);
            history.RecordSuccess(registration.Name, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Name} elle çalıştırılırken hata oluştu.", registration.Name);
            history.RecordFailure(registration.Name, DateTime.UtcNow, ex.Message);
        }

        return List().FirstOrDefault(j => string.Equals(j.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
