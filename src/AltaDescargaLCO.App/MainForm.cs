using AltaDescargaLCO.Application;
using AltaDescargaLCO.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AltaDescargaLCO.App;

public sealed partial class MainForm : Form
{
    private static readonly Color ProcessColor = Color.FromArgb(0, 128, 0);
    private static readonly Color ErrorColor = Color.FromArgb(192, 0, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private bool _isExecuting;

    public MainForm(IServiceScopeFactory scopeFactory)
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

            AppendSeparator();
            var progress = new UiProgress(this, OnProgress);

            // One DI scope per run: the Azure credential created inside it is discarded when the
            // scope is disposed, so it cannot be reused by a later run or any other operation.
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IPacProvisioningService>();
            var result = await pipeline.ExecuteAsync(request!, progress);

            if (result.Success)
            {
                AppendLine("Proceso terminado correctamente.", ProcessColor);
            }
            else
            {
                AppendLine(
                    $"El proceso se detuvo en la etapa de {StageName(result.FailedStage!.Value)}.", ErrorColor);
            }
        }
        catch (Exception ex)
        {
            AppendLine($"Error inesperado: {ex.GetType().Name}: {ex.Message}", ErrorColor);
        }
        finally
        {
            btnExecute.Enabled = true;
            _isExecuting = false;
        }
    }

    private void btnCopy_Click(object? sender, EventArgs e)
    {
        if (rtbLog.TextLength == 0)
        {
            return;
        }

        Clipboard.SetText(rtbLog.Text);
    }

    private bool TryBuildRequest(out PacActionRequest? request)
    {
        request = null;

        if (!rbAdd.Checked && !rbRemove.Checked)
        {
            AppendSeparator();
            AppendLine("Seleccione el tipo de movimiento: Alta o Baja.", ErrorColor);
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtRfc.Text))
        {
            AppendSeparator();
            AppendLine("Capture el RFC.", ErrorColor);
            txtRfc.Focus();
            return false;
        }

        request = new PacActionRequest(txtRfc.Text, rbAdd.Checked ? PacAction.Add : PacAction.Remove);
        return true;
    }

    private void OnProgress(PipelineProgress progress) =>
        AppendLine(
            $"[{StageName(progress.Stage)}] {progress.Message}",
            progress.Severity == PipelineSeverity.Error ? ErrorColor : ProcessColor);

    private static string StageName(PipelineStage stage) => stage switch
    {
        PipelineStage.Validation => "Validación",
        PipelineStage.Database => "Base de datos",
        PipelineStage.AzureRestart => "Reinicio en Azure",
        PipelineStage.HealthCheck => "Verificación",
        _ => stage.ToString()
    };

    private void AppendLine(string message, Color color)
    {
        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }

    private void AppendSeparator()
    {
        if (rtbLog.TextLength > 0)
        {
            AppendLine(new string('-', 60), ProcessColor);
        }
    }

    /// <summary>
    /// Reports progress synchronously. Progress&lt;T&gt; posts to the UI thread instead, which lets a
    /// pipeline that finishes without yielding write its summary line before its stage lines.
    /// </summary>
    private sealed class UiProgress : IProgress<PipelineProgress>
    {
        private readonly Control _owner;
        private readonly Action<PipelineProgress> _handler;

        public UiProgress(Control owner, Action<PipelineProgress> handler)
        {
            _owner = owner;
            _handler = handler;
        }

        public void Report(PipelineProgress value)
        {
            if (_owner.InvokeRequired)
            {
                _owner.Invoke(_handler, value);
            }
            else
            {
                _handler(value);
            }
        }
    }
}
