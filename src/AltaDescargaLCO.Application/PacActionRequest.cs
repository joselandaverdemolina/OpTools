using AltaDescargaLCO.Domain;

namespace AltaDescargaLCO.Application;

public sealed record PacActionRequest(string RawRfc, PacAction Action);

public sealed record PipelineResult(bool Success, PipelineStage? FailedStage, string? ErrorMessage)
{
    public static PipelineResult Ok() => new(true, null, null);

    public static PipelineResult Failed(PipelineStage stage, string message) => new(false, stage, message);
}
