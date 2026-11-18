using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Instalador
{
    public class FrmProgreso06AV : Form
    {
        private static readonly Color Primario = Color.FromArgb(21, 101, 192);
        private static readonly Color Ok = Color.FromArgb(46, 125, 50);
        private static readonly Color Error = Color.FromArgb(198, 40, 40);

        private readonly OpcionesInstalacion06AV _opciones;

        private Label lblTitulo;
        private ProgressBar barra;
        private TextBox txtLog;
        private CheckBox chkAcceso;
        private Button btnFinalizar;

        private string _exeApp;

        public bool Exito { get; private set; }

        public FrmProgreso06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            ConstruirUI();
            Shown += async (s, e) => await EjecutarInstalacionAsync();
        }

        private void ConstruirUI()
        {
            Text = new InstaladorBLL06AV(_opciones).Describir(_opciones.Modo) + " en curso";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ClientSize = new Size(560, 420);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            lblTitulo = new Label
            {
                Text = "Preparando la base de datos...",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Primario,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            barra = new ProgressBar
            {
                Location = new Point(26, 62),
                Width = 508,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            txtLog = new TextBox
            {
                Location = new Point(26, 96),
                Size = new Size(508, 270),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(245, 245, 245),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };

            chkAcceso = new CheckBox
            {
                Text = "Crear acceso directo en el Escritorio",
                Checked = _opciones.CrearAccesoDirecto,
                Visible = _opciones.CrearAccesoDirecto,
                AutoSize = true,
                Location = new Point(26, 380),
                Enabled = false
            };

            btnFinalizar = new Button
            {
                Text = "Finalizar",
                Location = new Point(438, 374),
                Width = 96,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Primario,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnFinalizar.FlatAppearance.BorderSize = 0;
            btnFinalizar.Click += (s, e) => Finalizar();

            Controls.AddRange(new Control[] { lblTitulo, barra, txtLog, chkAcceso, btnFinalizar });
        }

        private void Log(string mensaje)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), mensaje);
                return;
            }
            txtLog.AppendText(mensaje + Environment.NewLine);
        }

        private async Task EjecutarInstalacionAsync()
        {
            try
            {
                if (_opciones.InstalarSistema)
                {
                    lblTitulo.Text = "Instalando el sistema...";
                    _exeApp = await Task.Run(() => InstaladorSistema06AV.Instalar(_opciones.CarpetaInstalacion, Log));
                    lblTitulo.Text = "Preparando la base de datos...";
                }

                ResultadoInstalacion06AV resultado =
                    await Task.Run(() => new InstaladorBLL06AV(_opciones).Ejecutar(Log));

                ConfigurarAplicacion();

                Exito = true;
                Log("");
                if (resultado.RutaBackupPrevio != null)
                    Log("Backup previo guardado en el servidor: " + resultado.RutaBackupPrevio);
                if (_opciones.Modo == ModoInstalacion06AV.Instalar || _opciones.Modo == ModoInstalacion06AV.ReinstalarLimpio)
                    Log("Usuario inicial: admin / Admin1234 (se pide cambiarla en el primer ingreso).");
                Log("Operación finalizada con éxito.");
                lblTitulo.Text = "Operación completada";
                lblTitulo.ForeColor = Ok;
            }
            catch (Exception ex)
            {
                Exito = false;
                Log("");
                Log("ERROR: " + ex.Message);
                lblTitulo.Text = "La operación falló";
                lblTitulo.ForeColor = Error;
                chkAcceso.Checked = false;
                btnFinalizar.Text = "Cerrar";
            }
            finally
            {
                barra.Style = ProgressBarStyle.Continuous;
                barra.Value = 100;
                chkAcceso.Enabled = Exito && _exeApp != null;
                if (_exeApp == null) chkAcceso.Checked = false;
                btnFinalizar.Enabled = true;
                ControlBox = true;
            }
        }

        // Deja el sistema instalado apuntando a la base, con accesos del menú Inicio y
        // registrado en "Aplicaciones instaladas".
        private void ConfigurarAplicacion()
        {
            if (_exeApp == null)
            {
                // Modo solo-base: se usa lo que ya esté instalado, si existe.
                string instalado = InstaladorSistema06AV.RutaExe(_opciones.CarpetaInstalacion);
                if (!System.IO.File.Exists(instalado))
                {
                    Log("Aviso: el sistema no está instalado en " + _opciones.CarpetaInstalacion + ".");
                    return;
                }
                _exeApp = instalado;
            }

            Log(ConfiguradorApp06AV.EscribirCadenaConexion(_exeApp, _opciones.CadenaBaseDatos())
                ? "Conexión del sistema configurada."
                : "Aviso: no se pudo escribir la configuración del sistema en " + System.IO.Path.GetDirectoryName(_exeApp) + ".");

            if (_opciones.InstalarSistema)
            {
                string carpeta = System.IO.Path.GetDirectoryName(_exeApp);
                InstaladorSistema06AV.CrearAccesosMenuInicio(carpeta, Log);
                InstaladorSistema06AV.RegistrarDesinstalacion(carpeta, Log);
            }
        }

        private void Finalizar()
        {
            if (Exito && _exeApp != null && chkAcceso.Visible && chkAcceso.Checked)
            {
                string lnk = InstaladorSistema06AV.CrearAccesoEscritorio(System.IO.Path.GetDirectoryName(_exeApp), out string error);
                if (lnk == null)
                    MessageBox.Show(this, "No se pudo crear el acceso directo en el Escritorio:\n" + error,
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (Exito && _exeApp != null && _opciones.AbrirAlFinalizar &&
                MessageBox.Show(this, "La instalación terminó correctamente.\n\n¿Querés abrir el sistema ahora?",
                    "Instalación completada", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    InstaladorSistema06AV.AbrirSistema(_exeApp);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo abrir el sistema:\n" + ex.Message,
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            DialogResult = Exito ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }
    }
}
