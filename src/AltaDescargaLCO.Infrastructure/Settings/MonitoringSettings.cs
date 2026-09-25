namespace AltaDescargaLCO.Infrastructure.Settings;

public sealed class MonitoringSettings
{
    public const string SectionName = "Monitoring";

    public string ServiceUrl { get; set; } = string.Empty;

    public int ServiceCheckCount { get; set; } = 5;

    public int ServiceRequestTimeoutSeconds { get; set; } = 10;

    public double HealthyThresholdPercent { get; set; } = 80.0;

    public double DownThresholdPercent { get; set; } = 20.0;

    /// <summary>Id ARM completo del recurso de Application Insights.</summary>
    public string AppInsightsResourceId { get; set; } = string.Empty;

    /// <summary>Valor de cloud_RoleName del componente que se va a medir.</summary>
    public string ComponentRoleName { get; set; } = string.Empty;

    public int ErrorWindowMinutes { get; set; } = 30;

    public double ErrorThresholdPercent { get; set; } = 5.0;

    public int QueryTimeoutSeconds { get; set; } = 60;
}
