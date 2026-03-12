using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swiss.FCh.Monitoring.HealthChecks;

public class DatabaseHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private readonly TContext _dbContext;

    public DatabaseHealthCheck(TContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection is healthy.")
                : HealthCheckResult.Unhealthy("Unable to connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("An exception occurred while checking the database connection.", ex);
        }
    }
}
