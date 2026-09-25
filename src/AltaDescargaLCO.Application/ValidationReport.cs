namespace AltaDescargaLCO.Application;

public enum ValidationStage
{
    Availability,
    ErrorRate
}

public sealed record ValidationProgress(ValidationStage Stage, string Message, PipelineSeverity Severity);

public enum ServiceAvailabilityStatus
{
    Healthy,
    Intermittent,
    Down
}

public sealed record ValidationOutcome(bool Passed, string Detail);

public sealed record ValidationReport(ValidationOutcome Availability, ValidationOutcome ErrorRate)
{
    public bool AllPassed => Availability.Passed && ErrorRate.Passed;
}
