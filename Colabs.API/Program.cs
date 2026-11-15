using Colabs.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// Configuração do Serilog (log estruturado)
// Console: acompanhamento em tempo real. File: histórico em disco, com rotação diária.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/colabs-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando a Colabs API...");

    var builder = WebApplication.CreateBuilder(args);

    // Substitui o logger padrão do ASP.NET Core pelo Serilog
    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Colabs API",
            Version = "v1",
            Description = "Plataforma comunitária que conecta necessidades reais a quem pode ajudar."
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Health check provisório (liveness). Será completado no passo 12, com o check do Oracle.
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Precisa vir antes do request logging para que o CorrelationId já esteja no contexto de log.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Loga uma linha estruturada por requisição concluída (método, path, status, duração).
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    // Authentication SEMPRE antes de Authorization (a autenticação JWT é adicionada no passo 4).
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Liveness: a aplicação está de pé? (não checa dependências externas)
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação Colabs falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para o WebApplicationFactory<Program> dos testes de integração:
// com top-level statements, a classe Program gerada é 'internal' por padrão.
// Este partial a torna 'public', permitindo o acesso pelo assembly de testes.
public partial class Program { }