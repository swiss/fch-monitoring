using System.Text.Json;
using Swiss.FCh.Monitoring.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Swiss.FCh.Monitoring.Tests.HealthChecks;

[TestFixture]
internal sealed class HealthCheckResponseWriterTests
{
    [Test]
    public async Task WriteResponse_ShouldReturnHealthyReport_WhenHealthReportIsHealthy()
    {
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                {
                    "Storage grid service",
                    new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "Storage grid service is healthy",
                        exception: null,
                        duration: TimeSpan.Zero,
                        data: new Dictionary<string, object>())
                },
                {
                    "postgres db",
                    new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "Connection to database with EF context successful.",
                        exception: null,
                        duration: TimeSpan.Zero,
                        data: new Dictionary<string, object>())
                }
            },
            totalDuration: TimeSpan.FromSeconds(1));

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);

        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Storage grid service").GetProperty("status").GetString(), Is.EqualTo("Healthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Storage grid service").GetProperty("description").GetString(), Is.EqualTo("Storage grid service is healthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("postgres db").GetProperty("status").GetString(), Is.EqualTo("Healthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("postgres db").GetProperty("description").GetString(), Is.EqualTo("Connection to database with EF context successful."));
        });

        reader.Dispose();
    }

    [Test]
    public async Task WriteResponse_ShouldReturnUnhealthyReport_WhenHealthReportHasUnhealthyEntry()
    {
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                {
                    "Storage grid service",
                    new HealthReportEntry(
                        status: HealthStatus.Unhealthy,
                        description: "Storage grid service is not reachable",
                        exception: null,
                        duration: TimeSpan.Zero,
                        data: new Dictionary<string, object>())
                }
            },
            totalDuration: TimeSpan.FromSeconds(1));

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);
        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.GetProperty("status").GetString(), Is.EqualTo("Unhealthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Storage grid service").GetProperty("status").GetString(), Is.EqualTo("Unhealthy"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Storage grid service").GetProperty("description").GetString(), Is.EqualTo("Storage grid service is not reachable"));
        });

        reader.Dispose();
    }

    [Test]
    public async Task WriteResponse_ShouldReturnDegradedReport_WhenHealthReportHasDegradedEntry()
    {
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                {
                    "Some External Service",
                    new HealthReportEntry(
                        status: HealthStatus.Degraded,
                        description: "Response is slower than expected",
                        exception: null,
                        duration: TimeSpan.FromMilliseconds(500),
                        data: new Dictionary<string, object>())
                }
            },
            totalDuration: TimeSpan.FromSeconds(1));

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);

        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.GetProperty("status").GetString(), Is.EqualTo("Degraded"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Some External Service").GetProperty("status").GetString(), Is.EqualTo("Degraded"));
            Assert.That(jsonObject.GetProperty("results").GetProperty("Some External Service").GetProperty("description").GetString(), Is.EqualTo("Response is slower than expected"));
        });

        reader.Dispose();
    }

    [Test]
    public async Task WriteResponse_ShouldIncludeData_WhenHealthReportEntryHasData()
    {
        var healthReportEntryData = new Dictionary<string, object>
        {
            { "Latency", 120 },
            { "Endpoint", "https://example.com/api/health" }
        };

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                {
                    "External API",
                    new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "API responded with status 200 OK",
                        exception: null,
                        duration: TimeSpan.FromMilliseconds(120),
                        data: healthReportEntryData)
                }
            },
            totalDuration: TimeSpan.FromSeconds(1));

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);
        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.GetProperty("results").GetProperty("External API").GetProperty("data").GetProperty("Latency").GetInt32(), Is.EqualTo(120));
            Assert.That(jsonObject.GetProperty("results").GetProperty("External API").GetProperty("data").GetProperty("Endpoint").GetString(), Is.EqualTo("https://example.com/api/health"));
        });

        reader.Dispose();
    }

    [Test]
    public async Task WriteResponse_WhenHealthReportEntryHasException_ShouldNotIncludeException()
    {
        const string exceptionMessage = "Something went wrong!";
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                {
                    "Faulty service",
                    new HealthReportEntry(
                        status: HealthStatus.Unhealthy,
                        description: "An exception occurred.",
                        exception: new InvalidOperationException(exceptionMessage),
                        duration: TimeSpan.Zero,
                        data: new Dictionary<string, object>())
                }
            },
            totalDuration: TimeSpan.FromSeconds(1));

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);

        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(jsonObject.GetProperty("results").GetProperty("Faulty service").TryGetProperty("exception", out _), Is.False);
        });

        reader.Dispose();
    }

    [Test]
    public async Task WriteResponse_ShouldReturnJsonContentType()
    {
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        Assert.That(context.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
    }

    [Test]
    public async Task WriteResponse_ShouldReturnEmptyResults_WhenNoEntriesAreProvided()
    {
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await HealthCheckResponseWriter.WriteResponse(context, healthReport).ConfigureAwait(false);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var jsonResponse = await reader.ReadToEndAsync().ConfigureAwait(false);

        var jsonObject = JsonDocument.Parse(jsonResponse).RootElement;

        Assert.That(jsonObject.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
        Assert.That(jsonObject.TryGetProperty("results", out _), Is.False);

        reader.Dispose();
    }
}
