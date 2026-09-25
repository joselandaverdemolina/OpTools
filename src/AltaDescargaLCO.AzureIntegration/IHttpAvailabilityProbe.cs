namespace AltaDescargaLCO.AzureIntegration;

/// <summary>
/// Una verificación HTTP contra una URL cualquiera. No usa credenciales de Azure.
/// </summary>
public interface IHttpAvailabilityProbe
{
    Task<HealthProbeResult> CheckAsync(Uri url, TimeSpan timeout, CancellationToken cancellationToken = default);
}
