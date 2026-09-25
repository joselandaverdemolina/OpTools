namespace AltaDescargaLCO.Application;

public enum PipelineStage
{
    Validation,
    Database,
    AzureRestart,
    HealthCheck
}

public enum PipelineSeverity
{
    Info,
    Success,
    Error
}

public sealed record PipelineProgress(PipelineStage Stage, string Message, PipelineSeverity Severity);
