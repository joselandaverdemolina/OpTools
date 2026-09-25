namespace AltaDescargaLCO.App.Views;

partial class PacProvisioningView
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
        logPanel = new LogPanel();
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
        txtRfc.KeyDown += txtRfc_KeyDown;

        // btnExecute
        btnExecute.Location = new Point(340, 119);
        btnExecute.Name = "btnExecute";
        btnExecute.Size = new Size(130, 31);
        btnExecute.TabIndex = 3;
        btnExecute.Text = "Ejecutar";
        btnExecute.UseVisualStyleBackColor = true;
        btnExecute.Click += btnExecute_Click;

        // logPanel
        logPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logPanel.Location = new Point(16, 164);
        logPanel.Name = "logPanel";
        logPanel.Size = new Size(596, 326);
        logPanel.TabIndex = 4;

        // PacProvisioningView
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(grpAction);
        Controls.Add(lblRfc);
        Controls.Add(txtRfc);
        Controls.Add(btnExecute);
        Controls.Add(logPanel);
        Name = "PacProvisioningView";
        Size = new Size(628, 506);
        grpAction.ResumeLayout(false);
        ResumeLayout(false);
    }

    private GroupBox grpAction;
    private RadioButton rbAdd;
    private RadioButton rbRemove;
    private Label lblRfc;
    private TextBox txtRfc;
    private Button btnExecute;
    private LogPanel logPanel;
}
