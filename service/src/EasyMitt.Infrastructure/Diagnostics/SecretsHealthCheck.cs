using EasyMitt.Infrastructure.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Diagnostics;

/// <summary>
/// Startup uyarı amaçlı — kritik secret'ların mevcut olduğunu doğrular.
/// </summary>
public sealed class SecretsHealthCheck(IOptions<ConfiguredIdentityOptions> identityOptions) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(identityOptions.Value.SigningKey))
            missing.Add("Authentication:SigningKey");

        if (missing.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Kritik secret eksik.",
                data: new Dictionary<string, object> { ["missing"] = string.Join(",", missing) }));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Secrets configured."));
    }
}
