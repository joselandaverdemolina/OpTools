namespace AltaDescargaLCO.AzureIntegration;

public sealed class HttpAvailabilityProbe : IHttpAvailabilityProbe
{
    private readonly HttpClient _httpClient;

    public HttpAvailabilityProbe(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthProbeResult> CheckAsync(
        Uri url,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var perRequestTimeout = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, perRequestTimeout.Token);

        try
        {
            using var response = await _httpClient.GetAsync(url, linked.Token);
            var detail = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            return new HealthProbeResult(response.IsSuccessStatusCode, detail);
        }
        // El filtro distingue "se vencio mi timeout" de "el llamador cancelo": lo segundo se propaga.
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthProbeResult(false, $"sin respuesta en {timeout.TotalSeconds:0} s");
        }
        catch (HttpRequestException ex)
        {
            return new HealthProbeResult(false, ex.Message);
        }
    }
}
