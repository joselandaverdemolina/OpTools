namespace AltaDescargaLCO.AzureIntegration;

/// <summary>
/// Sondeo HTTP de la web app configurada en Azure:HealthCheckUrl.
/// </summary>
public interface IWebAppHealthProbe
{
    Task<HealthProbeResult> PingAsync(CancellationToken cancellationToken = default);
}
