namespace AltaDescargaLCO.App;

partial class MainForm
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
        grpAction = new GroupBox();
        rbAdd = new RadioButton();
        rbRemove = new RadioButton();
        lblRfc = new Label();
        txtRfc = new TextBox();
        btnExecute = new Button();
        lblLog = new Label();
        btnCopy = new Button();
        rtbLog = new RichTextBox();
        grpAction.SuspendLayout();
        SuspendLayout();

        // grpAction
        grpAction.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpAction.Controls.Add(rbAdd);
        grpAction.Controls.Add(rbRemove);
        grpAction.Location = new Point(16, 16);
        grpAction.Name = "grpAction";
        grpAction.Size = new Size(596, 66);
        grpAction.TabIndex = 0;
        grpAction.TabStop = false;
        grpAction.Text = "Tipo de movimiento";

        // rbAdd
        rbAdd.AutoSize = true;
        rbAdd.Location = new Point(18, 28);
        rbAdd.Name = "rbAdd";
        rbAdd.Size = new Size(60, 24);
        rbAdd.TabIndex = 0;
        rbAdd.Text = "Alta";

        // rbRemove
        rbRemove.AutoSize = true;
        rbRemove.Location = new Point(130, 28);
        rbRemove.Name = "rbRemove";
        rbRemove.Size = new Size(95, 24);
        rbRemove.TabIndex = 1;
        rbRemove.Text = "Baja";

        // lblRfc
        lblRfc.AutoSize = true;
        lblRfc.Location = new Point(16, 98);
        lblRfc.Name = "lblRfc";
        lblRfc.Size = new Size(38, 20);
        lblRfc.TabIndex = 1;
        lblRfc.Text = "RFC";

        // txtRfc
        txtRfc.CharacterCasing = CharacterCasing.Upper;
        txtRfc.Location = new Point(16, 121);
        txtRfc.MaxLength = 13;
        txtRfc.Name = "txtRfc";
        txtRfc.Size = new Size(300, 27);
        txtRfc.TabIndex = 2;

        // btnExecute
        btnExecute.Location = new Point(340, 119);
        btnExecute.Name = "btnExecute";
        btnExecute.Size = new Size(130, 31);
        btnExecute.TabIndex = 3;
        btnExecute.Text = "Ejecutar";
        btnExecute.UseVisualStyleBackColor = true;
        btnExecute.Click += btnExecute_Click;

        // lblLog
        lblLog.AutoSize = true;
        lblLog.Location = new Point(16, 168);
        lblLog.Name = "lblLog";
        lblLog.Size = new Size(150, 20);
        lblLog.TabIndex = 4;
        lblLog.Text = "Bitácora del proceso";

        // btnCopy
        btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.FlatStyle = FlatStyle.Flat;
        btnCopy.Location = new Point(556, 163);
        btnCopy.Name = "btnCopy";
        btnCopy.Size = new Size(56, 26);
        btnCopy.TabIndex = 5;
        btnCopy.Text = "Copiar";
        btnCopy.UseVisualStyleBackColor = true;
        btnCopy.Click += btnCopy_Click;

        // rtbLog
        rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rtbLog.BackColor = Color.White;
        rtbLog.Font = new Font("Consolas", 9F);
        rtbLog.Location = new Point(16, 194);
        rtbLog.Name = "rtbLog";
        rtbLog.ReadOnly = true;
        rtbLog.Size = new Size(596, 296);
        rtbLog.TabIndex = 6;
        rtbLog.Text = string.Empty;

        // MainForm
        AcceptButton = btnExecute;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(628, 510);
        Controls.Add(grpAction);
        Controls.Add(lblRfc);
        Controls.Add(txtRfc);
        Controls.Add(btnExecute);
        Controls.Add(lblLog);
        Controls.Add(btnCopy);
        Controls.Add(rtbLog);
        MinimumSize = new Size(520, 420);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Alta / Baja de RFC PAC";
        grpAction.ResumeLayout(false);
        ResumeLayout(false);
    }

    private GroupBox grpAction;
    private RadioButton rbAdd;
    private RadioButton rbRemove;
    private Label lblRfc;
    private TextBox txtRfc;
    private Button btnExecute;
    private Label lblLog;
    private Button btnCopy;
    private RichTextBox rtbLog;
}

