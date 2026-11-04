using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public partial class FRMMain
    {
        private const int IntervaloAvisosMs = 30 * 1000;

        private readonly Dictionary<string, Action> _navegacion = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private readonly AvisosBLL06AV _detectorAvisos = new AvisosBLL06AV();
        private readonly NotificacionesBLL06AV _notificacionesBLL = new NotificacionesBLL06AV();
        private List<Notificacion06AV> _notificaciones = new List<Notificacion06AV>();
        private int _ultimaNotificacionVista = -1;
        private Button btnAvisos;
        private ToolStripDropDown _popupAvisos;
        private ToolStripControlHost _hostAvisos;
        private PanelNotificaciones06AV _panelAvisos;
        private ToastNotificacion06AV _toastAvisos;
        private Timer _timerAvisos;

        private int NoLeidas => _notificaciones.Count(n => !n.Leida);

        private void CrearBotonAvisos()
        {
            if (btnAvisos != null) return;
            try { if (UsuarioSesion06AV.Instancia().UsuarioActual == null) return; }
            catch { return; }

            btnAvisos = new Button
            {
                Size = new Size(40, 34),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false,
                Text = ""
            };
            TematizarBotonAvisos();
            btnAvisos.Click += (s, e) => { ActualizarAvisos(); MostrarAvisos(); };
            btnAvisos.Paint += DibujarInsignia;

            _panelAvisos = new PanelNotificaciones06AV();
            _panelAvisos.NotificacionElegida += AbrirNotificacion;
            _panelAvisos.NotificacionLeida += n => MarcarLeidas(new[] { n });
            _panelAvisos.TodasLeidas += () => MarcarLeidas(_notificaciones.Where(n => !n.Leida).ToList());

            _hostAvisos = new ToolStripControlHost(_panelAvisos) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false };
            _popupAvisos = new ToolStripDropDown();
            _popupAvisos.Items.Add(_hostAvisos);
            RenderizadorMenu06AV.Aplicar(_popupAvisos);
            _popupAvisos.Padding = new Padding(1);

            _toastAvisos = new ToastNotificacion06AV { Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            _toastAvisos.Click += (s, e) => { _toastAvisos.Ocultar(); MostrarAvisos(); };
            Controls.Add(_toastAvisos);
            UbicarToast();
            Resize += (s, e) => UbicarToast();

            flpTopActions.Controls.Add(btnAvisos);
            flpTopActions.Controls.SetChildIndex(btnAvisos, 0);
            ActualizarTooltipAvisos();
        }

        private void UbicarToast()
        {
            if (_toastAvisos == null) return;
            _toastAvisos.Location = new Point(ClientSize.Width - _toastAvisos.Width - 24, ClientSize.Height - _toastAvisos.Height - 24);
        }

        private void TematizarBotonAvisos()
        {
            if (btnAvisos == null) return;
            ConfigurarBotonTopbar(btnAvisos, Tema.FondoSidebar, Tema.Texto);
            btnAvisos.Font = new Font("Segoe MDL2 Assets", 12f);
            if (_popupAvisos != null) RenderizadorMenu06AV.Aplicar(_popupAvisos);
            _panelAvisos?.Invalidate();
            _toastAvisos?.Invalidate();
            btnAvisos.Invalidate();
        }

        private void ActualizarTooltipAvisos()
        {
            if (btnAvisos == null) return;
            var t = GestorIdioma06AV.Instancia;
            int total = NoLeidas;
            toolTipMain.SetToolTip(btnAvisos, total == 0 ? t.Obtener("notif_ninguna_nueva") : t.Obtener("notif_tooltip", total));
            _panelAvisos?.Invalidate();
        }

        private void IniciarAvisos()
        {
            if (btnAvisos == null) return;
            _timerAvisos = new Timer { Interval = IntervaloAvisosMs };
            _timerAvisos.Tick += (s, e) => ActualizarAvisos();
            _timerAvisos.Start();
            FormClosed += (s, e) => { _timerAvisos.Stop(); _timerAvisos.Dispose(); };

            Shown += (s, e) =>
            {
                _detectorAvisos.Detectar(forzar: true);
                ActualizarAvisos();
            };
        }

        private void ActualizarAvisos()
        {
            if (btnAvisos == null) return;
            try { _detectorAvisos.Detectar(); } catch { }
            try { _notificaciones = _notificacionesBLL.ObtenerParaUsuario(); }
            catch { return; }

            var nuevas = _notificaciones.Where(n => !n.Leida && n.Id > _ultimaNotificacionVista).ToList();
            // La primera carga solo marca la referencia, salvo que haya pendientes: entonces se avisa una vez al entrar.
            if (nuevas.Count > 0 && _toastAvisos != null && !(_popupAvisos?.Visible ?? false))
                _toastAvisos.Mostrar(nuevas[0], nuevas.Count - 1);
            if (_notificaciones.Count > 0)
                _ultimaNotificacionVista = Math.Max(_ultimaNotificacionVista, _notificaciones.Max(n => n.Id));

            if (_popupAvisos != null && _popupAvisos.Visible) CargarPanelAvisos();
            ActualizarTooltipAvisos();
            btnAvisos.Invalidate();
        }

        private void CargarPanelAvisos()
        {
            _panelAvisos.Cargar(_notificaciones);
            _hostAvisos.Size = _panelAvisos.Size;
        }

        private void DibujarInsignia(object sender, PaintEventArgs e)
        {
            int n = NoLeidas;
            if (n == 0) return;
            string texto = n > 9 ? "9+" : n.ToString();
            bool urgente = _notificaciones.Any(a => !a.Leida && a.Urgente);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fuente = new Font("Segoe UI", 6.75f, FontStyle.Bold))
            {
                Size medida = TextRenderer.MeasureText(texto, fuente);
                int ancho = Math.Max(15, medida.Width - 2);
                var r = new Rectangle(btnAvisos.Width - ancho - 2, 1, ancho, 15);
                using (var b = new SolidBrush(urgente ? Tema.Peligro : Tema.Acento))
                    g.FillEllipse(b, r);
                TextRenderer.DrawText(g, texto, fuente, r, urgente || !Tema.EsOscuro ? Color.White : Tema.Grafito950,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void MostrarAvisos()
        {
            if (btnAvisos == null || _popupAvisos == null || !btnAvisos.IsHandleCreated) return;
            _toastAvisos?.Ocultar();
            CargarPanelAvisos();
            _popupAvisos.Show(btnAvisos, new Point(btnAvisos.Width, btnAvisos.Height + 6), ToolStripDropDownDirection.BelowLeft);
        }

        private void MarcarLeidas(IList<Notificacion06AV> notificaciones)
        {
            var pendientes = notificaciones.Where(n => !n.Leida).ToList();
            if (pendientes.Count == 0) return;
            try { _notificacionesBLL.MarcarLeidas(pendientes.Select(n => n.Id)); }
            catch { return; }
            foreach (var n in pendientes) n.Leida = true;

            if (_popupAvisos.Visible) CargarPanelAvisos();
            ActualizarTooltipAvisos();
            btnAvisos.Invalidate();
        }

        private void AbrirNotificacion(Notificacion06AV n)
        {
            MarcarLeidas(new[] { n });
            _popupAvisos.Close();
            if (!string.IsNullOrEmpty(n.Destino) && _navegacion.TryGetValue(n.Destino, out Action ir)) ir();
        }
    }
}
