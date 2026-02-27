using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swiss.FCh.Monitoring.HealthChecks;

public class UrlHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _url;
    private readonly string? _httpClientName;

    public UrlHealthCheck(IHttpClientFactory httpClientFactory, string url, string? httpClientName = null)
    {
        _httpClientFactory = httpClientFactory;
        _url = url;
        _httpClientName = httpClientName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var client = _httpClientFactory.CreateClient(_httpClientName ?? context.Registration.Name);
            var response = await client.GetAsync(_url, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Endpoint is reachable.")
                : context.Registration.FailureStatus == HealthStatus.Degraded
                    ? HealthCheckResult.Degraded($"Endpoint returned status code ({(int)response.StatusCode}) {response.StatusCode}.")
                    : HealthCheckResult.Unhealthy($"Endpoint returned status code ({(int)response.StatusCode}) {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return context.Registration.FailureStatus == HealthStatus.Degraded
                ? HealthCheckResult.Degraded("Error connecting to endpoint.", ex)
                : HealthCheckResult.Unhealthy("Error connecting to endpoint.", ex);
        }
    }
}
