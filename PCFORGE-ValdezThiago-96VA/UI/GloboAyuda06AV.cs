using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    // Globo flotante de la ayuda contextual: no roba el foco ni aparece en la barra de tareas.
    internal sealed class GloboAyuda06AV : Form
    {
        private const int AnchoMaximo = 340;
        private const int Margen = 12;

        private string _titulo = "";
        private string _texto = "";
        private string _pie = "";
        private readonly Timer _autoOcultar = new Timer { Interval = 12000 };

        public GloboAyuda06AV()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            _autoOcultar.Tick += (s, e) => Ocultar();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void Mostrar(TemaAyuda06AV tema, Point pantalla, string pie)
        {
            if (tema == null) { Ocultar(); return; }
            _titulo = tema.Titulo ?? "";
            _texto = tema.Texto ?? "";
            _pie = pie ?? "";

            Size tam = Medir();
            Rectangle area = Screen.FromPoint(pantalla).WorkingArea;
            int x = pantalla.X + 18, y = pantalla.Y + 22;
            if (x + tam.Width > area.Right) x = Math.Max(area.Left, pantalla.X - tam.Width - 8);
            if (y + tam.Height > area.Bottom) y = Math.Max(area.Top, pantalla.Y - tam.Height - 8);
            Bounds = new Rectangle(x, y, tam.Width, tam.Height);

            using (var path = Pintura06AV.Redondeado(new Rectangle(0, 0, Width, Height), 10))
            {
                var anterior = Region;
                Region = new Region(path);
                anterior?.Dispose();
            }

            if (!Visible) Show(); else Invalidate();
            _autoOcultar.Stop();
            _autoOcultar.Start();
        }

        public void Ocultar()
        {
            _autoOcultar.Stop();
            if (Visible) Hide();
        }

        private Size Medir()
        {
            int ancho = AnchoMaximo - 2 * Margen;
            using (var g = CreateGraphics())
            {
                SizeF t = g.MeasureString(_titulo, Tema.FuenteBold, ancho);
                SizeF c = g.MeasureString(_texto, Tema.FuenteRegular, ancho);
                SizeF p = string.IsNullOrEmpty(_pie) ? SizeF.Empty : g.MeasureString(_pie, Tema.FuenteMini, ancho);
                int w = (int)Math.Ceiling(Math.Max(Math.Max(t.Width, c.Width), p.Width)) + 2 * Margen + 6;
                int h = (int)Math.Ceiling(t.Height + c.Height + p.Height) + 2 * Margen + 10;
                return new Size(Math.Min(AnchoMaximo, Math.Max(180, w)), h);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Tema.FondoElevado);
            using (var b = new SolidBrush(Tema.Primario)) g.FillRectangle(b, 0, 0, 4, Height);
            using (var p = new Pen(Tema.Borde)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            int ancho = Width - 2 * Margen;
            float y = Margen;
            using (var bt = new SolidBrush(Tema.TextoFuerte))
            {
                SizeF t = g.MeasureString(_titulo, Tema.FuenteBold, ancho);
                g.DrawString(_titulo, Tema.FuenteBold, bt, new RectangleF(Margen, y, ancho, t.Height));
                y += t.Height + 4;
            }
            using (var bc = new SolidBrush(Tema.Texto))
            {
                SizeF c = g.MeasureString(_texto, Tema.FuenteRegular, ancho);
                g.DrawString(_texto, Tema.FuenteRegular, bc, new RectangleF(Margen, y, ancho, c.Height));
                y += c.Height + 6;
            }
            if (!string.IsNullOrEmpty(_pie))
                using (var bp = new SolidBrush(Tema.TextoSuave))
                    g.DrawString(_pie, Tema.FuenteMini, bp, new RectangleF(Margen, y, ancho, Height - y));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _autoOcultar.Dispose();
            base.Dispose(disposing);
        }
    }
}
