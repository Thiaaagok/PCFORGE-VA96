using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.Controles;
using SER;
using SER.Excepciones;
using SER.Integridad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public partial class FRMLogin : Form
    {
        public FRMLogin()
        {
            InitializeComponent();
            AplicarTema();
            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            HabilitarArrastreVentana(this);
            HabilitarArrastreVentana(lblMarca);
            HabilitarArrastreVentana(lblEslogan);
        }

        private static readonly Color ColorPanelMarca   = Color.FromArgb(143, 37, 12);
        private static readonly Color ColorPanelAcceso  = Color.FromArgb(255, 164, 61);
        private static readonly Color ColorTriangulo    = Color.FromArgb(187, 77, 0);
        private static readonly Color ColorCampo        = Color.FromArgb(217, 217, 217);
        private static readonly Color ColorBotonAcceso  = Color.FromArgb(202, 83, 16);
        private static readonly Color ColorBotonCerrar  = Color.FromArgb(247, 178, 99);
        private static readonly Color ColorTextoTarjeta = Color.Black;

        private static readonly Point[] TrianguloSuperior = { new Point(70, -181), new Point(205, 41), new Point(0, 202) };
        private static readonly Point[] TrianguloInferior = { new Point(1154, 377), new Point(909, 465), new Point(967, 721) };

        private const string FamiliaDisenio = "Jaini Purva";
        private static PrivateFontCollection _fuentesPrivadas;

        internal static FontFamily FamiliaTitulos(out bool privada)
        {
            privada = false;
            using (var instaladas = new InstalledFontCollection())
            {
                var instalada = instaladas.Families.FirstOrDefault(f => f.Name == FamiliaDisenio);
                if (instalada != null) return instalada;
            }

            try
            {
                if (_fuentesPrivadas == null)
                {
                    string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fuentes", "JainiPurva-Regular.ttf");
                    if (!File.Exists(ruta)) return null;
                    _fuentesPrivadas = new PrivateFontCollection();
                    _fuentesPrivadas.AddFontFile(ruta);
                    // PrivateFontCollection solo alcanza a GDI+; los TextBox dibujan con GDI y necesitan la fuente registrada en el proceso.
                    AddFontResourceEx(ruta, FR_PRIVATE, IntPtr.Zero);
                }
                privada = true;
                return _fuentesPrivadas.Families.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            int mitad = ClientSize.Width / 2;

            using (var b = new SolidBrush(ColorPanelMarca)) g.FillRectangle(b, 0, 0, mitad, ClientSize.Height);
            using (var b = new SolidBrush(ColorPanelAcceso)) g.FillRectangle(b, mitad, 0, ClientSize.Width - mitad, ClientSize.Height);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(ColorTriangulo))
            {
                g.FillPolygon(b, TrianguloSuperior);
                g.FillPolygon(b, TrianguloInferior);
            }
        }

        private const uint FR_PRIVATE = 0x10;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        private const string IconoOjo = "";
        private const string IconoOjoTachado = "";

        private void AlternarVerContrasenia(object sender, EventArgs e)
        {
            bool oculta = ContraseniaTextBox.PasswordChar != '\0';
            ContraseniaTextBox.PasswordChar = oculta ? '\0' : '*';
            btnVerContrasenia.Text = oculta ? IconoOjoTachado : IconoOjo;
            ContraseniaTextBox.Focus();
            ContraseniaTextBox.SelectionStart = ContraseniaTextBox.TextLength;
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void HabilitarArrastreVentana(Control control)
        {
            if (control == null) return;
            control.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            };
        }

        private void AplicarTema()
        {
            DoubleBuffered = true;
            BackColor = ColorPanelMarca;

            var familia = FamiliaTitulos(out bool privada);
            Font Titulo(float px) => familia != null
                ? new Font(familia, px, FontStyle.Regular, GraphicsUnit.Pixel)
                : new Font("Segoe UI Semibold", px * 0.8f, FontStyle.Bold, GraphicsUnit.Pixel);

            lblMarca.Font = Titulo(96f);
            lblMarca.ForeColor = Color.White;
            lblEslogan.Font = Titulo(36f);
            lblEslogan.ForeColor = Color.White;

            pnlTarjeta.BackColor = Color.White;
            lblBienvenidos.Font = Titulo(36f);
            lblBienvenidos.ForeColor = ColorTextoTarjeta;

            foreach (var lbl in new[] { Usuario, materialLabel2 })
            {
                lbl.Font = Titulo(24f);
                lbl.ForeColor = ColorTextoTarjeta;
                lbl.BackColor = Color.White;
            }

            foreach (var campo in new[] { pnlCampoUsuario, pnlCampoContrasenia })
                campo.BackColor = ColorCampo;
            foreach (var txt in new[] { LoginTextBox, ContraseniaTextBox })
            {
                txt.BackColor = ColorCampo;
                txt.ForeColor = ColorTextoTarjeta;
                txt.Font = familia != null
                    ? new Font(familia, 20f, FontStyle.Regular, GraphicsUnit.Pixel)
                    : new Font("Segoe UI", 15f, FontStyle.Regular, GraphicsUnit.Pixel);
            }

            btnVerContrasenia.Text = IconoOjo;
            btnVerContrasenia.Font = new Font("Segoe MDL2 Assets", 18f, FontStyle.Regular, GraphicsUnit.Pixel);
            btnVerContrasenia.ForeColor = Color.FromArgb(80, 80, 80);
            btnVerContrasenia.BackColor = ColorCampo;
            btnVerContrasenia.FlatStyle = FlatStyle.Flat;
            btnVerContrasenia.FlatAppearance.BorderSize = 0;
            btnVerContrasenia.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(ColorCampo, 0.05f);
            btnVerContrasenia.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(ColorCampo, 0.1f);
            btnVerContrasenia.UseVisualStyleBackColor = false;
            btnVerContrasenia.Cursor = Cursors.Hand;

            EstilizarBoton(IniciarSesionBTN, ColorBotonAcceso, Titulo(24f));
            EstilizarBoton(CerrarBTN, ColorBotonCerrar, Titulo(24f));

            if (privada)
            {
                foreach (var lbl in new[] { lblMarca, lblEslogan, lblBienvenidos, Usuario, materialLabel2 })
                    lbl.UseCompatibleTextRendering = true;
                IniciarSesionBTN.UseCompatibleTextRendering = true;
                CerrarBTN.UseCompatibleTextRendering = true;
            }
        }

        private static void EstilizarBoton(Button boton, Color fondo, Font fuente)
        {
            boton.BackColor = fondo;
            boton.ForeColor = Color.Black;
            boton.Font = fuente;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.BorderColor = fondo;
            boton.FlatAppearance.MouseOverBackColor = ControlPaint.Light(fondo, 0.15f);
            boton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(fondo, 0.05f);
            boton.UseVisualStyleBackColor = false;
            boton.Cursor = Cursors.Hand;
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            this.Text                  = t.Obtener("login");
            lblBienvenidos.Text        = t.Obtener("login_bienvenidos");
            lblEslogan.Text            = t.Obtener("login_eslogan");
            Usuario.Text               = t.Obtener("usuario_label");
            materialLabel2.Text        = t.Obtener("contrasenia_label");
            IniciarSesionBTN.Text      = t.Obtener("iniciar_sesion");
            CerrarBTN.Text             = t.Obtener("cerrar").ToUpperInvariant();
        }

        private void Login_Load(object sender, EventArgs e)
        {
        }

        private void IniciarSesionBTN_Click(object sender, EventArgs e)
        {
            string login = LoginTextBox.Text;
            string contrasenia = ContraseniaTextBox.Text;

            try
            {
                var integridad = new IntegridadBLL06AV();
                ResultadoVerificacion06AV revision = null;
                try { revision = integridad.Verificar(); }
                catch { revision = null; }

                if (revision != null && revision.SinLineaBase)
                {
                    try { integridad.Recalcular(); } catch { }
                }
                else if (revision != null && !revision.EsConsistente)
                {
                    var ti = GestorIdioma06AV.Instancia;

                    if (new UsuariosBLL06AV().TienePatente(login, contrasenia, PatenteEnum06AV.RepararIntegridad))
                    {
                        using (var frm = new FRMReparacionDV(revision))
                            frm.ShowDialog(this);

                        LoginTextBox.Clear();
                        ContraseniaTextBox.Clear();
                        LoginTextBox.Focus();
                        return;
                    }

                    ConfirmacionForm.MostrarInfo(
                        ti.Obtener("login_inconsistencia_usuario"),
                        titulo: ti.Obtener("dv_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Error,
                        owner: this);
                    Close();
                    Application.Exit();
                    return;
                }

                UsuariosBLL06AV SER = new UsuariosBLL06AV();
                Usuario06AV usuario = SER.Login(login, contrasenia);

                AplicarIdioma();

                var t = GestorIdioma06AV.Instancia;

                if (usuario.DebeCambiarContrasenia)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("login_cambiar_pass_requerido"),
                        titulo: t.Obtener("cambio_pass_requerido_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this);

                    using (var f = new FRMCambiarContrasenia(usuario.Dni, esObligatorio: true))
                    {
                        f.ShowDialog(this);

                        if (!f.ContraseniaCambiada)
                        {
                            UsuarioSesion06AV.Instancia().CerrarSesion();
                            ConfirmacionForm.MostrarInfo(
                                t.Obtener("login_pass_obligatoria"),
                                titulo: t.Obtener("acceso_denegado"),
                                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                                owner: this);
                            return;
                        }

                        UsuarioSesion06AV.Instancia().CerrarSesion();
                        ConfirmacionForm.MostrarInfo(
                            t.Obtener("login_pass_actualizada"),
                            titulo: t.Obtener("pass_actualizada_titulo"),
                            tipo: ConfirmacionForm.TipoConfirmacion.Info,
                            owner: this);

                        LoginTextBox.Clear();
                        ContraseniaTextBox.Clear();
                        LoginTextBox.Focus();
                        return;
                    }
                }

                FRMMain formPrincipal = new FRMMain();
                formPrincipal.Show();
                this.Hide();
            }
            catch (UsuarioValidacionException ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("validacion"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this);
            }
            catch (SesionActivaException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("sesion_activa_bloqueo"),
                    titulo: t.Obtener("acceso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this);
            }
            catch (UsuarioNoEncontradoException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("login_o_pass_incorrectos"),
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (ContraseniaInvalidaException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("login_o_pass_incorrectos"),
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Bloqueado")
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("usuario_bloqueado_contacte"),
                    titulo: t.Obtener("acceso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Inactivo")
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("usuario_inactivo_contacte"),
                    titulo: t.Obtener("acceso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioAccesoDatosException ex)
            {
                var t = GestorIdioma06AV.Instancia;

                Exception real = ex;
                while (real.InnerException != null) real = real.InnerException;

                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_conexion_tarde") + "\n\n" + real.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (InvalidOperationException ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_permisos_inconsistentes") + "\n\n" + ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (Exception ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_conexion_tarde") + "\n\n" + ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
        }

        private void CerrarBTN_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

    }
}
