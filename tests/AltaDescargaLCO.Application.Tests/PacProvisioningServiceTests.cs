using AltaDescargaLCO.Application;
using AltaDescargaLCO.AzureIntegration;
using AltaDescargaLCO.DataAccess;
using AltaDescargaLCO.Domain;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.Application.Tests;

public class PacProvisioningServiceTests
{
    private const string ValidRfc = "GODE561231GR8";

    [Fact]
    public async Task Add_inserts_restarts_and_verifies()
    {
        var repository = new FakePacRepository();
        var webApp = new FakeWebAppLifecycleService();
        var probe = new FakeHealthProbe(healthyAfterAttempts: 1);
        var service = Build(repository, webApp, probe);

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), new NoProgress());

        Assert.True(result.Success);
        Assert.Equal(new[] { ValidRfc }, repository.Rows);
        Assert.Equal(1, webApp.RestartCount);
        Assert.Equal(1, probe.Attempts);
    }

    [Fact]
    public async Task Add_of_an_existing_rfc_skips_the_insert_but_still_restarts()
    {
        var repository = new FakePacRepository(ValidRfc);
        var webApp = new FakeWebAppLifecycleService();
        var progress = new RecordingProgress();
        var service = Build(repository, webApp, new FakeHealthProbe(healthyAfterAttempts: 1));

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), progress);

        Assert.True(result.Success);
        Assert.Equal(new[] { ValidRfc }, repository.Rows);
        Assert.Equal(1, webApp.RestartCount);
        Assert.Contains(progress.Messages, m => m.Contains("ya estaba registrado"));
    }

    [Fact]
    public async Task Remove_deletes_restarts_and_verifies()
    {
        var repository = new FakePacRepository(ValidRfc);
        var webApp = new FakeWebAppLifecycleService();
        var service = Build(repository, webApp, new FakeHealthProbe(healthyAfterAttempts: 1));

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Remove), new NoProgress());

        Assert.True(result.Success);
        Assert.Empty(repository.Rows);
        Assert.Equal(1, webApp.RestartCount);
    }

    [Fact]
    public async Task Remove_of_a_missing_rfc_still_restarts()
    {
        var repository = new FakePacRepository();
        var webApp = new FakeWebAppLifecycleService();
        var progress = new RecordingProgress();
        var service = Build(repository, webApp, new FakeHealthProbe(healthyAfterAttempts: 1));

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Remove), progress);

        Assert.True(result.Success);
        Assert.Equal(1, webApp.RestartCount);
        Assert.Contains(progress.Messages, m => m.Contains("No se encontró"));
    }

    [Fact]
    public async Task An_invalid_rfc_stops_before_touching_sql_or_azure()
    {
        var repository = new FakePacRepository();
        var webApp = new FakeWebAppLifecycleService();
        var probe = new FakeHealthProbe(healthyAfterAttempts: 1);
        var service = Build(repository, webApp, probe);

        var result = await service.ExecuteAsync(new PacActionRequest("NOT-AN-RFC", PacAction.Add), new NoProgress());

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.Validation, result.FailedStage);
        Assert.Equal(0, repository.CallCount);
        Assert.Equal(0, webApp.RestartCount);
        Assert.Equal(0, probe.Attempts);
    }

    [Fact]
    public async Task A_database_error_stops_before_the_restart()
    {
        var repository = new FakePacRepository { FailWith = new InvalidOperationException("login failed") };
        var webApp = new FakeWebAppLifecycleService();
        var service = Build(repository, webApp, new FakeHealthProbe(healthyAfterAttempts: 1));

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), new NoProgress());

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.Database, result.FailedStage);
        Assert.Contains("login failed", result.ErrorMessage);
        Assert.Equal(0, webApp.RestartCount);
    }

    [Fact]
    public async Task A_failed_restart_skips_the_health_check()
    {
        var repository = new FakePacRepository();
        var webApp = new FakeWebAppLifecycleService { FailWith = new InvalidOperationException("forbidden") };
        var probe = new FakeHealthProbe(healthyAfterAttempts: 1);
        var service = Build(repository, webApp, probe);

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), new NoProgress());

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.AzureRestart, result.FailedStage);
        Assert.Contains("forbidden", result.ErrorMessage);
        Assert.Equal(0, probe.Attempts);
    }

    [Fact]
    public async Task A_health_check_that_never_succeeds_fails_with_the_last_probe_detail()
    {
        var repository = new FakePacRepository();
        var webApp = new FakeWebAppLifecycleService { State = WebAppRuntimeState.Stopped };
        var probe = new FakeHealthProbe(healthyAfterAttempts: int.MaxValue);
        var service = Build(repository, webApp, probe, healthCheckTimeoutSeconds: 1);

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), new NoProgress());

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.HealthCheck, result.FailedStage);
        Assert.Contains("HTTP 503", result.ErrorMessage);
        Assert.Contains("detenida", result.ErrorMessage);
    }

    [Fact]
    public async Task The_health_check_retries_until_the_app_answers()
    {
        var repository = new FakePacRepository();
        var probe = new FakeHealthProbe(healthyAfterAttempts: 2);
        var service = Build(repository, new FakeWebAppLifecycleService(), probe, healthCheckTimeoutSeconds: 10);

        var result = await service.ExecuteAsync(new PacActionRequest(ValidRfc, PacAction.Add), new NoProgress());

        Assert.True(result.Success);
        Assert.Equal(2, probe.Attempts);
    }

    private static PacProvisioningService Build(
        IPacRepository repository,
        IWebAppLifecycleService webApp,
        IWebAppHealthProbe probe,
        int healthCheckTimeoutSeconds = 30) =>
        new(repository, webApp, probe, Options.Create(new AzureSettings
        {
            SubscriptionId = "sub",
            ResourceGroupName = "rg",
            WebAppName = "app",
            HealthCheckUrl = "https://example.invalid/",
            HealthCheckPollIntervalSeconds = 1,
            HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds
        }));

    /// <summary>
    /// Imita al procedimiento almacenado: el alta inserta sólo si no existe y la baja elimina sólo
    /// si existe, devolviendo en ambos casos si hubo cambio.
    /// </summary>
    private sealed class FakePacRepository : IPacRepository
    {
        public FakePacRepository(params string[] existingRows) => Rows = new List<string>(existingRows);

        public List<string> Rows { get; }

        public int CallCount { get; private set; }

        public Exception? FailWith { get; init; }

        public Task<PacChangeOutcome> ApplyAsync(
            PacAction action,
            Rfc rfc,
            CancellationToken cancellationToken = default)
        {
            if (FailWith is not null)
            {
                throw FailWith;
            }

            CallCount++;

            if (action == PacAction.Add)
            {
                if (Rows.Contains(rfc.Value))
                {
                    return Task.FromResult(PacChangeOutcome.NoChangeNeeded);
                }

                Rows.Add(rfc.Value);
                return Task.FromResult(PacChangeOutcome.Applied);
            }

            return Task.FromResult(Rows.Remove(rfc.Value)
                ? PacChangeOutcome.Applied
                : PacChangeOutcome.NoChangeNeeded);
        }
    }

    private sealed class FakeWebAppLifecycleService : IWebAppLifecycleService
    {
        public int RestartCount { get; private set; }

        public Exception? FailWith { get; init; }

        public WebAppRuntimeState State { get; init; } = WebAppRuntimeState.Running;

        public Task RestartWebAppAsync(CancellationToken cancellationToken = default)
        {
            if (FailWith is not null)
            {
                throw FailWith;
            }

            RestartCount++;
            return Task.CompletedTask;
        }

        public Task<WebAppRuntimeState> GetWebAppStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(State);
    }

    private sealed class FakeHealthProbe : IWebAppHealthProbe
    {
        private readonly int _healthyAfterAttempts;

        public FakeHealthProbe(int healthyAfterAttempts) => _healthyAfterAttempts = healthyAfterAttempts;

        public int Attempts { get; private set; }

        public Task<HealthProbeResult> PingAsync(CancellationToken cancellationToken = default)
        {
            Attempts++;
            return Task.FromResult(Attempts >= _healthyAfterAttempts
                ? new HealthProbeResult(true, "HTTP 200 OK")
                : new HealthProbeResult(false, "HTTP 503 Service Unavailable"));
        }
    }

    private sealed class NoProgress : IProgress<PipelineProgress>
    {
        public void Report(PipelineProgress value)
        {
        }
    }

    private sealed class RecordingProgress : IProgress<PipelineProgress>
    {
        public List<string> Messages { get; } = new();

        public void Report(PipelineProgress value) => Messages.Add(value.Message);
    }
}

