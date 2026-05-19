using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Infrastructure.Archiving;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Diagnostics;

public sealed class ArchiveHealthCheck(
    IImmutableArchiveStore store,
    IOptions<ArchiveOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object> { ["backend"] = options.Value.Backend };
        try
        {
            // Olmayan bir nesneyi okumayı dene; null dönmesi yeterli (yazımı doğrulamak için PUT yapmıyoruz).
            _ = await store.ReadAsync("__healthprobe__", cancellationToken);
            return HealthCheckResult.Healthy("Archive backend reachable.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Archive backend not reachable.", ex, data);
        }
    }
}
