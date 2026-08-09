using System.Net.Http.Json;
using System.Text.Json;
using Billing.Application.Abstractions;
using Billing.Application.DTOs.Auth;
using Billing.Identity.Services;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Billing.IntegrationTests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{Program}"/> that configures the test
/// host with in-memory configuration (connection string, JWT settings) so the
/// application can boot without a live SQL Server or Redis instance. Database-
/// dependent services (<see cref="IUnitOfWork"/>, <see cref="ICacheService"/>)
/// are replaced with Moq stubs.
/// </summary>
public abstract class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide the configuration values that AddPersistence / AddIdentity
        // require at host-build time (before ConfigureTestServices runs).
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=BillingSystem_Test;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Jwt:Issuer"] = "BillingSystem",
                ["Jwt:Audience"] = "BillingSystemClients",
                ["Jwt:SecretKey"] =
                    "TestSecretKey_For_Integration_Tests_At_Least_32_Characters_Long_2026!",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Jwt:MaxFailedAttempts"] = "5",
                ["Jwt:LockoutMinutes"] = "15",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["Cache:InstanceName"] = "billing-test",
                ["Cache:DefaultTtlSeconds"] = "300"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Stub out database-dependent services so no live SQL Server or
            // Redis connection is attempted during the test run.
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped(_ => new Mock<IUnitOfWork>().Object);

            services.RemoveAll<ICacheService>();
            services.AddSingleton(_ => new Mock<ICacheService>().Object);
        });
    }
}

/// <summary>
/// Integration tests that boot the full API pipeline using
/// <see cref="WebApplicationFactory{TProgram}"/>. External dependencies
/// (database, Redis cache) are replaced with in-memory stubs so the tests
/// run without SQL Server or Redis.
/// </summary>
public class HealthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_200_And_Healthy_Status()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Healthy");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_Json_Content_Type()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}

/// <summary>
/// Integration tests for the authentication endpoints. The <see cref="IAuthService"/>
/// is replaced with a mock so we can verify the API envelope and routing without
/// hitting the database.
/// </summary>
public class AuthEndpointTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;
    private readonly Mock<IAuthService> _authServiceMock;

    public AuthEndpointTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
        _authServiceMock = factory.AuthServiceMock;
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Should_Return_200_And_Result_Envelope()
    {
        // Arrange
        var loginResponse = new LoginResponse(
            AccessToken: "access-token-123",
            RefreshToken: "refresh-token-456",
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            UserId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            TenantName: "Demo Shop",
            UserName: "admin",
            Email: "admin@billingsystem.com",
            FullName: "Admin User",
            Roles: new List<string> { "ShopAdmin" },
            Permissions: new List<string> { "ProductsView" });

        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Ok(loginResponse, "Login successful."));

        var client = _factory.CreateClient();
        var request = new { Email = "admin@billingsystem.com", Password = "Admin@123", TenantSlug = (string?)null };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Be("Login successful.");
        result.GetProperty("data").GetProperty("accessToken").GetString().Should().Be("access-token-123");
        result.GetProperty("data").GetProperty("refreshToken").GetString().Should().Be("refresh-token-456");
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Should_Return_422_And_Failure_Envelope()
    {
        // Arrange — Result.Fail(message) with no errors maps to 422 via ToActionResult.
        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Fail("Invalid email or password."));

        var client = _factory.CreateClient();
        var request = new { Email = "wrong@example.com", Password = "wrong", TenantSlug = (string?)null };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("success").GetBoolean().Should().BeFalse();
        result.GetProperty("message").GetString().Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_With_Empty_Body_Should_Return_400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Email = "", Password = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_With_Valid_Data_Should_Return_200()
    {
        // Arrange
        var registerResponse = new LoginResponse(
            AccessToken: "access-token-reg",
            RefreshToken: "refresh-token-reg",
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            UserId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            TenantName: "Demo Shop",
            UserName: "newuser",
            Email: "newuser@example.com",
            FullName: "New User",
            Roles: new List<string> { "ShopAdmin" },
            Permissions: new List<string> { "ProductsView" });

        _authServiceMock
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Ok(registerResponse, "Registration successful."));

        var client = _factory.CreateClient();
        var request = new
        {
            FullName = "New User",
            Email = "newuser@example.com",
            Password = "Password1",
            PhoneNumber = (string?)"555-1234",
            TenantName = "New Shop"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("accessToken").GetString().Should().Be("access-token-reg");
    }

    [Fact]
    public async Task Refresh_With_Valid_Tokens_Should_Return_200()
    {
        // Arrange
        var refreshResponse = new RefreshResponse(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            ExpiresAt: DateTime.UtcNow.AddMinutes(15));

        _authServiceMock
            .Setup(s => s.RefreshAsync(It.IsAny<RefreshRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshResponse>.Ok(refreshResponse, "Token refreshed."));

        var client = _factory.CreateClient();
        var request = new { AccessToken = "old-access", RefreshToken = "old-refresh" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("accessToken").GetString().Should().Be("new-access-token");
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Token_Should_Return_401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act — /api/auth/revoke requires [Authorize]
        var response = await client.PostAsJsonAsync("/api/auth/revoke", new { RefreshToken = "test" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Integration tests verifying that unknown routes return 404 and that the
/// API responds to non-existent endpoints gracefully.
/// </summary>
public class RoutingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RoutingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unknown_Endpoint_Should_Return_404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}

/// <summary>
/// A <see cref="TestWebApplicationFactory"/> variant that also replaces
/// <see cref="IAuthService"/> with a mock, used by <see cref="AuthEndpointTests"/>.
/// </summary>
public class AuthTestWebApplicationFactory : TestWebApplicationFactory
{
    public Mock<IAuthService> AuthServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton(AuthServiceMock.Object);
        });
    }
}
