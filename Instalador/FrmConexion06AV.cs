using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Instalador
{
    public class FrmConexion06AV : Form
    {
        private static readonly Color Primario = Color.FromArgb(21, 101, 192);
        private static readonly Color Texto = Color.FromArgb(33, 33, 33);
        private static readonly Color TextoSuave = Color.FromArgb(110, 110, 110);

        private ComboBox cboInstancia;
        private Button btnDetectar;
        private Label lblEstado;
        private RadioButton rbWindows;
        private RadioButton rbSql;
        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Label lblUsuario;
        private Label lblPassword;
        private TextBox txtBaseDatos;
        private Button btnContinuar;
        private Button btnCancelar;

        public OpcionesInstalacion06AV Opciones { get; private set; }

        // Estado de la base elegida, leído mientras se prueba la conexión (así no hay
        // tiempos muertos sin ventana entre este formulario y el siguiente).
        public bool BaseExiste { get; private set; }
        public int TablasExistentes { get; private set; }
        public int UsuariosExistentes { get; private set; }
        public string CarpetaBackups { get; private set; }

        public FrmConexion06AV()
        {
            ConstruirUI();
            // La ventana aparece enseguida y la búsqueda corre en segundo plano.
            Shown += async (s, e) => await DetectarInstanciasAsync();
        }

        private void ConstruirUI()
        {
            Text = "Instalación de PCFORGE";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 420);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var lblTitulo = new Label
            {
                Text = "Configurar conexión a la base de datos",
                Font = new Font("Segoe UI Semibold", 15.75f, FontStyle.Bold),
                ForeColor = Primario,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            var lblIntro = new Label
            {
                Text = "Elegí la instancia de SQL Server de este equipo o escribí un servidor remoto.",
                ForeColor = TextoSuave,
                AutoSize = true,
                Location = new Point(26, 58)
            };

            var lblCarpeta = new Label
            {
                Text = "El sistema se instalará en: " + InstaladorSistema06AV.CarpetaInstalada(),
                ForeColor = TextoSuave,
                AutoSize = true,
                Location = new Point(26, 78)
            };

            var lblInstancia = new Label
            {
                Text = "Instancia:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 100)
            };
            cboInstancia = new ComboBox
            {
                Location = new Point(30, 122),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            btnDetectar = new Button
            {
                Text = "Detectar",
                Location = new Point(422, 121),
                Width = 108,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnDetectar.FlatAppearance.BorderColor = Color.FromArgb(189, 189, 189);
            btnDetectar.Click += async (s, e) => await DetectarInstanciasAsync();

            lblEstado = new Label
            {
                Text = "",
                ForeColor = TextoSuave,
                AutoSize = true,
                Location = new Point(30, 152)
            };

            var lblBase = new Label
            {
                Text = "Base de datos:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 186)
            };
            txtBaseDatos = new TextBox
            {
                Location = new Point(30, 208),
                Width = 380,
                Text = "IngSoftValdezAlegre"
            };

            var lblAuth = new Label
            {
                Text = "Autenticación:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 246)
            };
            rbWindows = new RadioButton
            {
                Text = "Windows",
                Checked = true,
                AutoSize = true,
                Location = new Point(30, 268)
            };
            rbSql = new RadioButton
            {
                Text = "SQL Server",
                AutoSize = true,
                Location = new Point(140, 268)
            };
            rbWindows.CheckedChanged += (s, e) => ActualizarAuth();
            rbSql.CheckedChanged += (s, e) => ActualizarAuth();

            lblUsuario = new Label
            {
                Text = "Usuario:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(30, 300)
            };
            txtUsuario = new TextBox { Location = new Point(90, 297), Width = 150, Text = "sa" };

            lblPassword = new Label
            {
                Text = "Contraseña:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(260, 300)
            };
            txtPassword = new TextBox
            {
                Location = new Point(345, 297),
                Width = 165,
                UseSystemPasswordChar = true
            };

            btnContinuar = new Button
            {
                Text = "Continuar",
                Location = new Point(322, 366),
                Width = 108,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Primario,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnContinuar.FlatAppearance.BorderSize = 0;
            btnContinuar.Click += (s, e) => Continuar();

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(438, 366),
                Width = 96,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Texto,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(189, 189, 189);

            Controls.AddRange(new Control[]
            {
                lblTitulo, lblIntro, lblCarpeta, lblInstancia, cboInstancia, btnDetectar, lblEstado,
                lblBase, txtBaseDatos, lblAuth, rbWindows, rbSql,
                lblUsuario, txtUsuario, lblPassword, txtPassword,
                btnContinuar, btnCancelar
            });

            AcceptButton = btnContinuar;
            CancelButton = btnCancelar;
            ActualizarAuth();
        }

        private void ActualizarAuth()
        {
            bool sql = rbSql.Checked;
            lblUsuario.Enabled = txtUsuario.Enabled = sql;
            lblPassword.Enabled = txtPassword.Enabled = sql;
        }

        private bool _ocupado;

        private void Ocupado(bool ocupado, string estado)
        {
            _ocupado = ocupado;
            UseWaitCursor = ocupado;
            btnDetectar.Enabled = btnContinuar.Enabled = cboInstancia.Enabled = !ocupado;
            lblEstado.Text = estado ?? "";
        }

        private async Task DetectarInstanciasAsync()
        {
            if (_ocupado) return;
            Ocupado(true, "Buscando instancias de SQL Server en este equipo...");
            string seleccion = cboInstancia.Text;
            try
            {
                var resultado = await Task.Run(() =>
                {
                    List<string> encontradas = InstaladorDAL06AV.DetectarInstancias();
                    var pruebas = encontradas.Select(i => Task.Run(() => new { Nombre = i, Ok = InstaladorDAL06AV.InstanciaResponde(i) })).ToArray();
                    Task.WaitAll(pruebas);
                    return pruebas.Select(t => t.Result).ToList();
                });

                if (IsDisposed) return;
                cboInstancia.Items.Clear();
                foreach (var r in resultado.OrderByDescending(r => r.Ok))
                    cboInstancia.Items.Add(r.Nombre);

                int responden = resultado.Count(r => r.Ok);
                if (!string.IsNullOrWhiteSpace(seleccion))
                    cboInstancia.Text = seleccion;
                else if (cboInstancia.Items.Count > 0)
                    cboInstancia.SelectedIndex = 0;

                Ocupado(false, resultado.Count == 0
                    ? "No se encontró SQL Server en este equipo. Instalalo o escribí el nombre de un servidor remoto."
                    : $"Se encontraron {resultado.Count} instancia(s); {responden} responde(n).");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) Ocupado(false, "No se pudieron detectar instancias: " + ex.Message);
            }
        }

        private async void Continuar()
        {
            if (_ocupado) return;
            string instancia = (cboInstancia.Text ?? "").Trim();
            if (instancia.Length == 0)
            {
                MessageBox.Show(this, "Elegí o escribí una instancia de SQL Server.",
                    "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string baseDatos = (txtBaseDatos.Text ?? "").Trim();
            if (baseDatos.Length == 0) baseDatos = "IngSoftValdezAlegre";
            if (!System.Text.RegularExpressions.Regex.IsMatch(baseDatos, @"^[A-Za-z0-9_]+$"))
            {
                MessageBox.Show(this, "El nombre de la base solo puede tener letras, números y guion bajo.",
                    "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var op = new OpcionesInstalacion06AV
            {
                Servidor = instancia,
                BaseDatos = baseDatos,
                SeguridadIntegrada = rbWindows.Checked
            };
            if (rbSql.Checked)
            {
                op.Usuario = txtUsuario.Text;
                op.Contrasenia = txtPassword.Text;
            }

            Ocupado(true, "Probando conexión con " + instancia + "...");
            try
            {
                await Task.Run(() =>
                {
                    var dal = new InstaladorDAL06AV(op);
                    dal.ProbarConexion();
                    BaseExiste = dal.ExisteBaseDatos();
                    if (BaseExiste)
                    {
                        TablasExistentes = dal.ContarTablas();
                        UsuariosExistentes = dal.ContarUsuarios();
                        try { CarpetaBackups = dal.CarpetaBackupServidor(); } catch {  }
                    }
                });
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                Ocupado(false, "");
                MessageBox.Show(this,
                    "No se pudo conectar a la instancia indicada:\n\n" + ex.Message,
                    "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (IsDisposed) return;
            Ocupado(false, "");
            Opciones = op;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
