using Colabs.Data;
using Colabs.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Colabs.Infrastructure;

/// <summary>
/// Configuração centralizada da infraestrutura da aplicação:
/// persistência (Oracle/EF Core) e Observabilidade (OpenTelemetry).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OracleConnection");

        // 1. Configuração do DbContext (Oracle)
        services.AddDbContext<AppDbContext>(options =>
            options.UseOracle(connectionString,
                b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

        // 2. Configuração do OpenTelemetry (Tracing + Métricas)
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(TelemetryConstants.ServiceName);

        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation() // Captura requisições HTTP de entrada (spans automáticos por endpoint)
                    .AddHttpClientInstrumentation() // Captura chamadas HTTP de saída
                    .AddSource(TelemetryConstants.ServiceName) // Assina os spans customizados criados nos Services
                    .AddConsoleExporter(); // Debug local; trocar por AddOtlpExporter() para enviar a um coletor real
            })
            .WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    // Métricas padrão do ASP.NET Core: tempo de resposta e contagem de requisições por status code.
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(TelemetryConstants.MeterName) // Assina as métricas de negócio customizadas
                    .AddConsoleExporter();
            });

        return services;
    }
}