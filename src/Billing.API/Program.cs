using System.Text.Json.Serialization;
using Billing.Application;
using Billing.Infrastructure;
using Billing.Infrastructure.Middleware;
using Billing.Identity;
using Billing.Persistence;
using Serilog;

// ── Bootstrap Serilog (two-stage init: read minimal config before host build) ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (full configuration from appsettings) ──
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    // ── Layer DI registrations (Clean Architecture composition root) ──
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddIdentity(builder.Configuration);

    // ── API services ──
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger / OpenAPI with JWT bearer support ──
    builder.Services.AddSwaggerGen(options =>
    {
        // Enable [SwaggerOperation] / [SwaggerResponse] annotations on controllers
        options.EnableAnnotations();

        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Billing & POS SaaS API",
            Version = "v1",
            Description = "Multi-tenant billing, POS, inventory and shop management SaaS API.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Billing System",
                Email = "support@billingsystem.local"
            }
        });

        // JWT Bearer authentication scheme for Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your JWT token.\r\n\r\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── CORS for the React SPA ──
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BillingCors", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ── Forwarded headers (for reverse proxy / load balancer) ──
    // Configured in the middleware pipeline below via UseForwardedHeaders.

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // ── Middleware pipeline (order matters) ──
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseCors("BillingCors");
    app.UseResponseCompression();

    // Enable Swagger in all environments for testing & documentation
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing & POS API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseAuthentication();
    // TenantContextMiddleware must run AFTER authentication so that the JWT
    // claims (tenant_id, user_id, shop_id) are already populated on
    // HttpContext.User and can be resolved into the scoped ITenantContext.
    app.UseMiddleware<TenantContextMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Partial class so the generated <c>Program</c> declaration is accessible for
/// integration tests (WebApplicationFactory<Program>).
/// </summary>
public partial class Program { }
