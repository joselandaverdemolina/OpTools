namespace AltaDescargaLCO.AzureIntegration;

public sealed record ErrorRateMeasurement(long TotalRequests, long TotalExceptions, double ErrorPercent);

/// <summary>
/// Única operación permitida contra Application Insights: leer el conteo de excepciones y de
/// solicitudes del componente configurado. Ningún miembro entrega credencial, token ni cliente, así
/// que el inicio de sesión no puede reutilizarse para otra consulta ni para otro recurso.
/// </summary>
public interface IErrorRateProbe
{
    Task<ErrorRateMeasurement> MeasureAsync(CancellationToken cancellationToken = default);
}
