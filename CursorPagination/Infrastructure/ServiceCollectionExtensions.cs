using CursorPagination.Domain.Abstractions;
using CursorPagination.Persistence;
using CursorPagination.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CursorPagination.Infrastructure;

internal static class ServiceCollectionExtensions
{
    internal static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        InitializePersistence(services, configuration);
        InitializeMediatR(services);
        InitializeOpenTelemetry(services);
    }

    private static void InitializePersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );
        
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void InitializeMediatR(this IServiceCollection services)
    {
        services.AddMediatR(conf =>
        {
            conf.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });
        
    }

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
}