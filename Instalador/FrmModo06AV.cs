using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Instalador
{
    // Reinstalador: se muestra cuando la base ya existe en la instancia elegida.
    public class FrmModo06AV : Form
    {
        private static readonly Color Primario = Color.FromArgb(21, 101, 192);
        private static readonly Color Texto = Color.FromArgb(33, 33, 33);
        private static readonly Color TextoSuave = Color.FromArgb(110, 110, 110);
        private static readonly Color Peligro = Color.FromArgb(198, 40, 40);

        private readonly OpcionesInstalacion06AV _opciones;

        private RadioButton rbReparar;
        private RadioButton rbReinstalar;
        private RadioButton rbRestaurar;
        private TextBox txtBak;
        private Button btnExaminar;
        private Button btnContinuar;

        public FrmModo06AV(OpcionesInstalacion06AV opciones, int tablas, int usuarios, string carpetaBackups)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            ConstruirUI(tablas, usuarios, carpetaBackups);
        }

        private void ConstruirUI(int tablas, int usuarios, string carpetaBackups)
        {
            Text = "Reinstalación - Sistema ya instalado";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 470);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var lblTitulo = new Label
            {
                Text = "El sistema ya está instalado",
                Font = new Font("Segoe UI Semibold", 15.75f, FontStyle.Bold),
                ForeColor = Primario,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            var lblInfo = new Label
            {
                Text = $"La base '{_opciones.BaseDatos}' existe en '{_opciones.Servidor}' " +
                       $"({tablas} tablas, {usuarios} usuarios).\r\n" +
                       "Antes de cualquier cambio se hace un backup automático en el servidor.",
                ForeColor = TextoSuave,
                Location = new Point(26, 60),
                Size = new Size(508, 40)
            };

            rbReparar = CrearOpcion("Reparar / actualizar (recomendado)", 112, true);
            var dReparar = CrearDescripcion(
                "Vuelve a ejecutar los scripts: crea tablas y procedimientos que falten y actualiza " +
                "los existentes. Conserva todos los datos.", 136);

            rbReinstalar = CrearOpcion("Reinstalar desde cero", 184, false);
            var dReinstalar = CrearDescripcion(
                "Borra la base y la crea de nuevo con los datos iniciales. Los datos actuales " +
                "quedan solo en el backup automático.", 208);
            dReinstalar.ForeColor = Peligro;

            rbRestaurar = CrearOpcion("Restaurar desde un backup (.bak)", 256, false);
            var dRestaurar = CrearDescripcion(
                "Reemplaza la base por el backup elegido y la actualiza a la versión actual. " +
                "El archivo debe ser accesible para el servicio de SQL Server.", 280);

            txtBak = new TextBox { Location = new Point(48, 326), Width = 376, ReadOnly = true, BackColor = Color.White };
            btnExaminar = new Button
            {
                Text = "Examinar...",
                Location = new Point(432, 325),
                Width = 102,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnExaminar.FlatAppearance.BorderColor = Color.FromArgb(189, 189, 189);
            btnExaminar.Click += (s, e) => ElegirBackup(carpetaBackups);

            var lblCarpeta = new Label
            {
                Text = "Carpeta de backups del servidor: " + (carpetaBackups ?? "(desconocida)"),
                ForeColor = TextoSuave,
                Location = new Point(48, 358),
                Size = new Size(486, 34),
                Font = new Font("Segoe UI", 8.5f)
            };

            rbReparar.CheckedChanged += (s, e) => ActualizarEstado();
            rbReinstalar.CheckedChanged += (s, e) => ActualizarEstado();
            rbRestaurar.CheckedChanged += (s, e) => ActualizarEstado();

            btnContinuar = new Button
            {
                Text = "Continuar",
                Location = new Point(322, 416),
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

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(438, 416),
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
                lblTitulo, lblInfo,
                rbReparar, dReparar, rbReinstalar, dReinstalar, rbRestaurar, dRestaurar,
                txtBak, btnExaminar, lblCarpeta, btnContinuar, btnCancelar
            });

            AcceptButton = btnContinuar;
            CancelButton = btnCancelar;
            ActualizarEstado();
        }

        private static RadioButton CrearOpcion(string texto, int y, bool marcado) => new RadioButton
        {
            Text = texto,
            Checked = marcado,
            AutoSize = true,
            Location = new Point(28, y),
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = Texto
        };

        private static Label CrearDescripcion(string texto, int y) => new Label
        {
            Text = texto,
            ForeColor = TextoSuave,
            Location = new Point(48, y),
            Size = new Size(486, 40)
        };

        private void ActualizarEstado()
        {
            bool restaurar = rbRestaurar.Checked;
            txtBak.Enabled = btnExaminar.Enabled = restaurar;
        }

        private void ElegirBackup(string carpetaBackups)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Elegir backup a restaurar",
                Filter = "Backup de SQL Server (*.bak)|*.bak|Todos los archivos (*.*)|*.*",
                CheckFileExists = false
            })
            {
                if (!string.IsNullOrEmpty(carpetaBackups) && Directory.Exists(carpetaBackups))
                    dlg.InitialDirectory = carpetaBackups;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtBak.Text = dlg.FileName;
            }
        }

        private void Continuar()
        {
            if (rbRestaurar.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtBak.Text))
                {
                    MessageBox.Show(this, "Elegí el archivo .bak a restaurar.",
                        "Restaurar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!Confirmar("La base actual se reemplazará por el contenido del backup elegido.\r\n" +
                               "Se hará un backup automático de la base actual antes.\r\n\r\n¿Continuar?"))
                    return;
                _opciones.Modo = ModoInstalacion06AV.Restaurar;
                _opciones.ArchivoRestore = txtBak.Text;
            }
            else if (rbReinstalar.Checked)
            {
                if (!Confirmar("Se BORRARÁ la base '" + _opciones.BaseDatos + "' y se creará de nuevo.\r\n" +
                               "Los datos actuales quedarán solo en el backup automático.\r\n\r\n¿Continuar?"))
                    return;
                _opciones.Modo = ModoInstalacion06AV.ReinstalarLimpio;
            }
            else
            {
                _opciones.Modo = ModoInstalacion06AV.Reparar;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool Confirmar(string mensaje) =>
            MessageBox.Show(this, mensaje, "Confirmar", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }
}
