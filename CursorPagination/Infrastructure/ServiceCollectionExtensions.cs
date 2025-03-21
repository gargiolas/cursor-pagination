using CursorPagination.Application.Abstractions.Services;
using CursorPagination.Domain.Abstractions;
using CursorPagination.Infrastructure.Services;
using CursorPagination.Persistence;
using CursorPagination.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CursorPagination.Infrastructure;

/// <summary>
/// Provides extension methods for registering application services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which the services will be added.</param>
    /// <param name="configuration">The application configuration used to resolve required settings.</param>
    internal static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        InitializePersistence(services, configuration);
        InitializeMediatR(services);
        InitializeOpenTelemetry(services);
        InitializeServices(services);
    }

    /// <summary>
    /// Configures and initializes the persistence layer for the application, including the database context and repository patterns.
    /// </summary>
    /// <param name="services">The service collection to which persistence-related services will be added.</param>
    /// <param name="configuration">The configuration instance to retrieve connection strings and other settings.</param>
    private static void InitializePersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.EnableSensitiveDataLogging(true);
            options.LogTo(Console.WriteLine);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// Configures and initializes MediatR for the application.
    /// <param name="services">The IServiceCollection to add MediatR services to.</param>
    private static void InitializeMediatR(this IServiceCollection services)
    {
        services.AddMediatR(conf =>
        {
            conf.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });
    }

    /// <summary>
    /// Configures and initializes OpenTelemetry for tracing and metrics in the application.
    /// </summary>
    /// <param name="services">The service collection to which OpenTelemetry services and configurations will be added.</param>
    private static void InitializeOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("CursorPagination"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddMeter(
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "System.Net.Http");
            })
            .UseOtlpExporter();
    }

    /// <summary>
    /// Initializes and registers services in the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to which the services are added.</param>
    private static void InitializeServices(IServiceCollection services)
    {
        services.AddScoped<ICursorService, CursorService>();
    }
}