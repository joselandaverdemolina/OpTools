namespace AltaDescargaLCO.App.Views;

partial class LogPanel
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblLog = new Label();
        btnCopy = new Button();
        rtbLog = new RichTextBox();
        SuspendLayout();

        // lblLog
        lblLog.AutoSize = true;
        lblLog.Location = new Point(0, 4);
        lblLog.Name = "lblLog";
        lblLog.Size = new Size(150, 20);
        lblLog.TabIndex = 0;
        lblLog.Text = "Bitácora del proceso";

        // btnCopy
        btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.FlatStyle = FlatStyle.Flat;
        btnCopy.Location = new Point(540, 0);
        btnCopy.Name = "btnCopy";
        btnCopy.Size = new Size(56, 26);
        btnCopy.TabIndex = 1;
        btnCopy.Text = "Copiar";
        btnCopy.UseVisualStyleBackColor = true;
        btnCopy.Click += btnCopy_Click;

        // rtbLog
        rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rtbLog.BackColor = Color.White;
        rtbLog.Font = new Font("Consolas", 9F);
        rtbLog.Location = new Point(0, 30);
        rtbLog.Name = "rtbLog";
        rtbLog.ReadOnly = true;
        rtbLog.Size = new Size(596, 290);
        rtbLog.TabIndex = 2;
        rtbLog.Text = string.Empty;

        // LogPanel
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(lblLog);
        Controls.Add(btnCopy);
        Controls.Add(rtbLog);
        Name = "LogPanel";
        Size = new Size(596, 320);
        ResumeLayout(false);
    }

    private Label lblLog;
    private Button btnCopy;
    private RichTextBox rtbLog;
}
