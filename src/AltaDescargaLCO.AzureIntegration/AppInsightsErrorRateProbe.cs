using AltaDescargaLCO.Infrastructure.Settings;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query.Logs;
using Azure.Monitor.Query.Logs.Models;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.AzureIntegration;

public sealed class AppInsightsErrorRateProbe : IErrorRateProbe
{
    // Esquema clásico de Application Insights. La ventana de tiempo no se filtra aquí: la impone
    // LogsQueryTimeRange, para no tener dos fuentes de verdad.
    private const string ClassicQuery = """
        let roleName = "{0}";
        let reqs = toscalar(requests   | where cloud_RoleName =~ roleName | summarize sum(itemCount));
        let excs = toscalar(exceptions | where cloud_RoleName =~ roleName | summarize sum(itemCount));
        print
            TotalRequests   = coalesce(reqs, long(0)),
            TotalExceptions = coalesce(excs, long(0)),
            ErrorPercent    = iff(coalesce(reqs, long(0)) == 0, real(0),
                                  round(100.0 * todouble(coalesce(excs, long(0)))
                                              / todouble(coalesce(reqs, long(0))), 4))
        """;

    // Mismo cálculo con los nombres del esquema de Log Analytics, para los recursos donde el
    // esquema clásico no resuelve.
    private const string WorkspaceQuery = """
        let roleName = "{0}";
        let reqs = toscalar(AppRequests   | where AppRoleName =~ roleName | summarize sum(ItemCount));
        let excs = toscalar(AppExceptions | where AppRoleName =~ roleName | summarize sum(ItemCount));
        print
            TotalRequests   = coalesce(reqs, long(0)),
            TotalExceptions = coalesce(excs, long(0)),
            ErrorPercent    = iff(coalesce(reqs, long(0)) == 0, real(0),
                                  round(100.0 * todouble(coalesce(excs, long(0)))
                                              / todouble(coalesce(reqs, long(0))), 4))
        """;

    private readonly MonitoringSettings _settings;
    private readonly AzureSettings _azureSettings;

    public AppInsightsErrorRateProbe(
        IOptions<MonitoringSettings> settings,
        IOptions<AzureSettings> azureSettings)
    {
        _settings = settings.Value;
        _azureSettings = azureSettings.Value;
    }

    public async Task<ErrorRateMeasurement> MeasureAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AppInsightsResourceId)
            || string.IsNullOrWhiteSpace(_settings.ComponentRoleName))
        {
            throw new InvalidOperationException(
                "Las configuraciones Monitoring:AppInsightsResourceId y Monitoring:ComponentRoleName "
                + "son obligatorias en appsettings.json.");
        }

        var resourceId = new ResourceIdentifier(_settings.AppInsightsResourceId);
        var timeRange = new LogsQueryTimeRange(TimeSpan.FromMinutes(_settings.ErrorWindowMinutes));
        var roleName = _settings.ComponentRoleName.Replace("\\", "\\\\").Replace("\"", "\\\"");

        using var queryTimeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(_settings.QueryTimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, queryTimeout.Token);

        // El cliente y la credencial se construyen aquí y se desechan al salir: el token vive sólo
        // durante la consulta y no queda disponible para nada más.
        var client = new LogsQueryClient(CreateCredential());

        try
        {
            return await RunAsync(client, resourceId, string.Format(ClassicQuery, roleName), timeRange, linked.Token);
        }
        catch (RequestFailedException ex) when (ex.Status == 400)
        {
            // 400 significa que la consulta no encaja con el esquema de este recurso; se reintenta
            // con los nombres de Log Analytics. Un 403 de permisos no entra aquí y se propaga.
            return await RunAsync(client, resourceId, string.Format(WorkspaceQuery, roleName), timeRange, linked.Token);
        }
    }

    private static async Task<ErrorRateMeasurement> RunAsync(
        LogsQueryClient client,
        ResourceIdentifier resourceId,
        string query,
        LogsQueryTimeRange timeRange,
        CancellationToken cancellationToken)
    {
        var response = await client.QueryResourceAsync(
            resourceId, query, timeRange, cancellationToken: cancellationToken);

        var result = response.Value;
        if (result.Status == LogsQueryResultStatus.Failure)
        {
            throw new InvalidOperationException(
                $"Application Insights rechazó la consulta: {result.Error?.Message}");
        }

        var rows = result.Table?.Rows;
        if (rows is null || rows.Count == 0)
        {
            throw new InvalidOperationException(
                "Application Insights no devolvió resultados para la consulta de errores.");
        }

        var row = rows[0];
        return new ErrorRateMeasurement(
            row.GetInt64("TotalRequests") ?? 0,
            row.GetInt64("TotalExceptions") ?? 0,
            row.GetDouble("ErrorPercent") ?? 0);
    }

    private TokenCredential CreateCredential()
    {
        var options = new InteractiveBrowserCredentialOptions();
        if (!string.IsNullOrWhiteSpace(_azureSettings.TenantId))
        {
            options.TenantId = _azureSettings.TenantId;
        }

        // TokenCachePersistenceOptions se deja sin asignar a propósito: el token vive sólo en este
        // proceso, no se escribe a disco y no sobrevive a la operación.
        return new InteractiveBrowserCredential(options);
    }
}
