using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>Un paso del recorrido guiado: qué resaltar y qué explicar.</summary>
    internal sealed class PasoCoach06AV
    {
        public string Titulo;
        public string Texto;
        public string ClaveModulo;   // navega el sistema a esta pantalla antes de mostrar el paso (o null)
        // Qué se resalta:
        //   "centro"          nada (tarjeta centrada)
        //   "sidebar"         el ítem del menú de ClaveModulo
        //   "contenido"       el área de trabajo
        //   "boton:<clave>"   el botón de la pantalla actual cuyo texto es la clave i18n (si no está, el área de trabajo)
        //   "main:<id>"       un elemento de la ventana principal (FRMMain.ElementoGuia)
        public string Objetivo;

        public PasoCoach06AV(string titulo, string texto, string claveModulo, string objetivo)
        {
            Titulo = titulo;
            Texto = texto;
            ClaveModulo = claveModulo;
            Objetivo = objetivo;
        }
    }

    /// <summary>
    /// Recorrido guiado ("coach marks"): oscurece la ventana, deja iluminado el elemento
    /// real que se explica (con un borde de color) y muestra al lado una tarjeta con el
    /// título, «N de M», la explicación y los botones Saltar recorrido / Atrás / Siguiente.
    /// Va navegando el sistema pantalla por pantalla. Teclado: → o Enter avanza, ← vuelve,
    /// Esc sale. Usa el tema y el idioma activos.
    /// </summary>
    internal sealed class CoachMarks06AV
    {
        private const int Margen = 6;     // aire alrededor del elemento iluminado
        private const int Separacion = 14; // distancia entre el elemento y la tarjeta

        private readonly FRMMain _main;
        private readonly List<PasoCoach06AV> _pasos;
        private readonly Action _alCerrar;
        private int _i;

        private Velo06AV _velo;
        private Marco06AV _marco;
        private Tarjeta06AV _tarjeta;
        private Control _objetivo;
        private bool _cerrado;

        private static CoachMarks06AV _activo;

        public CoachMarks06AV(FRMMain main, List<PasoCoach06AV> pasos, Action alCerrar = null)
        {
            _main = main;
            _pasos = pasos;
            _alCerrar = alCerrar;
        }

        public void Iniciar()
        {
            if (_main == null || _pasos == null || _pasos.Count == 0) return;
            _activo?.Cerrar();
            _activo = this;

            _velo = new Velo06AV();
            _marco = new Marco06AV();
            _tarjeta = new Tarjeta06AV();
            _tarjeta.Saltar += Cerrar;
            _tarjeta.Atras += () => { if (_i > 0) { _i--; Mostrar(); } };
            _tarjeta.Siguiente += () => { if (_i >= _pasos.Count - 1) Cerrar(); else { _i++; Mostrar(); } };

            // El borde y la tarjeta son "propiedad" del velo: Windows siempre dibuja una ventana
            // propia por encima de su dueña, así el velo nunca puede tapar la tarjeta (y dejar
            // los botones sin poder tocarse) aunque la ventana principal se active o cambie de pantalla.
            _velo.Show(_main);
            _marco.Show(_velo);
            _tarjeta.Show(_velo);
            _velo.Tocado += TraerTarjeta;

            _main.Move += Seguir;
            _main.Resize += Seguir;
            _main.FormClosed += AlCerrarMain;
            Tema.TemaChanged += Mostrar;
            // Si una pantalla abre un diálogo (error, confirmación…), el recorrido se oculta
            // mientras tanto para que el diálogo quede a la vista y se pueda responder.
            Application.EnterThreadModal += AlAbrirDialogo;
            Application.LeaveThreadModal += AlCerrarDialogo;

            _i = 0;
            Mostrar();
        }

        private void AlCerrarMain(object sender, FormClosedEventArgs e) => Cerrar();

        // -- lógica del recorrido -------------------------------------------------------------

        private void Mostrar()
        {
            if (_cerrado) return;
            var p = _pasos[_i];

            // navega a la pantalla del paso (si el usuario tiene permiso)
            if (!string.IsNullOrEmpty(p.ClaveModulo) && _main.PuedeNavegarAModulo(p.ClaveModulo))
            {
                try { _main.NavegarAModulo(p.ClaveModulo); } catch { }
            }

            var t = GestorIdioma06AV.Instancia;
            _tarjeta.Cargar(p.Titulo, p.Texto, t.Obtener("tour_contador", _i + 1, _pasos.Count),
                            _i > 0, _i >= _pasos.Count - 1);

            // la pantalla recién abierta termina de acomodarse antes de buscar el elemento
            _main.BeginInvoke((Action)(() =>
            {
                if (_cerrado) return;
                _objetivo = ResolverObjetivo(p);
                Ubicar();
            }));
        }

        private Control ResolverObjetivo(PasoCoach06AV p)
        {
            try
            {
                string obj = p.Objetivo ?? "centro";
                if (obj == "sidebar" && !string.IsNullOrEmpty(p.ClaveModulo))
                {
                    var item = _main.ItemSidebarPorClave(p.ClaveModulo);
                    if (Visible(item)) return item;
                    var padre = (item as ItemSidebar06AV)?.Padre;
                    return Visible(padre) ? padre : null;
                }
                if (obj == "contenido")
                    return _main.AreaContenido();
                if (obj.StartsWith("boton:"))
                    return BuscarBoton(_main.AreaContenido(), GestorIdioma06AV.Instancia.Obtener(obj.Substring(6)))
                           ?? _main.AreaContenido();
                if (obj.StartsWith("main:"))
                {
                    var c = _main.ElementoGuia(obj.Substring(5));
                    return Visible(c) ? c : null;
                }
            }
            catch { }
            return null;
        }

        private static bool Visible(Control c) => c != null && !c.IsDisposed && c.Visible && c.Width > 2 && c.Height > 2;

        /// <summary>El control visible de la pantalla cuyo texto es el indicado (prefiere botones).</summary>
        private static Control BuscarBoton(Control raiz, string texto)
        {
            string buscado = GestorIdioma06AV.NormalizarTexto(texto);
            if (raiz == null || buscado.Length == 0) return null;
            Control otro = null;
            var pila = new Stack<Control>();
            pila.Push(raiz);
            while (pila.Count > 0)
            {
                var c = pila.Pop();
                foreach (Control h in c.Controls) pila.Push(h);
                if (c == raiz || !Visible(c)) continue;
                if (!string.Equals(GestorIdioma06AV.NormalizarTexto(c.Text), buscado, StringComparison.OrdinalIgnoreCase)) continue;
                if (c is ButtonBase) return c;
                if (otro == null && !(c is Label)) otro = c;
            }
            return otro;
        }

        private void Seguir(object sender, EventArgs e)
        {
            if (_cerrado || _enDialogo) return;
            bool minimizada = _main.WindowState == FormWindowState.Minimized;
            _velo.Visible = _tarjeta.Visible = !minimizada;
            if (minimizada) { _marco.Visible = false; return; }
            Ubicar();
        }

        private void Ubicar()
        {
            if (_cerrado || _enDialogo || _main.WindowState == FormWindowState.Minimized) return;
            Rectangle ventana = _main.RectangleToScreen(_main.ClientRectangle);
            _velo.Bounds = ventana;

            Rectangle? foco = null;
            if (Visible(_objetivo))
            {
                try
                {
                    Rectangle r = _objetivo.RectangleToScreen(_objetivo.ClientRectangle);
                    r.Inflate(Margen, Margen);
                    r.Intersect(ventana);
                    if (r.Width > 4 && r.Height > 4) foco = r;
                }
                catch { foco = null; }
            }

            _velo.Hueco(foco.HasValue ? new Rectangle(foco.Value.X - ventana.X, foco.Value.Y - ventana.Y, foco.Value.Width, foco.Value.Height) : (Rectangle?)null);

            if (foco.HasValue)
            {
                Rectangle f = foco.Value;
                var aro = Rectangle.Inflate(f, 3, 3);
                _marco.Bounds = aro;
                _marco.ActualizarMarco(3);
                if (!_marco.Visible) _marco.Show(_velo);
                _tarjeta.Location = PosicionTarjeta(f, ventana, _tarjeta.Size);
            }
            else
            {
                _marco.Visible = false;
                _tarjeta.Location = new Point(ventana.Left + (ventana.Width - _tarjeta.Width) / 2,
                                              ventana.Top + (ventana.Height - _tarjeta.Height) / 2);
            }

            _marco.BringToFront();
            _tarjeta.BringToFront();
            _tarjeta.Invalidate();
        }

        /// <summary>A la derecha, abajo, a la izquierda o arriba del elemento: el primer lugar donde entra.</summary>
        private static Point PosicionTarjeta(Rectangle f, Rectangle v, Size t)
        {
            int Ajustar(int valor, int min, int max) => Math.Max(min, Math.Min(valor, max));
            int xMin = v.Left + 8, xMax = v.Right - t.Width - 8;
            int yMin = v.Top + 8, yMax = v.Bottom - t.Height - 8;

            var candidatos = new[]
            {
                new Point(f.Right + Separacion, Ajustar(f.Top, yMin, yMax)),             // derecha
                new Point(Ajustar(f.Left, xMin, xMax), f.Bottom + Separacion),           // abajo
                new Point(f.Left - Separacion - t.Width, Ajustar(f.Top, yMin, yMax)),    // izquierda
                new Point(Ajustar(f.Left, xMin, xMax), f.Top - Separacion - t.Height),   // arriba
            };
            foreach (var p in candidatos)
                if (p.X >= xMin && p.X <= xMax && p.Y >= yMin && p.Y <= yMax) return p;

            // el elemento ocupa casi toda la ventana (el área de trabajo): la tarjeta va adentro, arriba a la derecha
            return new Point(Ajustar(f.Right - t.Width - 24, xMin, xMax), Ajustar(f.Top + 24, yMin, yMax));
        }

        private bool _enDialogo;

        private void AlAbrirDialogo(object sender, EventArgs e)
        {
            if (_cerrado) return;
            _enDialogo = true;
            foreach (Form f in new Form[] { _tarjeta, _marco, _velo })
                try { f.Visible = false; } catch { }
        }

        private void AlCerrarDialogo(object sender, EventArgs e)
        {
            if (_cerrado || !_enDialogo) return;
            _enDialogo = false;
            try
            {
                _velo.Visible = true;
                _tarjeta.Visible = true;
                Ubicar();
                TraerTarjeta();
            }
            catch { }
        }

        private void TraerTarjeta()
        {
            if (_cerrado || _tarjeta == null || _tarjeta.IsDisposed) return;
            _tarjeta.BringToFront();
            _tarjeta.Activate();
        }

        /// <summary>Cierra el recorrido en curso, si hay uno (Esc desde la ventana principal).</summary>
        public static bool CerrarActivo()
        {
            if (_activo == null) return false;
            _activo.Cerrar();
            return true;
        }

        public void Cerrar()
        {
            if (_cerrado) return;
            _cerrado = true;
            if (_activo == this) _activo = null;
            try { _main.Move -= Seguir; _main.Resize -= Seguir; _main.FormClosed -= AlCerrarMain; } catch { }
            Tema.TemaChanged -= Mostrar;
            Application.EnterThreadModal -= AlAbrirDialogo;
            Application.LeaveThreadModal -= AlCerrarDialogo;
            foreach (Form f in new Form[] { _tarjeta, _marco, _velo })
                try { if (f != null) { f.Close(); f.Dispose(); } } catch { }
            try { if (!_main.IsDisposed) _main.Activate(); } catch { }
            try { _alCerrar?.Invoke(); } catch { }
        }

        // -- velo oscuro con el hueco del elemento iluminado ----------------------------------

        private sealed class Velo06AV : Form
        {
            public Velo06AV()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                BackColor = Color.Black;
                Opacity = 0.55;
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000 | 0x80; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                    return cp;
                }
            }

            protected override bool ShowWithoutActivation => true;

            /// <summary>Clic sobre la zona oscura: no hace nada en el sistema, solo vuelve a mostrar la tarjeta.</summary>
            public event Action Tocado;

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Tocado?.Invoke();
            }

            public void Hueco(Rectangle? local)
            {
                var reg = new Region(new Rectangle(0, 0, Width, Height));
                if (local.HasValue)
                    using (GraphicsPath p = Pintura06AV.Redondeado(local.Value, 10))
                        reg.Exclude(p);
                var anterior = Region;
                Region = reg;
                anterior?.Dispose();
            }
        }

        // -- borde de color alrededor del elemento (no recibe clics) --------------------------

        private sealed class Marco06AV : Form
        {
            public Marco06AV()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
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

            protected override bool ShowWithoutActivation => true;

            public void ActualizarMarco(int grosor)
            {
                int w = Width, h = Height;
                if (w <= 2 * grosor || h <= 2 * grosor) return;
                BackColor = Tema.Acento;
                using (GraphicsPath exterior = Pintura06AV.Redondeado(new Rectangle(0, 0, w, h), 12))
                using (GraphicsPath interior = Pintura06AV.Redondeado(new Rectangle(grosor, grosor, w - 2 * grosor, h - 2 * grosor), 10))
                {
                    var reg = new Region(exterior);
                    reg.Exclude(interior);
                    var anterior = Region;
                    Region = reg;
                    anterior?.Dispose();
                }
            }
        }

        // -- tarjeta con la explicación y los botones -----------------------------------------

        private sealed class Tarjeta06AV : Form
        {
            private const int Ancho = 380;
            private const int Pad = 18;

            private readonly Label _lblTitulo = new Label { AutoSize = false, UseMnemonic = false };
            private readonly Label _lblContador = new Label { AutoSize = false };
            private readonly Label _lblTexto = new Label { AutoSize = false, UseMnemonic = false };
            private readonly Label _lnkSaltar = new Label { AutoSize = true, Cursor = Cursors.Hand };
            private readonly Button _btnAtras = new Button { Width = 92, Height = 34, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabStop = false };
            private readonly Button _btnSig = new Button { Width = 104, Height = 34, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabStop = false };
            private bool _hayAtras;

            public event Action Saltar, Atras, Siguiente;

            public Tarjeta06AV()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                KeyPreview = true;
                Width = Ancho;
                DoubleBuffered = true;
                Controls.AddRange(new Control[] { _lblTitulo, _lblContador, _lblTexto, _lnkSaltar, _btnAtras, _btnSig });
                _lnkSaltar.Click += (s, e) => Saltar?.Invoke();
                _btnAtras.Click += (s, e) => { if (_hayAtras) Atras?.Invoke(); };
                _btnSig.Click += (s, e) => Siguiente?.Invoke();
                _lnkSaltar.MouseEnter += (s, e) => _lnkSaltar.Font = new Font(Tema.FuenteRegular, FontStyle.Underline);
                _lnkSaltar.MouseLeave += (s, e) => _lnkSaltar.Font = Tema.FuenteRegular;
            }

            protected override bool ShowWithoutActivation => false;

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Escape) Saltar?.Invoke();
                else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Enter) Siguiente?.Invoke();
                else if (e.KeyCode == Keys.Left && _hayAtras) Atras?.Invoke();
                else return;
                e.Handled = true;
            }

            protected override bool ProcessDialogKey(Keys keyData)
            {
                // Enter y las flechas no deben mover el foco entre los botones
                if (keyData == Keys.Enter || keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Escape)
                {
                    OnKeyDown(new KeyEventArgs(keyData));
                    return true;
                }
                return base.ProcessDialogKey(keyData);
            }

            public void Cargar(string titulo, string texto, string contador, bool hayAtras, bool esUltimo)
            {
                var t = GestorIdioma06AV.Instancia;
                _hayAtras = hayAtras;
                BackColor = Tema.FondoPanel;
                foreach (Control c in Controls) c.BackColor = Tema.FondoPanel;

                _lblContador.Text = contador;
                _lblContador.Font = Tema.FuenteMini;
                _lblContador.ForeColor = Tema.TextoSuave;

                _lblTitulo.Text = titulo;
                _lblTitulo.Font = Tema.FuenteSubtit;
                _lblTitulo.ForeColor = Tema.TextoFuerte;

                _lblTexto.Text = texto;
                _lblTexto.Font = Tema.FuenteRegular;
                _lblTexto.ForeColor = Tema.Texto;

                _lnkSaltar.Text = t.Obtener("tour_saltar");
                _lnkSaltar.Font = Tema.FuenteRegular;
                _lnkSaltar.ForeColor = Tema.Primario;
                _lnkSaltar.Visible = !esUltimo;

                _btnAtras.Text = t.Obtener("tour_atras");
                _btnSig.Text = t.Obtener(esUltimo ? "tour_finalizar" : "tour_siguiente");
                if (hayAtras) Tema.AplicarBotonSecundario(_btnAtras); else Tema.AplicarBotonDeshabilitado(_btnAtras);
                Tema.AplicarBotonPrimario(_btnSig);

                Acomodar();
            }

            private void Acomodar()
            {
                const TextFormatFlags envolver = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
                Size contador = TextRenderer.MeasureText(_lblContador.Text, _lblContador.Font);
                _lblContador.Bounds = new Rectangle(Ancho - Pad - contador.Width, Pad + 3, contador.Width, contador.Height);

                int anchoTitulo = Ancho - 2 * Pad - contador.Width - 10;
                int altoTitulo = TextRenderer.MeasureText(_lblTitulo.Text, _lblTitulo.Font, new Size(anchoTitulo, int.MaxValue), envolver).Height;
                _lblTitulo.Bounds = new Rectangle(Pad, Pad, anchoTitulo, altoTitulo);

                int anchoTexto = Ancho - 2 * Pad;
                int altoTexto = TextRenderer.MeasureText(_lblTexto.Text, _lblTexto.Font, new Size(anchoTexto, int.MaxValue), envolver).Height;
                _lblTexto.Bounds = new Rectangle(Pad, _lblTitulo.Bottom + 8, anchoTexto, altoTexto + 2);

                int yBotones = _lblTexto.Bottom + 16;
                _btnSig.Location = new Point(Ancho - Pad - _btnSig.Width, yBotones);
                _btnAtras.Location = new Point(_btnSig.Left - 8 - _btnAtras.Width, yBotones);
                _lnkSaltar.Location = new Point(_btnAtras.Left - 16 - _lnkSaltar.PreferredWidth,
                                                yBotones + (_btnSig.Height - _lnkSaltar.PreferredHeight) / 2);
                Height = yBotones + _btnSig.Height + Pad;

                var anterior = Region;
                using (GraphicsPath p = Pintura06AV.Redondeado(new Rectangle(0, 0, Width, Height), 12))
                    Region = new Region(p);
                anterior?.Dispose();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Pintura06AV.Calidad(e.Graphics);
                Pintura06AV.Borde(e.Graphics, new Rectangle(1, 1, Width - 3, Height - 3), 11, Tema.Acento, 2f);
            }
        }

        // -- recorridos -----------------------------------------------------------------------

        // Textos en Resources\Idiomas: tour_xxx_NN_tit / tour_xxx_NN_txt.
        private static PasoCoach06AV Paso(string clave, string modulo, string objetivo)
        {
            var t = GestorIdioma06AV.Instancia;
            return new PasoCoach06AV(t.Obtener(clave + "_tit"), t.Obtener(clave + "_txt"), modulo, objetivo);
        }

        /// <summary>Recorrido de bienvenida: dónde está cada cosa en la pantalla principal.</summary>
        public static List<PasoCoach06AV> TourBienvenida() => new List<PasoCoach06AV>
        {
            Paso("tour_bv_01", "menu_inicio", "main:usuario"),
            Paso("tour_bv_02", "menu_inicio", "main:inicio"),
            Paso("tour_bv_03", "menu_inicio", "main:menu"),
            Paso("tour_bv_04", "menu_inicio", "main:contraer"),
            Paso("tour_bv_05", "menu_inicio", "contenido"),
            Paso("tour_bv_06", "menu_inicio", "main:ayuda"),
            Paso("tour_bv_07", "menu_inicio", "main:avisos"),
            Paso("tour_bv_08", "menu_inicio", "main:tema"),
            Paso("tour_bv_09", "menu_inicio", "main:idioma"),
            Paso("tour_bv_10", "menu_inicio", "main:guia")
        };

        public static List<PasoCoach06AV> TourPN1() => new List<PasoCoach06AV>
        {
            Paso("tour_pn1_01", null, "centro"),
            Paso("tour_pn1_02", "pcf_menu_clientes", "sidebar"),
            Paso("tour_pn1_03", "pcf_menu_clientes", "boton:nuevo"),
            Paso("tour_pn1_04", "pcf_menu_ventas", "sidebar"),
            Paso("tour_pn1_05", "pcf_menu_ventas", "boton:pcf_nueva_venta"),
            Paso("tour_pn1_06", "pcf_menu_ventas", "contenido"),
            Paso("tour_pn1_07", "pcf_menu_produccion", "sidebar"),
            Paso("tour_pn1_08", "pcf_menu_produccion", "boton:pcf_nueva_orden"),
            Paso("tour_pn1_09", "pcf_menu_entregas", "sidebar"),
            Paso("tour_pn1_10", "pcf_menu_entregas", "contenido"),
            Paso("tour_pn1_11", null, "centro")
        };

        public static List<PasoCoach06AV> TourPN2() => new List<PasoCoach06AV>
        {
            Paso("tour_pn2_01", null, "centro"),
            Paso("tour_pn2_02", "pcf_menu_compras", "sidebar"),
            Paso("tour_pn2_03", "pcf_menu_compras", "boton:pcf_nueva_orden"),
            Paso("tour_pn2_04", "pcf_menu_proveedores", "sidebar"),
            Paso("tour_pn2_05", "pcf_menu_proveedores", "boton:nuevo"),
            Paso("tour_pn2_06", "pcf_menu_compras", "sidebar"),
            Paso("tour_pn2_07", "pcf_menu_compras", "boton:pcf_pedir_cotizacion"),
            Paso("tour_pn2_08", "pcf_menu_compras", "boton:pcf_aprobar"),
            Paso("tour_pn2_09", "pcf_menu_compras", "boton:pcf_recibir_insumos"),
            Paso("tour_pn2_10", null, "centro")
        };

        // -- "ya lo vio": se recuerda por usuario en esta PC ----------------------------------

        private static string ArchivoVistos =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCFORGE", "recorridos_vistos.txt");

        public static bool YaVisto(string recorrido, string usuario)
        {
            if (string.IsNullOrEmpty(usuario)) return true;
            try
            {
                string linea = recorrido + "|" + usuario;
                return File.Exists(ArchivoVistos) &&
                       File.ReadAllLines(ArchivoVistos).Any(l => string.Equals(l.Trim(), linea, StringComparison.OrdinalIgnoreCase));
            }
            catch { return true; }
        }

        public static void MarcarVisto(string recorrido, string usuario)
        {
            if (string.IsNullOrEmpty(usuario) || YaVisto(recorrido, usuario)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArchivoVistos));
                File.AppendAllText(ArchivoVistos, recorrido + "|" + usuario + Environment.NewLine);
            }
            catch { }
        }
    }
}