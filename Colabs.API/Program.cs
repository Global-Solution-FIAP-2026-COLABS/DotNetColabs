using Colabs.Infrastructure;
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

    // Infraestrutura: DbContext (Oracle) + OpenTelemetry
    builder.Services.AddInfrastructure(builder.Configuration);

    // Tratamento global de exceções (IExceptionHandler, .NET 8)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ProblemDetails (RFC 7807) para todas as respostas de erro, com o CorrelationId da requisição.
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            if (context.HttpContext.Items["CorrelationId"] is string correlationId)
            {
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
            }
        });

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

    // Precisa vir antes do exception handler e do request logging para que o
    // CorrelationId já esteja disponível no contexto de log em ambos os casos.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Última linha de defesa: captura qualquer exceção não tratada, loga e devolve ProblemDetails.
    app.UseExceptionHandler();

    // Respostas 4xx/5xx sem corpo (401, 403, 404, 429...) também saem como ProblemDetails.
    app.UseStatusCodePages();

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
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException é lançada de propósito pelas ferramentas do EF (dotnet ef) e não é erro.
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