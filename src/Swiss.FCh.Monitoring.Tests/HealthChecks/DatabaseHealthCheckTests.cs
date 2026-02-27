using Swiss.FCh.Monitoring.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Swiss.FCh.Monitoring.Tests.HealthChecks;

#pragma warning disable CA1515 //needs to be public for NSubstitute to create mock
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}
#pragma warning restore CA1515

[TestFixture]
internal sealed class DatabaseHealthCheckTests
{
    private TestDbContext _dbContext;
    private DatabaseFacade _databaseFacade;
    private DatabaseHealthCheck<TestDbContext> _healthCheck;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<TestDbContext>(new DbContextOptions<TestDbContext>());
        _databaseFacade = Substitute.For<DatabaseFacade>(_dbContext);
        _dbContext.Database.Returns(_databaseFacade);
        _healthCheck = new DatabaseHealthCheck<TestDbContext>(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenCanConnect()
    {
        _databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var context = new HealthCheckContext();

        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(result.Description, Is.EqualTo("Database connection is healthy."));
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Data, Is.Empty);
        });
    }

    [Test]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenCannotConnect()
    {
        _databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        var context = new HealthCheckContext();

        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("Unable to connect to the database."));
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Data, Is.Empty);
        });
    }

    [Test]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WithException_WhenCanConnectThrows()
    {
        var exception = new InvalidOperationException("Database connection error.");
        _databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ => throw exception);
        var context = new HealthCheckContext();

        var result = await _healthCheck.CheckHealthAsync(context, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Is.EqualTo("An exception occurred while checking the database connection."));
            Assert.That(result.Exception, Is.EqualTo(exception));
            Assert.That(result.Data, Is.Empty);
        });
    }
}
