using System.Net;
using Swiss.FCh.Monitoring.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Swiss.FCh.Monitoring.Tests.HealthChecks;

[TestFixture]
internal sealed class UrlHealthCheckTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

    private HttpClient? _httpClient;
    private MockHttpMessageHandler? _httpMessageHandler;

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _httpMessageHandler?.Dispose();
    }

    [Test]
    public async Task CheckHealthAsync_WhenEndpointIsReachable_ShouldReturnHealthy()
    {
        var (healthCheck, context) = CreateUrlHealthCheck(HttpStatusCode.OK);

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(result.Description, Is.EqualTo("Endpoint is reachable."));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenEndpointReturnsNonSuccessStatusCode()
    {
        var (healthCheck, context) = CreateUrlHealthCheck(HttpStatusCode.InternalServerError);

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("Endpoint returned status code (500) InternalServerError."));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenEndpointReturnsNonSuccessStatusCodeAndDegradedIsDefinedAsFailureStatus()
    {
        var (healthCheck, context) = CreateUrlHealthCheck(HttpStatusCode.InternalServerError, HealthStatus.Degraded);

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
            Assert.That(result.Description, Is.EqualTo("Endpoint returned status code (500) InternalServerError."));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_WhenEndpointIsNotReachable_ShouldReturnUnhealthy()
    {
        var (healthCheck, context) = CreateUrlHealthCheck(null);

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("Error connecting to endpoint."));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_WhenEndpointIsNotReachableAndDegradedIsDefinedAsFailureStatus_ShouldReturnDegraded()
    {
        var (healthCheck, context) = CreateUrlHealthCheck(null, HealthStatus.Degraded);

        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Degraded));
            Assert.That(result.Description, Is.EqualTo("Error connecting to endpoint."));
            Assert.That(result.Exception, Is.Null);
        });
    }

    private (UrlHealthCheck urlHealthCheck, HealthCheckContext healthCheckContext) CreateUrlHealthCheck(HttpStatusCode? statusCode, HealthStatus? failureStatus = HealthStatus.Unhealthy)
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler(statusCode);

        var httpClient = new HttpClient(mockHttpMessageHandler);
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var urlHealthCheck = new UrlHealthCheck(_httpClientFactory, "https://example.com/api/xyz");
        var healthCheckContext = new HealthCheckContext
            { Registration = new HealthCheckRegistration("UrlHealthCheck", urlHealthCheck, failureStatus, null) };

        _httpClient = httpClient;
        _httpMessageHandler = mockHttpMessageHandler;

        return (urlHealthCheck, healthCheckContext);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;

        public MockHttpMessageHandler(HttpStatusCode? statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_statusCode.HasValue)
            {
                return Task.FromResult(new HttpResponseMessage(_statusCode.Value));
            }

            throw new HttpRequestException("Network error");
        }
    }
}
