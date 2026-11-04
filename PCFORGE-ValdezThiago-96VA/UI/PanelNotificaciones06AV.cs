using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal class PanelNotificaciones06AV : Control
    {
        public const int Ancho = 380;
        private const int AltoEncabezado = 52;
        private const int AltoFila = 66;
        private const int AltoMaximo = 470;
        private const int AnchoCheck = 36;

        private List<Notificacion06AV> _items = new List<Notificacion06AV>();
        private int _desplazamiento;
        private int _hover = -1;
        private bool _hoverCheck;
        private bool _hoverTodas;
        private Rectangle _rectTodas;

        public event Action<Notificacion06AV> NotificacionElegida;
        public event Action<Notificacion06AV> NotificacionLeida;
        public event Action TodasLeidas;

        public PanelNotificaciones06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Width = Ancho;
            Cursor = Cursors.Default;
        }

        public void Cargar(List<Notificacion06AV> items)
        {
            _items = items ?? new List<Notificacion06AV>();
            int filas = Math.Max(1, _items.Count);
            Height = Math.Min(AltoMaximo, AltoEncabezado + filas * AltoFila + 8);
            _desplazamiento = Math.Min(_desplazamiento, MaximoDesplazamiento());
            Invalidate();
        }

        public static string Titulo(Notificacion06AV n) =>
            GestorIdioma06AV.Instancia.Obtener("notif_" + n.Tipo, n.Datos.Cast<object>().ToArray());

        public static string Antiguedad(DateTime fecha)
        {
            var t = GestorIdioma06AV.Instancia;
            TimeSpan d = DateTime.Now - fecha;
            if (d.TotalMinutes < 1) return t.Obtener("notif_hace_ahora");
            if (d.TotalHours < 1) return t.Obtener("notif_hace_min", (int)d.TotalMinutes);
            if (d.TotalDays < 1) return t.Obtener("notif_hace_h", (int)d.TotalHours);
            return t.Obtener("notif_hace_d", (int)d.TotalDays);
        }

        public static string Icono(Notificacion06AV n)
        {
            switch (n.Destino)
            {
                case NotificacionesBLL06AV.DestinoVentas: return "";
                case NotificacionesBLL06AV.DestinoEntregas: return "";
                case NotificacionesBLL06AV.DestinoProduccion: return "";
                case NotificacionesBLL06AV.DestinoCotizaciones: return "";
                default: return "";
            }
        }

        private int MaximoDesplazamiento() =>
            Math.Max(0, _items.Count * AltoFila + 8 - (Height - AltoEncabezado));

        private int FilaEn(Point p)
        {
            if (p.Y < AltoEncabezado) return -1;
            int i = (p.Y - AltoEncabezado + _desplazamiento) / AltoFila;
            return i >= 0 && i < _items.Count ? i : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int fila = FilaEn(e.Location);
            bool check = fila >= 0 && e.X > Width - AnchoCheck - 8 && !_items[fila].Leida;
            bool todas = _rectTodas.Contains(e.Location);
            if (fila != _hover || check != _hoverCheck || todas != _hoverTodas)
            {
                _hover = fila; _hoverCheck = check; _hoverTodas = todas;
                Cursor = fila >= 0 || todas ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1; _hoverCheck = false; _hoverTodas = false;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _desplazamiento = Math.Max(0, Math.Min(MaximoDesplazamiento(), _desplazamiento - e.Delta * AltoFila / 120));
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (_rectTodas.Contains(e.Location)) { TodasLeidas?.Invoke(); return; }

            int fila = FilaEn(e.Location);
            if (fila < 0) return;
            var n = _items[fila];
            if (_hoverCheck) NotificacionLeida?.Invoke(n);
            else NotificacionElegida?.Invoke(n);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Pintura06AV.Calidad(g);
            g.Clear(Tema.FondoSidebar);
            var t = GestorIdioma06AV.Instancia;

            using (var fTitulo = new Font("Segoe UI Semibold", 11f, FontStyle.Bold))
                Pintura06AV.TextoIzquierda(g, t.Obtener("notif_titulo"), fTitulo, Tema.TextoFuerte, new Rectangle(16, 0, 200, AltoEncabezado));

            int noLeidas = _items.Count(n => !n.Leida);
            _rectTodas = Rectangle.Empty;
            if (noLeidas > 0)
            {
                using (var fLink = new Font("Segoe UI", 8.75f, _hoverTodas ? FontStyle.Underline : FontStyle.Regular))
                {
                    string texto = t.Obtener("notif_marcar_todas");
                    int ancho = (int)Math.Ceiling(g.MeasureString(texto, fLink).Width) + 4;
                    _rectTodas = new Rectangle(Width - ancho - 14, 14, ancho, AltoEncabezado - 28);
                    Pintura06AV.TextoDerecha(g, texto, fLink, Tema.Acento, _rectTodas);
                }
            }
            using (var p = new Pen(Tema.BordeSidebar))
                g.DrawLine(p, 0, AltoEncabezado - 1, Width, AltoEncabezado - 1);

            var area = new Rectangle(0, AltoEncabezado, Width, Height - AltoEncabezado);
            g.SetClip(area);

            if (_items.Count == 0)
            {
                using (var f = new Font("Segoe UI", 9.5f))
                    Pintura06AV.TextoCentrado(g, "✓  " + t.Obtener("notif_ninguna"), f, Tema.TextoSuave, area);
                return;
            }

            using (var fIcono = new Font("Segoe MDL2 Assets", 14f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var fCheck = new Font("Segoe MDL2 Assets", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var fTexto = new Font("Segoe UI", 9.25f))
            using (var fTextoNuevo = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold))
            using (var fMini = new Font("Segoe UI", 8f))
            using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord })
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    int y = AltoEncabezado + i * AltoFila - _desplazamiento + 4;
                    if (y + AltoFila < AltoEncabezado || y > Height) continue;
                    var n = _items[i];
                    var fila = new Rectangle(6, y, Width - 12, AltoFila - 4);

                    if (i == _hover) Pintura06AV.Rellenar(g, fila, 8, Tema.SidebarHover);
                    else if (!n.Leida) Pintura06AV.Rellenar(g, fila, 8, Pintura06AV.Suave(Tema.SidebarActivo, 120));

                    Color tinta = n.Urgente ? Tema.Peligro : Tema.Acento;
                    var circulo = new Rectangle(fila.X + 10, fila.Y + 12, 34, 34);
                    Pintura06AV.Rellenar(g, circulo, 17, Pintura06AV.Suave(tinta, 40));
                    Pintura06AV.TextoCentrado(g, Icono(n), fIcono, tinta, circulo);

                    int x = circulo.Right + 12;
                    int anchoTexto = fila.Right - x - AnchoCheck;
                    using (var b = new SolidBrush(n.Leida ? Tema.Texto : Tema.TextoFuerte))
                        g.DrawString(Titulo(n), n.Leida ? fTexto : fTextoNuevo, b, new RectangleF(x, fila.Y + 8, anchoTexto, 36), sf);
                    Pintura06AV.TextoIzquierda(g, Antiguedad(n.Fecha), fMini, Tema.TextoSuave,
                                               new Rectangle(x, fila.Bottom - 22, anchoTexto, 16));

                    if (n.Leida) continue;
                    var zonaCheck = new Rectangle(fila.Right - AnchoCheck, fila.Y, AnchoCheck - 6, fila.Height);
                    if (i == _hover)
                    {
                        var boton = new Rectangle(zonaCheck.X + (zonaCheck.Width - 26) / 2, zonaCheck.Y + (zonaCheck.Height - 26) / 2, 26, 26);
                        if (_hoverCheck) Pintura06AV.Rellenar(g, boton, 13, Tema.SidebarActivo);
                        Pintura06AV.TextoCentrado(g, "", fCheck, _hoverCheck ? Tema.TextoFuerte : Tema.TextoSuave, boton);
                    }
                    else
                    {
                        var punto = new Rectangle(zonaCheck.X + zonaCheck.Width / 2 - 4, zonaCheck.Y + zonaCheck.Height / 2 - 4, 8, 8);
                        Pintura06AV.Rellenar(g, punto, 4, Tema.Acento);
                    }
                }
            }

            int maximo = MaximoDesplazamiento();
            if (maximo > 0)
            {
                int vista = area.Height;
                int alto = Math.Max(28, vista * vista / (vista + maximo));
                int top = area.Y + (vista - alto) * _desplazamiento / maximo;
                Pintura06AV.Rellenar(g, new Rectangle(Width - 5, top, 3, alto), 2, Tema.SidebarLinea);
            }
        }
    }
}
