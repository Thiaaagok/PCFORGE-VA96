using PCFORGE_ValdezThiago_96VA.Properties;
using SER;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    /// <summary>
    /// Pantalla de inicio de PC Forge: logo, nombre del sistema y módulos. Se dibuja por código,
    /// sigue el tema claro/oscuro y el idioma activo (claves home_* y menu_grp_* de Resources\Idiomas).
    /// </summary>
    [DesignerCategory("Code")]
    public class HomeControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private const TextFormatFlags FlagsMultilinea =
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.Left |
            TextFormatFlags.Top | TextFormatFlags.NoPrefix;

        private readonly Font _fTitulo = new Font("Segoe UI Semibold", 22f, FontStyle.Bold);
        private readonly Font _fSubtitulo = new Font("Segoe UI", 11.5f);
        private readonly Font _fChip = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);

        private readonly Image _logo;
        private readonly Color _fondoLogo = Tema.Grafito950;

        private string _titulo = "";
        private string _subtitulo = "";
        private string[] _modulos = new string[0];

        public HomeControl06AV()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            _logo = Resources.pcforge_logo;

            // El logo viene con fondo oscuro propio: la tarjeta toma ese mismo color para que no se note el borde.
            var bmp = _logo as Bitmap;
            if (bmp != null && bmp.Width > 4 && bmp.Height > 4)
            {
                Color c = bmp.GetPixel(3, 3);
                if (c.A == 255) _fondoLogo = c;
            }

            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Tema.TemaChanged += RefrescarTema;
            Disposed += (s, e) =>
            {
                GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
                Tema.TemaChanged -= RefrescarTema;
                _fTitulo.Dispose(); _fSubtitulo.Dispose(); _fChip.Dispose();
            };
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            _titulo = t.Obtener("home_titulo");
            _subtitulo = t.Obtener("home_subtitulo");
            _modulos = new[]
            {
                t.Obtener("menu_grp_admin"),
                t.Obtener("menu_grp_maestro"),
                t.Obtener("menu_grp_compra"),
                t.Obtener("menu_grp_venta"),
                t.Obtener("menu_grp_produccion"),
                t.Obtener("menu_grp_reportes")
            };
            Invalidate();
        }

        private void RefrescarTema()
        {
            BackColor = Tema.FondoApp;
            Invalidate();
        }

        private int S(int valor)
        {
            return (int)Math.Round(valor * DeviceDpi / 96.0);
        }

        // ── Dibujo ──────────────────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Tema.FondoApp);

            int w = Math.Min(Width - S(32), S(1080));
            if (w < S(420) || Height < S(260)) return;

            int x = (Width - w) / 2;
            int heroH = Math.Min(Height - S(32), Math.Max(S(260), (int)(Height * 0.62)));
            int y = (Height - heroH) / 2;

            DibujarHero(g, new Rectangle(x, y, w, heroH));
        }

        private void DibujarHero(Graphics g, Rectangle r)
        {
            using (GraphicsPath path = Redondeado(r, S(20)))
            {
                using (var fondo = new SolidBrush(_fondoLogo)) g.FillPath(fondo, path);
                using (var borde = new Pen(Color.FromArgb(70, Tema.Cian400), 1f)) g.DrawPath(borde, path);
            }

            int pad = S(28);
            int logoH = r.Height - 2 * S(18);
            int logoW = logoH;
            if (_logo != null)
            {
                logoW = (int)(logoH * (double)_logo.Width / _logo.Height);
                int maxW = (int)(r.Width * 0.42);
                if (logoW > maxW)
                {
                    logoW = maxW;
                    logoH = (int)(logoW * (double)_logo.Height / _logo.Width);
                }
            }
            var logoRect = new Rectangle(r.X + pad, r.Y + (r.Height - logoH) / 2, logoW, logoH);
            if (_logo != null) g.DrawImage(_logo, logoRect);

            int tx = logoRect.Right + S(36);
            int tw = r.Right - pad - tx;
            if (tw < S(120)) return;

            // separador vertical fino entre el logo y el texto
            using (var sep = new Pen(Color.FromArgb(50, Tema.Acero300), 1f))
                g.DrawLine(sep, tx - S(18), r.Y + S(36), tx - S(18), r.Bottom - S(36));

            Size szTitulo = Medir(g, _titulo, _fTitulo, tw);
            Size szSub = Medir(g, _subtitulo, _fSubtitulo, tw);
            int barraH = S(4), g1 = S(14), g2 = S(16);
            int chipsH = FlujoChips(g, tx, 0, tw, false);
            int bloque = szTitulo.Height + g1 + barraH + g1 + szSub.Height + g2 + chipsH;
            int ty = r.Y + Math.Max(S(10), (r.Height - bloque) / 2);

            TextRenderer.DrawText(g, _titulo, _fTitulo, new Rectangle(tx, ty, tw, szTitulo.Height), Color.White, FlagsMultilinea);
            ty += szTitulo.Height + g1;

            var barra = new Rectangle(tx, ty, S(72), barraH);
            using (GraphicsPath pb = Redondeado(barra, barraH / 2))
            using (var grad = new LinearGradientBrush(barra, Tema.Cian400, Tema.Naranja500, LinearGradientMode.Horizontal))
                g.FillPath(grad, pb);
            ty += barraH + g1;

            TextRenderer.DrawText(g, _subtitulo, _fSubtitulo, new Rectangle(tx, ty, tw, szSub.Height), Tema.Acero300, FlagsMultilinea);
            ty += szSub.Height + g2;

            FlujoChips(g, tx, ty, tw, true);
        }

        /// <summary>Dibuja (o solo mide) las "pastillas" de módulos, con salto de línea. Devuelve el alto total.</summary>
        private int FlujoChips(Graphics g, int x, int y, int anchoMax, bool dibujar)
        {
            int altoChip = S(26), sepX = S(8), sepY = S(8), padX = S(12);
            int cx = x, cy = y;
            for (int i = 0; i < _modulos.Length; i++)
            {
                string texto = _modulos[i];
                int ancho = TextRenderer.MeasureText(g, texto, _fChip, new Size(int.MaxValue / 2, altoChip),
                                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + 2 * padX;
                if (cx > x && cx + ancho > x + anchoMax)
                {
                    cx = x;
                    cy += altoChip + sepY;
                }
                if (dibujar)
                {
                    var rc = new Rectangle(cx, cy, ancho, altoChip);
                    using (GraphicsPath p = Redondeado(rc, altoChip / 2))
                    {
                        using (var fill = new SolidBrush(Color.FromArgb(34, Tema.Cian400))) g.FillPath(fill, p);
                        using (var pen = new Pen(Color.FromArgb(150, Tema.Cian400), 1f)) g.DrawPath(pen, p);
                    }
                    TextRenderer.DrawText(g, texto, _fChip, rc, Tema.Cian400,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                }
                cx += ancho + sepX;
            }
            return _modulos.Length == 0 ? 0 : (cy - y) + altoChip;
        }

        // ── Utilidades ──────────────────────────────────────────────────────────────────────
        private static Size Medir(Graphics g, string texto, Font fuente, int ancho)
        {
            return TextRenderer.MeasureText(g, texto, fuente, new Size(ancho, int.MaxValue / 2), FlagsMultilinea);
        }

        private static GraphicsPath Redondeado(Rectangle r, int radio)
        {
            int d = Math.Max(2, radio * 2);
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
