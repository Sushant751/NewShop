using Billing.Identity.Authorization;
using Billing.Identity.Options;
using Billing.Identity.Passwords;
using Billing.Identity.Services;
using Billing.Identity.Tokens;
using Billing.Shared.Enums;
using Billing.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Billing.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IAuthService, AuthService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // set true in production behind TLS
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
        });

        services.AddAuthorization(options =>
        {
            // Register a policy for every known permission. Policies are keyed by the
            // permission's member name (e.g. "ReportsView") so that controllers can
            // reference them via nameof(Permissions.ReportsView) inside [Authorize(Policy=...)]
            // attributes, whose arguments must be compile-time constants. The requirement
            // carries the dot-notation value (e.g. "Reports.View") that matches the permission
            // claims embedded in the JWT and checked by PermissionHandler.
            const BindingFlags bindingFlags =
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            foreach (var field in typeof(Permissions).GetFields(bindingFlags)
                         .Where(f => f.IsLiteral && f.FieldType == typeof(string)))
            {
                var policyName = field.Name;                                // "ReportsView"
                var permissionValue = (string)field.GetRawConstantValue()!; // "Reports.View"
                options.AddPolicy(policyName,
                    policy => policy.Requirements.Add(new PermissionRequirement(permissionValue)));
            }

            // Register role-based policies, keyed by role member name (e.g. "GlobalAdmin").
            foreach (var field in typeof(Roles).GetFields(bindingFlags)
                         .Where(f => f.IsLiteral && f.FieldType == typeof(string)))
            {
                var roleName = (string)field.GetRawConstantValue()!;
                options.AddPolicy(field.Name,
                    policy => policy.Requirements.Add(new RoleRequirement(roleName)));
            }
        });

        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleHandler>();

        return services;
    }
}
