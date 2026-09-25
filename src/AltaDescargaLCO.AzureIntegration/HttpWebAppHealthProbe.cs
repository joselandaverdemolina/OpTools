using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.AzureIntegration;

public sealed class HttpWebAppHealthProbe : IWebAppHealthProbe
{
    private readonly IHttpAvailabilityProbe _probe;
    private readonly AzureSettings _settings;

    public HttpWebAppHealthProbe(IHttpAvailabilityProbe probe, IOptions<AzureSettings> settings)
    {
        _probe = probe;
        _settings = settings.Value;
    }

    public Task<HealthProbeResult> PingAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.HealthCheckUrl))
        {
            throw new InvalidOperationException(
                "La configuración Azure:HealthCheckUrl es obligatoria en appsettings.json.");
        }

        return _probe.CheckAsync(
            new Uri(_settings.HealthCheckUrl),
            TimeSpan.FromSeconds(_settings.HealthCheckRequestTimeoutSeconds),
            cancellationToken);
    }
}
