using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>Un paso del recorrido guiado: que resaltar y que explicar.</summary>
    internal sealed class PasoCoach06AV
    {
        public string Titulo;
        public string Texto;
        public string ClaveModulo;   // navega el sistema a esta pantalla (o null)
        public string Objetivo;      // "sidebar" | "contenido" | "centro"

        public PasoCoach06AV(string titulo, string texto, string claveModulo, string objetivo)
        {
            Titulo = titulo;
            Texto = texto;
            ClaveModulo = claveModulo;
            Objetivo = objetivo;
        }
    }

    /// <summary>
    /// Sistema de "coach marks": resalta con un marco el control real (item del menu
    /// o el area de contenido) y muestra un globo con la explicacion y botones
    /// Anterior / Siguiente / Cerrar. Va navegando el sistema pantalla por pantalla.
    /// Se dibuja con el tema activo. No depende del control que lo lanza: usa dos
    /// ventanas propias (el marco, click-through, y el globo).
    /// </summary>
    internal sealed class CoachMarks06AV
    {
        private readonly FRMMain _main;
        private readonly List<PasoCoach06AV> _pasos;
        private int _i;

        private Marco06AV _marco;
        private Form _globo;
        private Label _lblTitulo, _lblTexto, _lblPaso;
        private Button _btnAtras, _btnSig, _btnCerrar;
        private Control _objetivo;
        private bool _cerrado;

        public CoachMarks06AV(FRMMain main, List<PasoCoach06AV> pasos)
        {
            _main = main;
            _pasos = pasos;
        }

        public void Iniciar()
        {
            if (_main == null || _pasos == null || _pasos.Count == 0) return;
            ConstruirVentanas();
            _i = 0;
            Mostrar();
            _globo.Show(_main);
            _main.Move += Seguir;
            _main.Resize += Seguir;
            _main.FormClosed += (s, e) => Cerrar();
        }

        // -- construccion de las ventanas -----------------------------------------------------

        private void ConstruirVentanas()
        {
            _marco = new Marco06AV();

            _globo = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                Width = 400,
                Height = 210,
                BackColor = Tema.FondoPanel
            };
            _globo.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, _globo.Width - 1, _globo.Height - 1);
                using (var p = new Pen(Tema.Primario, 2)) e.Graphics.DrawRectangle(p, r);
            };

            _lblTitulo = new Label
            {
                Dock = DockStyle.Top, Height = 38, Padding = new Padding(18, 12, 18, 2),
                Font = Tema.FuenteSubtit, ForeColor = Tema.Primario, BackColor = Tema.FondoPanel
            };
            _lblTexto = new Label
            {
                Dock = DockStyle.Fill, Padding = new Padding(18, 2, 18, 8),
                Font = Tema.FuenteRegular, ForeColor = Tema.Texto, BackColor = Tema.FondoPanel
            };

            var barra = new Panel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(14, 8, 14, 12), BackColor = Tema.FondoPanel };
            _lblPaso = new Label
            {
                Dock = DockStyle.Left, Width = 90, TextAlign = ContentAlignment.MiddleLeft,
                Font = Tema.FuenteMini, ForeColor = Tema.TextoSuave, BackColor = Tema.FondoPanel
            };
            var botones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.FondoPanel
            };
            _btnCerrar = NuevoBoton("Cerrar");
            _btnAtras = NuevoBoton("Anterior");
            _btnSig = NuevoBoton("Siguiente");
            _btnCerrar.Click += (s, e) => Cerrar();
            _btnAtras.Click += (s, e) => { if (_i > 0) { _i--; Mostrar(); } };
            _btnSig.Click += (s, e) => { if (_i >= _pasos.Count - 1) Cerrar(); else { _i++; Mostrar(); } };
            botones.Controls.Add(_btnCerrar);
            botones.Controls.Add(_btnAtras);
            botones.Controls.Add(_btnSig);
            barra.Controls.Add(botones);
            barra.Controls.Add(_lblPaso);

            _globo.Controls.Add(_lblTexto);
            _globo.Controls.Add(barra);
            _globo.Controls.Add(_lblTitulo);
        }

        private static Button NuevoBoton(string texto)
        {
            return new Button
            {
                Text = texto, AutoSize = false, Width = 104, Height = 32,
                Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
        }

        // -- logica del recorrido -------------------------------------------------------------

        private void Mostrar()
        {
            if (_cerrado) return;
            var p = _pasos[_i];

            // navega a la pantalla del paso (si el usuario tiene permiso)
            if (!string.IsNullOrEmpty(p.ClaveModulo) && _main.PuedeNavegarAModulo(p.ClaveModulo))
            {
                try { _main.NavegarAModulo(p.ClaveModulo); } catch { }
            }

            _objetivo = ResolverObjetivo(p);

            _lblTitulo.Text = p.Titulo;
            _lblTexto.Text = p.Texto;
            _lblPaso.Text = "Paso " + (_i + 1) + " de " + _pasos.Count;
            _btnAtras.Enabled = _i > 0;
            _btnSig.Text = _i >= _pasos.Count - 1 ? "Finalizar" : "Siguiente";
            Tema.AplicarBotonSecundario(_btnCerrar);
            if (_btnAtras.Enabled) Tema.AplicarBotonSecundario(_btnAtras);
            else Tema.AplicarBotonDeshabilitado(_btnAtras);
            Tema.AplicarBotonPrimario(_btnSig);

            Ubicar();
        }

        private Control ResolverObjetivo(PasoCoach06AV p)
        {
            try
            {
                if (p.Objetivo == "sidebar" && !string.IsNullOrEmpty(p.ClaveModulo))
                    return _main.ItemSidebarPorClave(p.ClaveModulo);
                if (p.Objetivo == "contenido")
                    return _main.AreaContenido();
            }
            catch { }
            return null;
        }

        private void Seguir(object sender, EventArgs e)
        {
            if (!_cerrado) Ubicar();
        }

        private void Ubicar()
        {
            Rectangle? foco = null;
            if (_objetivo != null && !_objetivo.IsDisposed && _objetivo.Visible && _objetivo.Width > 2 && _objetivo.Height > 2)
            {
                try { foco = _objetivo.RectangleToScreen(_objetivo.ClientRectangle); }
                catch { foco = null; }
            }

            Rectangle wa = Screen.FromControl(_main).WorkingArea;

            if (foco.HasValue)
            {
                Rectangle f = foco.Value;
                int pad = 8;
                _marco.Bounds = new Rectangle(f.X - pad, f.Y - pad, f.Width + 2 * pad, f.Height + 2 * pad);
                _marco.ActualizarMarco(3);
                if (!_marco.Visible) _marco.Show(_main);
                _marco.BringToFront();

                int bx = Math.Min(Math.Max(f.X, wa.Left + 8), wa.Right - _globo.Width - 8);
                int by = f.Bottom + 14;
                if (by + _globo.Height > wa.Bottom) by = f.Y - _globo.Height - 14;
                if (by < wa.Top + 8) by = wa.Top + 8;
                _globo.Location = new Point(bx, by);
            }
            else
            {
                _marco.Visible = false;
                _globo.Location = new Point(wa.Left + (wa.Width - _globo.Width) / 2,
                                            wa.Top + (wa.Height - _globo.Height) / 2);
            }

            if (_globo.Visible) _globo.BringToFront();
        }

        private void Cerrar()
        {
            if (_cerrado) return;
            _cerrado = true;
            try { _main.Move -= Seguir; _main.Resize -= Seguir; } catch { }
            try { if (_marco != null) { _marco.Close(); _marco.Dispose(); } } catch { }
            try { if (_globo != null) { _globo.Close(); _globo.Dispose(); } } catch { }
        }

        // -- ventana del marco resaltador (click-through) -------------------------------------

        private sealed class Marco06AV : Form
        {
            public Marco06AV()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                TopMost = true;
                BackColor = Tema.Acento;
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    // WS_EX_NOACTIVATE | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW
                    cp.ExStyle |= 0x08000000 | 0x20 | 0x80;
                    return cp;
                }
            }

            protected override bool ShowWithoutActivation { get { return true; } }

            public void ActualizarMarco(int th)
            {
                int w = Width, h = Height;
                if (w <= 2 * th || h <= 2 * th) return;
                BackColor = Tema.Acento;
                int rad = 10;
                using (GraphicsPath outer = Redondeado(new Rectangle(0, 0, w, h), rad))
                using (GraphicsPath inner = Redondeado(new Rectangle(th, th, w - 2 * th, h - 2 * th), Math.Max(2, rad - th)))
                {
                    var reg = new Region(outer);
                    reg.Exclude(inner);
                    Region = reg;
                }
            }

            private static GraphicsPath Redondeado(Rectangle r, int rad)
            {
                int d = Math.Max(2, rad * 2);
                if (d > r.Width) d = r.Width;
                if (d > r.Height) d = r.Height;
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                return p;
            }
        }

        // -- definicion de los recorridos -----------------------------------------------------

        // Textos en Resources\Idiomas: tour_pnN_MM_tit / tour_pnN_MM_txt (numeración G04).
        private static PasoCoach06AV Paso(string clave, string modulo, string objetivo)
        {
            var t = GestorIdioma06AV.Instancia;
            return new PasoCoach06AV(t.Obtener(clave + "_tit"), t.Obtener(clave + "_txt"), modulo, objetivo);
        }

        public static List<PasoCoach06AV> TourPN1() => new List<PasoCoach06AV>
        {
            Paso("tour_pn1_01", null, "centro"),
            Paso("tour_pn1_02", "pcf_menu_clientes", "sidebar"),
            Paso("tour_pn1_03", "pcf_menu_clientes", "contenido"),
            Paso("tour_pn1_04", "pcf_menu_ventas", "sidebar"),
            Paso("tour_pn1_05", "pcf_menu_ventas", "contenido"),
            Paso("tour_pn1_06", "pcf_menu_ventas", "contenido"),
            Paso("tour_pn1_07", "pcf_menu_produccion", "sidebar"),
            Paso("tour_pn1_08", "pcf_menu_produccion", "contenido"),
            Paso("tour_pn1_09", "pcf_menu_entregas", "sidebar"),
            Paso("tour_pn1_10", "pcf_menu_entregas", "contenido"),
            Paso("tour_pn1_11", null, "centro")
        };

        public static List<PasoCoach06AV> TourPN2() => new List<PasoCoach06AV>
        {
            Paso("tour_pn2_01", null, "centro"),
            Paso("tour_pn2_02", "pcf_menu_compras", "sidebar"),
            Paso("tour_pn2_03", "pcf_menu_compras", "contenido"),
            Paso("tour_pn2_04", "pcf_menu_proveedores", "sidebar"),
            Paso("tour_pn2_05", "pcf_menu_proveedores", "contenido"),
            Paso("tour_pn2_06", "pcf_menu_compras", "sidebar"),
            Paso("tour_pn2_07", "pcf_menu_compras", "contenido"),
            Paso("tour_pn2_08", "pcf_menu_compras", "contenido"),
            Paso("tour_pn2_09", "pcf_menu_compras", "contenido"),
            Paso("tour_pn2_10", null, "centro")
        };
    }
}
