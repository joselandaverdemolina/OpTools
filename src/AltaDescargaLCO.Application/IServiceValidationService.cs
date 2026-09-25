namespace AltaDescargaLCO.Application;

public interface IServiceValidationService
{
    /// <summary>
    /// Verifica la disponibilidad del servicio y el porcentaje de errores del componente. Las dos
    /// validaciones se ejecutan siempre, aunque la primera falle.
    /// </summary>
    Task<ValidationReport> ExecuteAsync(
        IProgress<ValidationProgress> progress,
        CancellationToken cancellationToken = default);
}
