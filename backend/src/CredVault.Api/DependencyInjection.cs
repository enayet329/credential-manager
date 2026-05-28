using System.Text;
using CredVault.Api.Auth;
using CredVault.Api.Endpoints;
using CredVault.Api.Filters;
using CredVault.Api.Lookups;
using CredVault.Api.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CredVault.Api;

/// <summary>Registers API-layer services and wires up Minimal API endpoints.</summary>
public static class DependencyInjection
{
    /// <summary>Adds auth, filters, lookups, and the schema seeder hosted service.</summary>
    public static IServiceCollection AddCredVaultApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<StepUpTokenService>();
        services.AddSingleton<AccessTokenService>();
        services.AddSingleton<ShareTokenService>();

        // SMTP email when configured; otherwise the dev logging stub.
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName));
        var smtpHost = configuration.GetValue<string>($"{SmtpOptions.SectionName}:Host");
        if (!string.IsNullOrWhiteSpace(smtpHost))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<SlugLookup>();
        services.AddScoped<AuditHookFilter>();
        services.AddSingleton<ResponseSafetyNetFilter>();

        services.AddSingleton<IAuthorizationHandler, StepUpAuthorizationHandler>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, StepUpAwareResultHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Defer JwtBearerOptions configuration until the host is fully built so
        // WebApplicationFactory's IConfiguration overrides have applied.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((jwtBearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                jwtBearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthConstants.StepUpPolicy, p => p.RequireAuthenticatedUser().AddRequirements(new StepUpRequirement()));

            foreach (var permission in new[]
            {
                Permissions.ReadMetadata, Permissions.ReadValue, Permissions.WriteCredentials,
                Permissions.WriteSuppliers, Permissions.WriteProjects, Permissions.AdminSchemas,
            })
            {
                options.AddPolicy(permission, p => p.RequireAuthenticatedUser().RequireClaim(AuthConstants.PermissionsClaim, permission));
            }
        });

        services.AddHostedService<CredentialSchemaSeeder>();

        // CORS — reads "Cors:AllowedOrigins" (string[]) from configuration. Defaults to
        // the local Next.js dev origin so the browser-based frontend can reach the API.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("X-Step-Up")
                      .AllowCredentials());
        });

        return services;
    }

    /// <summary>Registers every endpoint group.</summary>
    public static IEndpointRouteBuilder MapCredVaultEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapLoginEndpoints();
        routes.MapAccountEndpoints();
        routes.MapStepUpEndpoints();
        routes.MapMemberEndpoints();
        routes.MapShareEndpoints();
        routes.MapCredentialExportEndpoint();
        routes.MapSchemaEndpoints();
        routes.MapAdminSchemaEndpoints();
        routes.MapProjectEndpoints();
        routes.MapEnvironmentEndpoints();
        routes.MapSupplierEndpoints();
        routes.MapCredentialEndpoints();
        routes.MapCredentialNoteEndpoints();
        return routes;
    }
}
