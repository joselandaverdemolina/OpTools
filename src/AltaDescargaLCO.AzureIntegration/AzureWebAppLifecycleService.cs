using AltaDescargaLCO.Infrastructure.Settings;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.AzureIntegration;

public sealed class AzureWebAppLifecycleService : IWebAppLifecycleService
{
    private readonly AzureSettings _settings;

    public AzureWebAppLifecycleService(IOptions<AzureSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task RestartWebAppAsync(CancellationToken cancellationToken = default)
    {
        var site = GetSite();
        await site.RestartAsync(softRestart: false, synchronous: true, cancellationToken: cancellationToken);
    }

    public async Task<WebAppRuntimeState> GetWebAppStateAsync(CancellationToken cancellationToken = default)
    {
        var site = await GetSite().GetAsync(cancellationToken);
        return site.Value.Data.State switch
        {
            "Running" => WebAppRuntimeState.Running,
            "Stopped" => WebAppRuntimeState.Stopped,
            _ => WebAppRuntimeState.Unknown
        };
    }

    /// <summary>
    /// Builds a throwaway credential and ARM client addressed straight at the configured web app.
    /// Both are local to the call and discarded on return, and the resource id is composed from
    /// settings rather than discovered, so no other subscription or resource is ever touched.
    /// </summary>
    private WebSiteResource GetSite()
    {
        if (string.IsNullOrWhiteSpace(_settings.SubscriptionId)
            || string.IsNullOrWhiteSpace(_settings.ResourceGroupName)
            || string.IsNullOrWhiteSpace(_settings.WebAppName))
        {
            throw new InvalidOperationException(
                "Las configuraciones Azure:SubscriptionId, Azure:ResourceGroupName y Azure:WebAppName son "
                + "obligatorias en appsettings.json.");
        }

        var armClient = new ArmClient(CreateCredential());
        var siteId = WebSiteResource.CreateResourceIdentifier(
            _settings.SubscriptionId, _settings.ResourceGroupName, _settings.WebAppName);
        return armClient.GetWebSiteResource(siteId);
    }

    private TokenCredential CreateCredential()
    {
        var options = new InteractiveBrowserCredentialOptions();
        if (!string.IsNullOrWhiteSpace(_settings.TenantId))
        {
            options.TenantId = _settings.TenantId;
        }

        // TokenCachePersistenceOptions is deliberately left unset: the token then lives only in this
        // process, so nothing is written to disk and nothing outlives the operation.
        return new InteractiveBrowserCredential(options);
    }
}
