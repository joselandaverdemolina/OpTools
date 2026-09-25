namespace AltaDescargaLCO.Application;

public interface IPacProvisioningService
{
    /// <summary>
    /// Validates the RFC, applies the change in SQL, restarts the Azure web app and verifies it
    /// answers again, reporting every stage through <paramref name="progress"/>.
    /// </summary>
    Task<PipelineResult> ExecuteAsync(
        PacActionRequest request,
        IProgress<PipelineProgress> progress,
        CancellationToken cancellationToken = default);
}
