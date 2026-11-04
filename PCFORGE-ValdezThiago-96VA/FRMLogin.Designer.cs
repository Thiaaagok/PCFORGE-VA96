namespace PCFORGE_ValdezThiago_96VA
{
    partial class FRMLogin
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblMarca = new System.Windows.Forms.Label();
            this.lblEslogan = new System.Windows.Forms.Label();
            this.pnlTarjeta = new System.Windows.Forms.Panel();
            this.lblBienvenidos = new System.Windows.Forms.Label();
            this.Usuario = new System.Windows.Forms.Label();
            this.pnlCampoUsuario = new System.Windows.Forms.Panel();
            this.LoginTextBox = new System.Windows.Forms.TextBox();
            this.materialLabel2 = new System.Windows.Forms.Label();
            this.pnlCampoContrasenia = new System.Windows.Forms.Panel();
            this.ContraseniaTextBox = new System.Windows.Forms.TextBox();
            this.btnVerContrasenia = new System.Windows.Forms.Button();
            this.IniciarSesionBTN = new System.Windows.Forms.Button();
            this.CerrarBTN = new System.Windows.Forms.Button();
            this.pnlTarjeta.SuspendLayout();
            this.pnlCampoUsuario.SuspendLayout();
            this.pnlCampoContrasenia.SuspendLayout();
            this.SuspendLayout();
            this.lblMarca.BackColor = System.Drawing.Color.Transparent;
            this.lblMarca.Location = new System.Drawing.Point(72, 101);
            this.lblMarca.Name = "lblMarca";
            this.lblMarca.Size = new System.Drawing.Size(400, 107);
            this.lblMarca.TabIndex = 0;
            this.lblMarca.Text = "PCFORGE";
            this.lblEslogan.BackColor = System.Drawing.Color.Transparent;
            this.lblEslogan.Location = new System.Drawing.Point(72, 238);
            this.lblEslogan.Name = "lblEslogan";
            this.lblEslogan.Size = new System.Drawing.Size(420, 110);
            this.lblEslogan.TabIndex = 1;
            this.pnlTarjeta.Controls.Add(this.lblBienvenidos);
            this.pnlTarjeta.Controls.Add(this.Usuario);
            this.pnlTarjeta.Controls.Add(this.pnlCampoUsuario);
            this.pnlTarjeta.Controls.Add(this.materialLabel2);
            this.pnlTarjeta.Controls.Add(this.pnlCampoContrasenia);
            this.pnlTarjeta.Controls.Add(this.IniciarSesionBTN);
            this.pnlTarjeta.Controls.Add(this.CerrarBTN);
            this.pnlTarjeta.Location = new System.Drawing.Point(624, 101);
            this.pnlTarjeta.Name = "pnlTarjeta";
            this.pnlTarjeta.Size = new System.Drawing.Size(312, 331);
            this.pnlTarjeta.TabIndex = 2;
            this.lblBienvenidos.Location = new System.Drawing.Point(0, 8);
            this.lblBienvenidos.Name = "lblBienvenidos";
            this.lblBienvenidos.Size = new System.Drawing.Size(312, 48);
            this.lblBienvenidos.TabIndex = 0;
            this.lblBienvenidos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Usuario.Location = new System.Drawing.Point(11, 70);
            this.Usuario.Name = "Usuario";
            this.Usuario.Size = new System.Drawing.Size(290, 32);
            this.Usuario.TabIndex = 1;
            this.Usuario.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.pnlCampoUsuario.Controls.Add(this.LoginTextBox);
            this.pnlCampoUsuario.Location = new System.Drawing.Point(11, 107);
            this.pnlCampoUsuario.Name = "pnlCampoUsuario";
            this.pnlCampoUsuario.Padding = new System.Windows.Forms.Padding(8, 6, 8, 0);
            this.pnlCampoUsuario.Size = new System.Drawing.Size(290, 34);
            this.pnlCampoUsuario.TabIndex = 2;
            this.LoginTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LoginTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LoginTextBox.Name = "LoginTextBox";
            this.LoginTextBox.TabIndex = 0;
            this.materialLabel2.Location = new System.Drawing.Point(11, 145);
            this.materialLabel2.Name = "materialLabel2";
            this.materialLabel2.Size = new System.Drawing.Size(290, 32);
            this.materialLabel2.TabIndex = 3;
            this.materialLabel2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.pnlCampoContrasenia.Controls.Add(this.btnVerContrasenia);
            this.pnlCampoContrasenia.Controls.Add(this.ContraseniaTextBox);
            this.pnlCampoContrasenia.Location = new System.Drawing.Point(11, 182);
            this.pnlCampoContrasenia.Name = "pnlCampoContrasenia";
            this.pnlCampoContrasenia.Padding = new System.Windows.Forms.Padding(8, 6, 40, 0);
            this.pnlCampoContrasenia.Size = new System.Drawing.Size(290, 34);
            this.pnlCampoContrasenia.TabIndex = 4;
            this.ContraseniaTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ContraseniaTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContraseniaTextBox.Name = "ContraseniaTextBox";
            this.ContraseniaTextBox.PasswordChar = '*';
            this.ContraseniaTextBox.TabIndex = 0;
            this.btnVerContrasenia.Location = new System.Drawing.Point(256, 0);
            this.btnVerContrasenia.Name = "btnVerContrasenia";
            this.btnVerContrasenia.Size = new System.Drawing.Size(34, 34);
            this.btnVerContrasenia.TabIndex = 1;
            this.btnVerContrasenia.TabStop = false;
            this.btnVerContrasenia.Click += new System.EventHandler(this.AlternarVerContrasenia);
            this.IniciarSesionBTN.Location = new System.Drawing.Point(11, 229);
            this.IniciarSesionBTN.Name = "IniciarSesionBTN";
            this.IniciarSesionBTN.Size = new System.Drawing.Size(290, 42);
            this.IniciarSesionBTN.TabIndex = 5;
            this.IniciarSesionBTN.Click += new System.EventHandler(this.IniciarSesionBTN_Click);
            this.CerrarBTN.Location = new System.Drawing.Point(11, 279);
            this.CerrarBTN.Name = "CerrarBTN";
            this.CerrarBTN.Size = new System.Drawing.Size(290, 42);
            this.CerrarBTN.TabIndex = 6;
            this.CerrarBTN.Click += new System.EventHandler(this.CerrarBTN_Click);
            this.AcceptButton = this.IniciarSesionBTN;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1040, 476);
            this.ControlBox = false;
            this.Controls.Add(this.pnlTarjeta);
            this.Controls.Add(this.lblEslogan);
            this.Controls.Add(this.lblMarca);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FRMLogin";
            this.ShowIcon = false;
            this.ShowInTaskbar = true;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.Login_Load);
            this.pnlCampoContrasenia.ResumeLayout(false);
            this.pnlCampoContrasenia.PerformLayout();
            this.pnlCampoUsuario.ResumeLayout(false);
            this.pnlCampoUsuario.PerformLayout();
            this.pnlTarjeta.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lblMarca;
        private System.Windows.Forms.Label lblEslogan;
        private System.Windows.Forms.Panel pnlTarjeta;
        private System.Windows.Forms.Label lblBienvenidos;
        private System.Windows.Forms.Label Usuario;
        private System.Windows.Forms.Panel pnlCampoUsuario;
        private System.Windows.Forms.TextBox LoginTextBox;
        private System.Windows.Forms.Label materialLabel2;
        private System.Windows.Forms.Panel pnlCampoContrasenia;
        private System.Windows.Forms.TextBox ContraseniaTextBox;
        private System.Windows.Forms.Button btnVerContrasenia;
        private System.Windows.Forms.Button IniciarSesionBTN;
        private System.Windows.Forms.Button CerrarBTN;
    }
}
