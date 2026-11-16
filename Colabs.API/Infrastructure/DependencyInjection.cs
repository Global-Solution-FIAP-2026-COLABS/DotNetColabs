using Colabs.Data;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Infrastructure;

/// <summary>
/// Configuração centralizada da infraestrutura da aplicação.
/// Por enquanto: persistência (Oracle/EF Core). Health Checks e OpenTelemetry entram nos próximos passos.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OracleConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseOracle(connectionString,
                b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

        return services;
    }
}