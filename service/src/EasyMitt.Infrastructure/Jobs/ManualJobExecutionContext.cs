using Quartz;

namespace EasyMitt.Infrastructure.Jobs;

/// <summary>
/// Minimal IJobExecutionContext implementasyonu — sadece elle Run-Now akışlarında kullanılır.
/// </summary>
internal sealed class ManualJobExecutionContext(IServiceProvider services) : IJobExecutionContext
{
    public IServiceProvider Services => services;

    public IScheduler Scheduler => null!;
    public ITrigger Trigger => null!;
    public ICalendar? Calendar => null;
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => null!;
    public int RefireCount => 0;
    public JobDataMap MergedJobDataMap { get; } = new();
    public IJobDetail JobDetail => null!;
    public IJob JobInstance => null!;
    public DateTimeOffset FireTimeUtc { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => null;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => null;
    public string FireInstanceId { get; } = Guid.NewGuid().ToString("N");
    public object? Result { get; set; }
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public CancellationToken CancellationToken { get; } = CancellationToken.None;

    private readonly Dictionary<object, object> _data = new();
    public object? Get(object key) => _data.TryGetValue(key, out var v) ? v : null;
    public void Put(object key, object objectValue) => _data[key] = objectValue;
}
