using AltaDescargaLCO.Application;
using AltaDescargaLCO.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AltaDescargaLCO.App.Views;

public sealed partial class PacProvisioningView : UserControl
{
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _isExecuting;

    public PacProvisioningView(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        InitializeComponent();
    }

    private async void btnExecute_Click(object? sender, EventArgs e)
    {
        if (_isExecuting)
        {
            return;
        }

        _isExecuting = true;
        btnExecute.Enabled = false;
        try
        {
            if (!TryBuildRequest(out var request))
            {
                return;
            }

            logPanel.AppendSeparator();
            var progress = new UiProgress<PipelineProgress>(this, OnProgress);

            // Un scope por corrida: la credencial de Azure creada dentro muere al liberarse el
            // scope, así que no puede reutilizarse en otra corrida ni en otra operación.
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IPacProvisioningService>();
            var result = await pipeline.ExecuteAsync(request!, progress);

            if (result.Success)
            {
                logPanel.AppendLine("Proceso terminado correctamente.", LogPanel.ProcessColor);
            }
            else
            {
                logPanel.AppendLine(
                    $"El proceso se detuvo en la etapa de {StageName(result.FailedStage!.Value)}.",
                    LogPanel.ErrorColor);
            }
        }
        catch (Exception ex)
        {
            logPanel.AppendLine(
                $"Error inesperado: {ex.GetType().Name}: {ex.Message}", LogPanel.ErrorColor);
        }
        finally
        {
            btnExecute.Enabled = true;
            _isExecuting = false;
        }
    }

    // AcceptButton es propiedad de Form y no existe en un UserControl, así que Enter se atiende aquí
    // para no perder el atajo que ya usaban los operadores.
    private void txtRfc_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        btnExecute.PerformClick();
    }

    private bool TryBuildRequest(out PacActionRequest? request)
    {
        request = null;

        if (!rbAdd.Checked && !rbRemove.Checked)
        {
            logPanel.AppendSeparator();
            logPanel.AppendLine(
                "Seleccione el tipo de movimiento: Alta o Baja.", LogPanel.ErrorColor);
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtRfc.Text))
        {
            logPanel.AppendSeparator();
            logPanel.AppendLine("Capture el RFC.", LogPanel.ErrorColor);
            txtRfc.Focus();
            return false;
        }

        request = new PacActionRequest(txtRfc.Text, rbAdd.Checked ? PacAction.Add : PacAction.Remove);
        return true;
    }

    private void OnProgress(PipelineProgress progress) =>
        logPanel.AppendLine(
            $"[{StageName(progress.Stage)}] {progress.Message}",
            progress.Severity == PipelineSeverity.Error ? LogPanel.ErrorColor : LogPanel.ProcessColor);

    private static string StageName(PipelineStage stage) => stage switch
    {
        PipelineStage.Validation => "Validación",
        PipelineStage.Database => "Base de datos",
        PipelineStage.AzureRestart => "Reinicio en Azure",
        PipelineStage.HealthCheck => "Verificación",
        _ => stage.ToString()
    };
}
