using BE;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal class ToastNotificacion06AV : Control
    {
        private const int DuracionMs = 6000;
        private readonly Timer _timer = new Timer { Interval = DuracionMs };
        private Notificacion06AV _notificacion;
        private int _otras;
        private bool _hot;

        public ToastNotificacion06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(350, 76);
            Cursor = Cursors.Hand;
            Visible = false;
            _timer.Tick += (s, e) => Ocultar();
        }

        public void Mostrar(Notificacion06AV n, int otras)
        {
            _notificacion = n;
            _otras = otras;
            _timer.Stop();
            Visible = true;
            BringToFront();
            Invalidate();
            _timer.Start();
        }

        public void Ocultar()
        {
            _timer.Stop();
            Visible = false;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; _timer.Stop(); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; _timer.Start(); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_notificacion == null) return;
            var g = e.Graphics;
            Pintura06AV.Calidad(g);
            g.Clear(Parent?.BackColor ?? Tema.FondoApp);

            var caja = new Rectangle(0, 0, Width - 1, Height - 1);
            Pintura06AV.Rellenar(g, caja, 12, _hot ? Tema.SidebarHover : Tema.FondoSidebar);
            Pintura06AV.Borde(g, caja, 12, Tema.BordeSidebar);
            Color tinta = _notificacion.Urgente ? Tema.Peligro : Tema.Acento;
            Pintura06AV.Rellenar(g, new Rectangle(0, 12, 4, Height - 24), 2, tinta);

            var circulo = new Rectangle(16, (Height - 36) / 2, 36, 36);
            Pintura06AV.Rellenar(g, circulo, 18, Pintura06AV.Suave(tinta, 40));
            using (var fIcono = new Font("Segoe MDL2 Assets", 15f, FontStyle.Regular, GraphicsUnit.Pixel))
                Pintura06AV.TextoCentrado(g, PanelNotificaciones06AV.Icono(_notificacion), fIcono, tinta, circulo);

            var t = GestorIdioma06AV.Instancia;
            string encabezado = _otras > 0 ? t.Obtener("notif_toast_varias", _otras + 1) : t.Obtener("notif_toast_nueva");
            int x = circulo.Right + 12;
            using (var fMini = new Font("Segoe UI Semibold", 7.75f, FontStyle.Bold))
                Pintura06AV.TextoIzquierda(g, encabezado.ToUpperInvariant(), fMini, tinta, new Rectangle(x, 10, Width - x - 12, 16));
            using (var f = new Font("Segoe UI", 9.25f))
            using (var b = new SolidBrush(Tema.TextoFuerte))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord })
                g.DrawString(PanelNotificaciones06AV.Titulo(_notificacion), f, b, new RectangleF(x, 28, Width - x - 12, Height - 34), sf);
        }
    }
}
