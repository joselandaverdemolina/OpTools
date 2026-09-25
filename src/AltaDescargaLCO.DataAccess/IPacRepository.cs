using AltaDescargaLCO.Domain;

namespace AltaDescargaLCO.DataAccess;

public enum PacChangeOutcome
{
    
    Applied,

    
    NoChangeNeeded
}

public interface IPacRepository
{
    Task<PacChangeOutcome> ApplyAsync(
        PacAction action,
        Rfc rfc,
        CancellationToken cancellationToken = default);
}
