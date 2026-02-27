using Swiss.FCh.Monitoring.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swiss.FCh.Monitoring.Extensions;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddDatabase<TContext>(this IHealthChecksBuilder builder, string name = "database") where TContext : DbContext
    {
        return builder.AddCheck<DatabaseHealthCheck<TContext>>(name);
    }

    public static IHealthChecksBuilder AddUrl(this IHealthChecksBuilder builder, string name, string url, HealthStatus failureStatus = HealthStatus.Unhealthy,
        string? httpClientName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (string.IsNullOrWhiteSpace(httpClientName))
        {
            builder.Services.AddHttpClient(name);
            return builder.AddTypeActivatedCheck<UrlHealthCheck>(name, args: [url], failureStatus: failureStatus);
        }

        builder.Services.AddHttpClient(httpClientName);
        return builder.AddTypeActivatedCheck<UrlHealthCheck>(name, args: [url, httpClientName], failureStatus: failureStatus);
    }
}
