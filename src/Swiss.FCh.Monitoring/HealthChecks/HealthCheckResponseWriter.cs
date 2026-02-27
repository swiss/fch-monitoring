using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swiss.FCh.Monitoring.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    public static async Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(healthReport);

        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = healthReport.Status.ToString(),
            results = healthReport.Entries.Count == 0
                ? null
                : healthReport.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data.Count == 0 ? null : entry.Value.Data
                    }
                )
        };

        var json = JsonSerializer.Serialize(response, _jsonSerializerOptions);
        await context.Response.WriteAsync(json, Encoding.UTF8);
    }
}
