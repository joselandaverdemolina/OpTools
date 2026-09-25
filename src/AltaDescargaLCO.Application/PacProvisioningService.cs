using AltaDescargaLCO.AzureIntegration;
using AltaDescargaLCO.DataAccess;
using AltaDescargaLCO.Domain;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.Application;

public sealed class PacProvisioningService : IPacProvisioningService
{
    private readonly IPacRepository _repository;
    private readonly IWebAppLifecycleService _webApp;
    private readonly IWebAppHealthProbe _healthProbe;
    private readonly AzureSettings _azureSettings;

    public PacProvisioningService(
        IPacRepository repository,
        IWebAppLifecycleService webApp,
        IWebAppHealthProbe healthProbe,
        IOptions<AzureSettings> azureSettings)
    {
        _repository = repository;
        _webApp = webApp;
        _healthProbe = healthProbe;
        _azureSettings = azureSettings.Value;
    }

    public async Task<PipelineResult> ExecuteAsync(
        PacActionRequest request,
        IProgress<PipelineProgress> progress,
        CancellationToken cancellationToken = default)
    {
        Report(progress, PipelineStage.Validation, "Validando los datos capturados...");

        if (!Rfc.TryParse(request.RawRfc, out var rfc, out var validationError))
        {
            return Fail(progress, PipelineStage.Validation, validationError!);
        }

        var persona = rfc!.Type == PersonaType.Moral ? "persona moral" : "persona física";
        Report(progress, PipelineStage.Validation,
            $"El RFC {rfc.Value} es válido ({persona}).",
            PipelineSeverity.Success);

        var databaseOutcome = await UpdateDatabaseAsync(request.Action, rfc, progress, cancellationToken);
        if (databaseOutcome is not null)
        {
            return databaseOutcome;
        }

        var restartOutcome = await RestartWebAppAsync(progress, cancellationToken);
        if (restartOutcome is not null)
        {
            return restartOutcome;
        }

        return await VerifyWebAppAsync(progress, cancellationToken);
    }

    /// <returns>null when the pipeline should continue, otherwise the failing result.</returns>
    private async Task<PipelineResult?> UpdateDatabaseAsync(
        PacAction action,
        Rfc rfc,
        IProgress<PipelineProgress> progress,
        CancellationToken cancellationToken)
    {
        Report(progress, PipelineStage.Database, "Actualizando la base de datos...");

        try
        {
            var outcome = await _repository.ApplyAsync(action, rfc, cancellationToken);

            
            var (message, severity) = (action, outcome) switch
            {
                (PacAction.Add, PacChangeOutcome.Applied) =>
                    ($"Se dio de alta el RFC {rfc.Value} con Estado = 1.", PipelineSeverity.Success),
                (PacAction.Add, _) =>
                    ($"El RFC {rfc.Value} ya estaba registrado, no se insertó ningún registro.",
                        PipelineSeverity.Info),
                (PacAction.Remove, PacChangeOutcome.Applied) =>
                    ($"Se eliminó el RFC {rfc.Value}.", PipelineSeverity.Success),
                _ =>
                    ($"No se encontró el RFC {rfc.Value}, no se eliminó ningún registro.",
                        PipelineSeverity.Info)
            };

            Report(progress, PipelineStage.Database, message, severity);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Fail(progress, PipelineStage.Database, Describe(ex));
        }

        return null;
    }

    private async Task<PipelineResult?> RestartWebAppAsync(
        IProgress<PipelineProgress> progress,
        CancellationToken cancellationToken)
    {
        Report(progress, PipelineStage.AzureRestart,
            $"Reiniciando servicios (web app de Azure '{_azureSettings.WebAppName}'). "
            + "Puede aparecer una ventana para iniciar sesión...");

        try
        {
            await _webApp.RestartWebAppAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Fail(progress, PipelineStage.AzureRestart, Describe(ex));
        }

        Report(progress, PipelineStage.AzureRestart, "Azure aceptó la solicitud de reinicio.",
            PipelineSeverity.Success);
        return null;
    }

    private async Task<PipelineResult> VerifyWebAppAsync(
        IProgress<PipelineProgress> progress,
        CancellationToken cancellationToken)
    {
        Report(progress, PipelineStage.HealthCheck,
            $"Verificando que la web app responda en {_azureSettings.HealthCheckUrl} ...");

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _azureSettings.HealthCheckPollIntervalSeconds));
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(_azureSettings.HealthCheckTimeoutSeconds);

        try
        {
            while (true)
            {
                var probe = await _healthProbe.PingAsync(cancellationToken);
                if (probe.IsHealthy)
                {
                    Report(progress, PipelineStage.HealthCheck,
                        $"La web app está respondiendo ({probe.Detail}).", PipelineSeverity.Success);
                    return PipelineResult.Ok();
                }

                if (DateTimeOffset.UtcNow + pollInterval >= deadline)
                {
                    var state = await _webApp.GetWebAppStateAsync(cancellationToken);
                    return Fail(progress, PipelineStage.HealthCheck,
                        $"La web app no respondió correctamente en "
                        + $"{_azureSettings.HealthCheckTimeoutSeconds} s. Última verificación: {probe.Detail}. "
                        + $"Azure reporta la aplicación como {DescribeState(state)}.");
                }

                Report(progress, PipelineStage.HealthCheck,
                    $"Aún no responde ({probe.Detail}). Nuevo intento en {pollInterval.TotalSeconds:0} s...");
                await Task.Delay(pollInterval, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Fail(progress, PipelineStage.HealthCheck, Describe(ex));
        }
    }

    private static void Report(
        IProgress<PipelineProgress> progress,
        PipelineStage stage,
        string message,
        PipelineSeverity severity = PipelineSeverity.Info) =>
        progress.Report(new PipelineProgress(stage, message, severity));

    private static PipelineResult Fail(
        IProgress<PipelineProgress> progress,
        PipelineStage stage,
        string message)
    {
        Report(progress, stage, message, PipelineSeverity.Error);
        return PipelineResult.Failed(stage, message);
    }

    private static string DescribeState(WebAppRuntimeState state) => state switch
    {
        WebAppRuntimeState.Running => "en ejecución",
        WebAppRuntimeState.Stopped => "detenida",
        _ => "en un estado desconocido"
    };

    private static string Describe(Exception ex) => ExceptionDescription.Describe(ex);
}
