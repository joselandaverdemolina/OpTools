namespace AltaDescargaLCO.App.Views;

public sealed partial class LogPanel : UserControl
{
    public static readonly Color ProcessColor = Color.FromArgb(0, 128, 0);
    public static readonly Color ErrorColor = Color.FromArgb(192, 0, 0);

    public LogPanel()
    {
        InitializeComponent();
    }

    public void AppendLine(string message, Color color)
    {
        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }

    public void AppendSeparator()
    {
        if (rtbLog.TextLength > 0)
        {
            AppendLine(new string('-', 60), ProcessColor);
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
}
