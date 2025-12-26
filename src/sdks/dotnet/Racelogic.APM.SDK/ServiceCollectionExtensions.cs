using Microsoft.Extensions.DependencyInjection;

namespace Racelogic.APM;

/// <summary>
/// Extension methods for registering APM services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds APM telemetry services to the service collection.
    /// </summary>
    public static IServiceCollection AddApmTelemetry(
        this IServiceCollection services,
        Action<ApmOptions> configure)
    {
        services.Configure(configure);

        services.AddHttpClient("ApmClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IApmLogger, ApmLogger>();
        services.AddSingleton<IApmMetrics, ApmMetrics>();

        return services;
    }

    /// <summary>
    /// Adds APM telemetry services using configuration section.
    /// </summary>
    public static IServiceCollection AddApmTelemetry(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<ApmOptions>(configuration.GetSection("Apm"));

        services.AddHttpClient("ApmClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IApmLogger, ApmLogger>();
        services.AddSingleton<IApmMetrics, ApmMetrics>();

        return services;
    }
}
