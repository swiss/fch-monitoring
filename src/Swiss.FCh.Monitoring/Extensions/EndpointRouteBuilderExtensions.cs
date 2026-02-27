using Swiss.FCh.Monitoring.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Swiss.FCh.Monitoring.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapFChHealthChecks(this IEndpointRouteBuilder endpoints, string pattern = "/api/systemstatus")
    {
        endpoints
            .MapHealthChecks(pattern, new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteResponse
            })
            .AllowAnonymous();
    }
}
