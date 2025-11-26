// ==========================================================================
// T034: ServiceCollectionExtensions - DI Registration for SCIM Gateway
// ==========================================================================
// Extension methods to register all SCIM Gateway services
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Authentication;
using SCIMGateway.Core.Data;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Filtering;
using SCIMGateway.Core.Repositories;
using SCIMGateway.Core.Validation;

namespace SCIMGateway.Core.Configuration;

/// <summary>
/// Extension methods for registering SCIM Gateway services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all SCIM Gateway core services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScimGatewayCore(this IServiceCollection services)
    {
        // Data layer
        services.AddSingleton<ICosmosDbClient, CosmosDbClient>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IGroupRepository, GroupRepository>(); // To be added

        // Validation
        services.AddSingleton<ISchemaValidator, SchemaValidator>();

        // Filtering
        services.AddSingleton<IFilterParser, FilterParser>();

        // Error handling
        services.AddSingleton<IErrorHandler, ErrorHandler>();

        // Auditing
        services.AddSingleton<IAuditLogger, AuditLogger>();

        // Authentication
        services.AddScoped<IBearerTokenValidator, BearerTokenValidator>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddSingleton<IRateLimiter, RateLimiter>();

        return services;
    }

    /// <summary>
    /// Adds SCIM Gateway with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScimGatewayCore(
        this IServiceCollection services,
        Action<ScimGatewayOptions> configure)
    {
        services.Configure(configure);
        return services.AddScimGatewayCore();
    }
}
