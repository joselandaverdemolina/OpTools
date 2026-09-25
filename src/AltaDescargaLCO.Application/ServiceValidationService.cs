using AltaDescargaLCO.AzureIntegration;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.Application;

public sealed class ServiceValidationService : IServiceValidationService
{
    private readonly IHttpAvailabilityProbe _availabilityProbe;
    private readonly IErrorRateProbe _errorRateProbe;
    private readonly MonitoringSettings _settings;

    public ServiceValidationService(
        IHttpAvailabilityProbe availabilityProbe,
        IErrorRateProbe errorRateProbe,
        IOptions<MonitoringSettings> settings)
    {
        _availabilityProbe = availabilityProbe;
        _errorRateProbe = errorRateProbe;
        _settings = settings.Value;
    }

    public static ServiceAvailabilityStatus Classify(
        double successPercent,
        double healthyThresholdPercent,
        double downThresholdPercent)
    {
        if (successPercent >= healthyThresholdPercent)
        {
            return ServiceAvailabilityStatus.Healthy;
        }

        return successPercent <= downThresholdPercent
            ? ServiceAvailabilityStatus.Down
            : ServiceAvailabilityStatus.Intermittent;
    }

    public async Task<ValidationReport> ExecuteAsync(
        IProgress<ValidationProgress> progress,
        CancellationToken cancellationToken = default)
    {
        // Las dos validaciones corren siempre: la segunda no está condicionada al resultado de la
        // primera, para que el operador vea los dos veredictos en un solo clic.
        var availability = await CheckAvailabilityAsync(progress, cancellationToken);
        var errorRate = await CheckErrorRateAsync(progress, cancellationToken);
        return new ValidationReport(availability, errorRate);
    }

    private async Task<ValidationOutcome> CheckAvailabilityAsync(
        IProgress<ValidationProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ServiceUrl))
            {
                throw new InvalidOperationException(
                    "La configuración Monitoring:ServiceUrl es obligatoria en appsettings.json.");
            }

            if (_settings.ServiceCheckCount < 1)
            {
                throw new InvalidOperationException(
                    "La configuración Monitoring:ServiceCheckCount debe ser mayor que cero.");
            }

            var url = new Uri(_settings.ServiceUrl);
            var timeout = TimeSpan.FromSeconds(_settings.ServiceRequestTimeoutSeconds);
            var total = _settings.ServiceCheckCount;

            Report(progress, ValidationStage.Availability,
                $"Verificando la disponibilidad de {url} ({total} verificaciones)...");

            var successful = 0;
            for (var attempt = 1; attempt <= total; attempt++)
            {
                var check = await _availabilityProbe.CheckAsync(url, timeout, cancellationToken);
                if (check.IsHealthy)
                {
                    successful++;
                }

                Report(progress, ValidationStage.Availability,
                    $"Verificación {attempt} de {total}: {check.Detail}",
                    check.IsHealthy ? PipelineSeverity.Info : PipelineSeverity.Error);
            }

            var percent = 100.0 * successful / total;
            var status = Classify(
                percent, _settings.HealthyThresholdPercent, _settings.DownThresholdPercent);

            var summary = status switch
            {
                ServiceAvailabilityStatus.Healthy => "Servicio saludable",
                ServiceAvailabilityStatus.Intermittent => "Intermitencia en servicio OCSP",
                _ => "Caída del servicio OCSP"
            };

            var detail = $"{summary} ({percent:0.#}% de efectividad: {successful} de {total} "
                + "verificaciones exitosas).";
            var passed = status == ServiceAvailabilityStatus.Healthy;

            Report(progress, ValidationStage.Availability, detail,
                passed ? PipelineSeverity.Success : PipelineSeverity.Error);

            return new ValidationOutcome(passed, detail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Failure(progress, ValidationStage.Availability, ExceptionDescription.Describe(ex));
        }
    }

    private async Task<ValidationOutcome> CheckErrorRateAsync(
        IProgress<ValidationProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            Report(progress, ValidationStage.ErrorRate,
                $"Consultando el porcentaje de errores del componente '{_settings.ComponentRoleName}' "
                + $"en los últimos {_settings.ErrorWindowMinutes} minutos...");

            var measurement = await _errorRateProbe.MeasureAsync(cancellationToken);

            if (measurement.TotalRequests == 0)
            {
                // Sin solicitudes observadas no hay porcentaje que evaluar; pasar en verde aquí
                // haría que "no hay datos" se leyera como "todo bien".
                return Failure(progress, ValidationStage.ErrorRate,
                    $"No hubo solicitudes en los últimos {_settings.ErrorWindowMinutes} minutos, "
                    + "así que no es posible calcular el porcentaje de errores.");
            }

            var counts = $"{measurement.TotalExceptions} excepciones / "
                + $"{measurement.TotalRequests} solicitudes";

            if (measurement.ErrorPercent > _settings.ErrorThresholdPercent)
            {
                return Failure(progress, ValidationStage.ErrorRate,
                    $"El porcentaje de errores es {measurement.ErrorPercent:0.##}% ({counts}), "
                    + $"por encima del umbral de {_settings.ErrorThresholdPercent:0.##}%.");
            }

            var detail = $"El porcentaje de errores es {measurement.ErrorPercent:0.##}% ({counts}), "
                + $"dentro del umbral de {_settings.ErrorThresholdPercent:0.##}%.";
            Report(progress, ValidationStage.ErrorRate, detail, PipelineSeverity.Success);
            return new ValidationOutcome(true, detail);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Failure(progress, ValidationStage.ErrorRate, ExceptionDescription.Describe(ex));
        }
    }

    private static void Report(
        IProgress<ValidationProgress> progress,
        ValidationStage stage,
        string message,
        PipelineSeverity severity = PipelineSeverity.Info) =>
        progress.Report(new ValidationProgress(stage, message, severity));

    private static ValidationOutcome Failure(
        IProgress<ValidationProgress> progress,
        ValidationStage stage,
        string detail)
    {
        Report(progress, stage, detail, PipelineSeverity.Error);
        return new ValidationOutcome(false, detail);
    }
}
