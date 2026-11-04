using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal class ItemSidebar06AV : Control
    {
        public const int AltoRaiz = 42;
        public const int AltoHijo = 34;
        private const int CentroIcono = 22;
        private const int InicioTexto = 46;
        private const int InicioHijo = 40;

        private bool _hot;
        private bool _activo;
        private bool _expandido;
        private bool _colapsado;

        public ItemSidebar06AV(string texto, string icono, bool esHijo)
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Texto = texto;
            Icono = icono;
            EsHijo = esHijo;
            Height = esHijo ? AltoHijo : AltoRaiz;
            Margin = new Padding(0, esHijo ? 0 : 2, 0, esHijo ? 0 : 2);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public string Texto { get; set; }
        public string Icono { get; }
        public bool EsHijo { get; }
        public bool EsGrupo { get; set; }
        public bool EsUltimoHijo { get; set; }
        public ItemSidebar06AV Padre { get; set; }

        public bool Activo { get { return _activo; } set { if (_activo == value) return; _activo = value; Invalidate(); } }
        public bool Expandido { get { return _expandido; } set { if (_expandido == value) return; _expandido = value; Invalidate(); } }
        public bool Colapsado { get { return _colapsado; } set { if (_colapsado == value) return; _colapsado = value; Invalidate(); } }

        public void InvokeClick() => OnClick(EventArgs.Empty);

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Pintura06AV.Calidad(g);
            g.Clear(Parent?.BackColor ?? Tema.FondoSidebar);

            if (EsHijo) PintarHijo(g);
            else PintarRaiz(g);
        }

        private void PintarRaiz(Graphics g)
        {
            Rectangle caja = _colapsado
                ? new Rectangle((Width - AltoRaiz) / 2, 0, AltoRaiz, AltoRaiz)
                : new Rectangle(0, 0, Width, Height);

            Color fondo = _activo ? Tema.SidebarActivo : _hot ? Tema.SidebarHover : Color.Empty;
            if (fondo != Color.Empty) Pintura06AV.Rellenar(g, caja, 10, fondo);

            Color tinta = _activo ? Tema.TextoFuerte : Tema.Texto;
            using (var fIcono = new Font("Segoe MDL2 Assets", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                var rIcono = _colapsado ? caja : new Rectangle(CentroIcono - 12, 0, 24, Height);
                Pintura06AV.TextoCentrado(g, Icono, fIcono, _activo ? Tema.Acento : Tema.TextoSuave, rIcono);
            }
            if (_colapsado) return;

            using (var fTexto = new Font("Segoe UI", 13f, _activo ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
                Pintura06AV.TextoIzquierda(g, Texto, fTexto, tinta, new Rectangle(InicioTexto, 0, Width - InicioTexto - 28, Height));

            if (EsGrupo)
                using (var fChevron = new Font("Segoe MDL2 Assets", 9f, FontStyle.Regular, GraphicsUnit.Pixel))
                    Pintura06AV.TextoCentrado(g, _expandido ? "" : "", fChevron, Tema.TextoSuave,
                                              new Rectangle(Width - 28, 0, 20, Height));
        }

        private void PintarHijo(Graphics g)
        {
            // La línea del árbol sigue hacia abajo salvo en el último hijo, que la cierra con una curva.
            using (var p = new Pen(Tema.SidebarLinea, 1f))
            {
                int medio = Height / 2;
                int radio = 8;
                g.DrawLine(p, CentroIcono, 0, CentroIcono, EsUltimoHijo ? medio - radio : Height);
                using (var codo = new GraphicsPath())
                {
                    codo.AddArc(CentroIcono, medio - radio * 2, radio * 2, radio * 2, 180, -90);
                    g.DrawPath(p, codo);
                }
                g.DrawLine(p, CentroIcono + radio, medio, InicioHijo - 2, medio);
            }

            var pastilla = new Rectangle(InicioHijo, 3, Width - InicioHijo, Height - 6);
            Color fondo = _activo ? Tema.SidebarActivo : _hot ? Tema.SidebarHover : Color.Empty;
            if (fondo != Color.Empty) Pintura06AV.Rellenar(g, pastilla, 8, fondo);

            Color tinta = _activo ? Tema.TextoFuerte : _hot ? Tema.Texto : Tema.TextoSuave;
            using (var f = new Font("Segoe UI", 12f, _activo ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
                Pintura06AV.TextoIzquierda(g, Texto, f, tinta, new Rectangle(InicioHijo + 12, 0, Width - InicioHijo - 16, Height));
        }
    }
}
