namespace AltaDescargaLCO.Infrastructure.Settings;

public sealed class AzureSettings
{
    public const string SectionName = "Azure";

    public string SubscriptionId { get; set; } = string.Empty;

    public string ResourceGroupName { get; set; } = string.Empty;

    public string WebAppName { get; set; } = string.Empty;

     public string TenantId { get; set; } = string.Empty;

    public string HealthCheckUrl { get; set; } = string.Empty;

    public int HealthCheckPollIntervalSeconds { get; set; } = 5;

    public int HealthCheckTimeoutSeconds { get; set; } = 120;

    public int HealthCheckRequestTimeoutSeconds { get; set; } = 10;
}
