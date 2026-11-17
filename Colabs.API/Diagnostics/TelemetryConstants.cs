namespace Colabs.Diagnostics;

/// <summary>
/// Nomes usados para identificar as fontes de tracing (ActivitySource) e de métricas (Meter)
/// customizadas da aplicação junto ao OpenTelemetry.
/// </summary>
public static class TelemetryConstants
{
    public const string ServiceName = "Colabs.Api";
    public const string MeterName = "Colabs.Metrics";
}