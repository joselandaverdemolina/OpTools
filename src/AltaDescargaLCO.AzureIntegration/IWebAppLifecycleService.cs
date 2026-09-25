namespace AltaDescargaLCO.AzureIntegration;

/// <summary>
/// The only Azure operations this tool is allowed to perform: restart the one configured web app
/// and read back its runtime state. No member hands out a credential, token or ARM client, so the
/// interactive sign-in cannot be reused for any other Azure activity.
/// </summary>
public interface IWebAppLifecycleService
{
    Task RestartWebAppAsync(CancellationToken cancellationToken = default);

    Task<WebAppRuntimeState> GetWebAppStateAsync(CancellationToken cancellationToken = default);
}
