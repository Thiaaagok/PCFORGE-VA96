using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    /// <summary>
    /// Centro de ayuda: a la izquierda el árbol de secciones y temas (con buscador) y a la
    /// derecha el tema elegido: para qué sirve, quién lo hace y dónde, el recorrido del
    /// proceso (PN1/PN2), requisitos, paso a paso, controles del sistema, resultado, qué
    /// hacer si algo falla y temas relacionados. Abajo: ir a la pantalla real, hacer el
    /// recorrido guiado sobre el sistema y moverse al tema anterior/siguiente.
    /// El contenido viene de Resources\Ayuda\manual.{idioma}.xml (ManualAyuda06AV).
    /// </summary>
    [DesignerCategory("Code")]
    public class GuiaProcesosControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly Label _lblTitulo = new Label { AutoSize = true };
        private readonly Label _lblSubtitulo = new Label { AutoSize = false };
        private readonly Button _btnBienvenida = Boton();
        private readonly Panel _cabecera = new Panel { Dock = DockStyle.Top, Height = 86, Padding = new Padding(24, 16, 24, 8) };

        private readonly Panel _izquierda = new Panel { Dock = DockStyle.Left, Width = 310, Padding = new Padding(16, 4, 10, 16) };
        private readonly Panel _cajaBuscar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(36, 11, 10, 6) };
        private readonly TextBox _txtBuscar = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
        private readonly ArbolAyuda06AV _arbol = new ArbolAyuda06AV { Dock = DockStyle.Fill };

        private readonly VistaTema06AV _vista = new VistaTema06AV { Dock = DockStyle.Fill };
        private readonly Panel _pie = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(24, 12, 24, 12) };
        private readonly Button _btnAnterior = Boton();
        private readonly Button _btnSiguiente = Boton();
        private readonly Button _btnIr = Boton();
        private readonly Button _btnRecorrido = Boton();

        private TemaManual06AV _tema;

        public GuiaProcesosControl06AV()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            ConstruirLayout();

            _arbol.TemaElegido += Mostrar;
            _vista.TemaElegido += id => SeleccionarTema(id);
            _txtBuscar.TextChanged += (s, e) => _arbol.Filtrar(_txtBuscar.Text);
            _txtBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter) { _arbol.Focus(); _arbol.ElegirPrimeroVisible(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { _txtBuscar.Clear(); e.SuppressKeyPress = true; }
            };
            _btnBienvenida.Click += (s, e) => (FindForm() as FRMMain)?.IniciarRecorrido(FRMMain.RecorridoBienvenida);
            _btnRecorrido.Click += (s, e) => { if (_tema?.Recorrido != null) (FindForm() as FRMMain)?.IniciarRecorrido(_tema.Recorrido); };
            _btnIr.Click += (s, e) => { if (_tema?.Modulo != null) (FindForm() as FRMMain)?.NavegarAModulo(_tema.Modulo); };
            _btnAnterior.Click += (s, e) => Mover(-1);
            _btnSiguiente.Click += (s, e) => Mover(1);

            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) =>
            {
                GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
                Tema.TemaChanged -= AplicarTema;
            };
        }

        private static Button Boton() => new Button { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, MinimumSize = new Size(0, 36), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Padding = new Padding(10, 0, 10, 0) };

        private void ConstruirLayout()
        {
            _lblTitulo.Location = new Point(24, 14);
            _lblSubtitulo.Location = new Point(26, 50);
            _btnBienvenida.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cabecera.Controls.Add(_lblTitulo);
            _cabecera.Controls.Add(_lblSubtitulo);
            _cabecera.Controls.Add(_btnBienvenida);
            _cabecera.Resize += (s, e) => AcomodarCabecera();

            _cajaBuscar.Controls.Add(_txtBuscar);
            _cajaBuscar.Paint += (s, e) =>
            {
                Pintura06AV.Calidad(e.Graphics);
                var r = new Rectangle(0, 0, _cajaBuscar.Width - 1, _cajaBuscar.Height - 1);
                Pintura06AV.Rellenar(e.Graphics, r, 8, Tema.FondoPanel);
                Pintura06AV.Borde(e.Graphics, r, 8, _txtBuscar.Focused ? Tema.Primario : Tema.Borde, _txtBuscar.Focused ? 1.6f : 1f);
                Iconos06AV.Dibujar(e.Graphics, IconoPcf06AV.Lupa, new RectangleF(11, 11, 18, 18), Tema.TextoSuave, 1.7f);
            };
            _txtBuscar.GotFocus += (s, e) => _cajaBuscar.Invalidate();
            _txtBuscar.LostFocus += (s, e) => _cajaBuscar.Invalidate();
            var separador = new Panel { Dock = DockStyle.Top, Height = 10 };
            _izquierda.Controls.Add(_arbol);
            _izquierda.Controls.Add(separador);
            _izquierda.Controls.Add(_cajaBuscar);
            _izquierda.Paint += (s, e) => { using (var p = new Pen(Tema.Borde)) e.Graphics.DrawLine(p, _izquierda.Width - 1, 0, _izquierda.Width - 1, _izquierda.Height); };

            var izqPie = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
            _btnAnterior.Margin = new Padding(0, 0, 8, 0);
            _btnSiguiente.Margin = new Padding(0);
            izqPie.Controls.Add(_btnAnterior);
            izqPie.Controls.Add(_btnSiguiente);
            var derPie = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
            _btnRecorrido.Margin = new Padding(0, 0, 8, 0);
            _btnIr.Margin = new Padding(0);
            derPie.Controls.Add(_btnRecorrido);
            derPie.Controls.Add(_btnIr);
            _pie.Controls.Add(derPie);
            _pie.Controls.Add(izqPie);
            _pie.Paint += (s, e) => { using (var p = new Pen(Tema.Borde)) e.Graphics.DrawLine(p, 0, 0, _pie.Width, 0); };

            var derecha = new Panel { Dock = DockStyle.Fill };
            derecha.Controls.Add(_vista);
            derecha.Controls.Add(_pie);

            Controls.Add(derecha);
            Controls.Add(_izquierda);
            Controls.Add(_cabecera);
        }

        private void AcomodarCabecera()
        {
            _btnBienvenida.Location = new Point(_cabecera.ClientSize.Width - _cabecera.Padding.Right - _btnBienvenida.Width, 22);
            int ancho = Math.Max(200, _btnBienvenida.Left - _lblSubtitulo.Left - 24);
            int altoSub = TextRenderer.MeasureText(_lblSubtitulo.Text, _lblSubtitulo.Font, new Size(ancho, int.MaxValue),
                                                   TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
            _lblSubtitulo.Size = new Size(ancho, altoSub + 2);
            int alto = Math.Max(86, _lblSubtitulo.Bottom + 12);
            if (_cabecera.Height != alto) _cabecera.Height = alto;
        }

        // -- navegación -----------------------------------------------------------------------

        /// <summary>Muestra un tema por su id (desde el panel F1 o un tema relacionado).</summary>
        public void SeleccionarTema(string id)
        {
            var t = ManualAyuda06AV.Tema(id);
            if (t == null) return;
            if (!string.IsNullOrEmpty(_txtBuscar.Text) && !ManualAyuda06AV.Buscar(_txtBuscar.Text).Contains(t))
                _txtBuscar.Clear();
            _arbol.Elegir(t);
        }

        private void Mover(int delta)
        {
            var todos = ManualAyuda06AV.Temas.ToList();
            int i = _tema == null ? -1 : todos.IndexOf(_tema);
            int j = i + delta;
            if (j >= 0 && j < todos.Count) SeleccionarTema(todos[j].Id);
        }

        private void Mostrar(TemaManual06AV tema)
        {
            _tema = tema;
            _vista.Mostrar(tema);
            ActualizarPie();
        }

        private void ActualizarPie()
        {
            var t = GestorIdioma06AV.Instancia;
            var todos = ManualAyuda06AV.Temas.ToList();
            int i = _tema == null ? -1 : todos.IndexOf(_tema);

            _btnAnterior.Text = "←  " + t.Obtener("man_anterior");
            _btnSiguiente.Text = t.Obtener("man_siguiente") + "  →";
            _btnIr.Text = t.Obtener("man_ir");
            _btnRecorrido.Text = "▶  " + t.Obtener("man_recorrido");

            var main = FindForm() as FRMMain;
            bool puedeIr = _tema?.Modulo != null && (main == null || main.PuedeNavegarAModulo(_tema.Modulo)) && _tema.Modulo != "menu_guia";
            _btnIr.Visible = puedeIr;
            _btnRecorrido.Visible = !string.IsNullOrEmpty(_tema?.Recorrido);
            EstiloBoton(_btnAnterior, i > 0, false);
            EstiloBoton(_btnSiguiente, i >= 0 && i < todos.Count - 1, false);
            EstiloBoton(_btnIr, true, true);
            EstiloBoton(_btnRecorrido, true, false);
            if (_btnRecorrido.Visible && !_btnIr.Visible) Tema.AplicarBotonPrimario(_btnRecorrido);
        }

        private static void EstiloBoton(Button b, bool habilitado, bool primario)
        {
            b.Enabled = habilitado;
            if (!habilitado) Tema.AplicarBotonDeshabilitado(b);
            else if (primario) Tema.AplicarBotonPrimario(b);
            else Tema.AplicarBotonSecundario(b);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            PonerMarcaDeAgua();
            if (_tema == null) _arbol.ElegirPrimeroVisible();
            ActualizarPie();
        }

        // -- idioma y tema --------------------------------------------------------------------

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private void PonerMarcaDeAgua()
        {
            if (!_txtBuscar.IsHandleCreated) return;
            try { SendMessage(_txtBuscar.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, GestorIdioma06AV.Instancia.Obtener("man_buscar")); }
            catch { }
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            string id = _tema?.Id;
            _lblTitulo.Text = t.Obtener("man_titulo");
            _lblSubtitulo.Text = t.Obtener("man_subtitulo");
            _btnBienvenida.Text = "▶  " + t.Obtener("man_bienvenida");
            PonerMarcaDeAgua();

            _arbol.Recargar();
            _arbol.Filtrar(_txtBuscar.Text);
            _tema = null;
            if (id != null) SeleccionarTema(id);
            if (_tema == null) _arbol.ElegirPrimeroVisible();
            AplicarTema();
        }

        public void AplicarTema()
        {
            BackColor = Tema.FondoApp;
            _cabecera.BackColor = Tema.FondoApp;
            _lblTitulo.Font = Tema.FuenteTitulo;
            _lblTitulo.ForeColor = Tema.TextoFuerte;
            _lblTitulo.BackColor = Tema.FondoApp;
            _lblSubtitulo.Font = Tema.FuenteRegular;
            _lblSubtitulo.ForeColor = Tema.TextoSuave;
            _lblSubtitulo.BackColor = Tema.FondoApp;
            Tema.AplicarBotonSecundario(_btnBienvenida);

            _izquierda.BackColor = Tema.FondoApp;
            foreach (Control c in _izquierda.Controls) c.BackColor = Tema.FondoApp;
            _cajaBuscar.BackColor = Tema.FondoApp;
            _txtBuscar.BackColor = Tema.FondoPanel;
            _txtBuscar.ForeColor = Tema.Texto;
            _txtBuscar.Font = Tema.FuenteRegular;
            _pie.BackColor = Tema.FondoApp;
            foreach (Control c in _pie.Controls) c.BackColor = Tema.FondoApp;
            _arbol.Invalidate();
            _vista.Invalidate();
            ActualizarPie();
            AcomodarCabecera();
            Invalidate(true);
        }
    }

    // ===================================================================================
    // Base: control dibujado a mano con desplazamiento vertical propio (rueda y barra fina).
    // ===================================================================================
    [DesignerCategory("Code")]
    internal abstract class DesplazableAyuda06AV : Control
    {
        private const int AnchoBarra = 5;
        protected int Desplazamiento;
        private int _altoContenido;
        private int? _arrastre;

        protected DesplazableAyuda06AV()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
        }

        protected int AltoContenido
        {
            get => _altoContenido;
            set { _altoContenido = value; Limitar(); }
        }

        private int Maximo => Math.Max(0, _altoContenido - Height);

        protected void Limitar() => Desplazamiento = Math.Max(0, Math.Min(Desplazamiento, Maximo));

        protected void IrArriba() { Desplazamiento = 0; Invalidate(); }

        protected void HacerVisible(int y, int alto)
        {
            if (y < Desplazamiento) Desplazamiento = y;
            else if (y + alto > Desplazamiento + Height) Desplazamiento = y + alto - Height;
            Limitar();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Desplazamiento -= e.Delta * 48 / 120;
            Limitar();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (!Focused && !(FindForm()?.ActiveControl is TextBox)) Focus(); }

        private Rectangle RectBarra()
        {
            if (Maximo <= 0) return Rectangle.Empty;
            int alto = Math.Max(30, Height * Height / _altoContenido);
            int y = (Height - alto) * Desplazamiento / Maximo;
            return new Rectangle(Width - AnchoBarra - 2, y, AnchoBarra, alto);
        }

        protected void DibujarBarra(Graphics g)
        {
            var r = RectBarra();
            if (r.IsEmpty) return;
            Pintura06AV.Rellenar(g, r, AnchoBarra / 2, Pintura06AV.Suave(Tema.TextoSuave, 140));
        }

        protected bool EnBarra(Point p) { var r = RectBarra(); r.Inflate(4, 0); return !r.IsEmpty && r.Contains(p); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (EnBarra(e.Location)) _arrastre = e.Y - RectBarra().Y;
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _arrastre = null; }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_arrastre == null) return;
            int recorrido = Height - RectBarra().Height;
            if (recorrido <= 0) return;
            Desplazamiento = (e.Y - _arrastre.Value) * Maximo / recorrido;
            Limitar();
            Invalidate();
        }

        protected bool Arrastrando => _arrastre != null;
    }

    // ===================================================================================
    // Árbol de secciones y temas.
    // ===================================================================================
    [DesignerCategory("Code")]
    internal sealed class ArbolAyuda06AV : DesplazableAyuda06AV
    {
        private const int AltoSeccion = 36, AltoTema = 32;

        private sealed class Fila
        {
            public SeccionManual06AV Seccion;
            public TemaManual06AV Tema;
            public int Y, Alto;
        }

        private readonly HashSet<string> _cerradas = new HashSet<string>();
        private readonly List<Fila> _filas = new List<Fila>();
        private HashSet<TemaManual06AV> _visibles;   // null = sin filtro
        private string _filtro = "";
        private TemaManual06AV _elegido;
        private Fila _sobre;

        public event Action<TemaManual06AV> TemaElegido;

        public void Recargar()
        {
            _elegido = null;
            Armar();
        }

        public void Filtrar(string texto)
        {
            _filtro = (texto ?? "").Trim();
            _visibles = _filtro.Length == 0 ? null : new HashSet<TemaManual06AV>(ManualAyuda06AV.Buscar(_filtro));
            Desplazamiento = 0;
            Armar();
        }

        public void Elegir(TemaManual06AV t)
        {
            if (t == null) return;
            _cerradas.Remove(t.Seccion.Id);
            Armar();
            _elegido = t;
            var f = _filas.FirstOrDefault(x => x.Tema == t);
            if (f != null) HacerVisible(f.Y, f.Alto + AltoTema);
            Invalidate();
            TemaElegido?.Invoke(t);
        }

        public void ElegirPrimeroVisible()
        {
            var f = _filas.FirstOrDefault(x => x.Tema != null);
            if (f != null) Elegir(f.Tema);
        }

        private void Armar()
        {
            _filas.Clear();
            int y = 0;
            foreach (var s in ManualAyuda06AV.Secciones)
            {
                var temas = s.Temas.Where(t => _visibles == null || _visibles.Contains(t)).ToList();
                if (temas.Count == 0) continue;
                _filas.Add(new Fila { Seccion = s, Y = y, Alto = AltoSeccion });
                y += AltoSeccion;
                bool abierta = _visibles != null || !_cerradas.Contains(s.Id);
                if (!abierta) continue;
                foreach (var t in temas)
                {
                    _filas.Add(new Fila { Seccion = s, Tema = t, Y = y, Alto = AltoTema });
                    y += AltoTema;
                }
                y += 6;
            }
            AltoContenido = y + 8;
            Invalidate();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Limitar(); }

        private Fila FilaEn(Point p) => _filas.FirstOrDefault(f => p.Y + Desplazamiento >= f.Y && p.Y + Desplazamiento < f.Y + f.Alto);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Arrastrando) return;
            var f = EnBarra(e.Location) ? null : FilaEn(e.Location);
            Cursor = f != null ? Cursors.Hand : Cursors.Default;
            if (f != _sobre) { _sobre = f; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _sobre = null; Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || EnBarra(e.Location)) return;
            var f = FilaEn(e.Location);
            if (f == null) return;
            if (f.Tema != null) { Elegir(f.Tema); return; }
            if (_visibles != null) return;
            if (!_cerradas.Remove(f.Seccion.Id)) _cerradas.Add(f.Seccion.Id);
            Armar();
        }

        protected override bool IsInputKey(Keys keyData) => keyData == Keys.Up || keyData == Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) return;
            var temas = _filas.Where(f => f.Tema != null).Select(f => f.Tema).ToList();
            int i = temas.IndexOf(_elegido) + (e.KeyCode == Keys.Down ? 1 : -1);
            if (i >= 0 && i < temas.Count) Elegir(temas[i]);
            e.Handled = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Pintura06AV.Calidad(g);
            g.Clear(Tema.FondoApp);
            var t = GestorIdioma06AV.Instancia;

            if (_filas.Count == 0)
            {
                string msg = ManualAyuda06AV.Secciones.Count == 0 ? t.Obtener("man_sin_manual") : t.Obtener("man_sin_resultados", _filtro);
                TextRenderer.DrawText(g, msg, Tema.FuenteRegular, new Rectangle(8, 12, Width - 16, 80), Tema.TextoSuave,
                    TextFormatFlags.WordBreak | TextFormatFlags.Left);
                return;
            }

            int ancho = Width - 12;
            foreach (var f in _filas)
            {
                int y = f.Y - Desplazamiento;
                if (y + f.Alto < 0 || y > Height) continue;
                var fila = new Rectangle(0, y, ancho, f.Alto);

                if (f.Tema == null)
                {
                    bool abierta = _visibles != null || !_cerradas.Contains(f.Seccion.Id);
                    if (f == _sobre) Pintura06AV.Rellenar(g, Rectangle.Inflate(fila, 0, -3), 8, Tema.Seleccion);
                    TextRenderer.DrawText(g, abierta ? "▾" : "▸", Tema.FuenteSubtit, new Rectangle(4, y, 18, f.Alto), Tema.TextoSuave,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    string cuenta = f.Seccion.Temas.Count(tm => _visibles == null || _visibles.Contains(tm)).ToString();
                    var rTxt = new Rectangle(26, y, ancho - 26 - 34, f.Alto);
                    TextRenderer.DrawText(g, f.Seccion.Titulo, Tema.FuenteBold, rTxt, Tema.TextoFuerte,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                    var rCuenta = new Rectangle(ancho - 30, y + (f.Alto - 18) / 2, 26, 18);
                    Pintura06AV.Rellenar(g, rCuenta, 9, Tema.Borde);
                    TextRenderer.DrawText(g, cuenta, Tema.FuenteMini, rCuenta, Tema.TextoSuave,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    continue;
                }

                bool elegido = f.Tema == _elegido;
                var fondo = new Rectangle(14, y + 2, ancho - 14, f.Alto - 4);
                if (elegido)
                {
                    Pintura06AV.Rellenar(g, fondo, 7, Tema.PrimarioSuave);
                    Pintura06AV.Rellenar(g, new Rectangle(fondo.X, fondo.Y + 5, 3, fondo.Height - 10), 1, Tema.Primario);
                }
                else if (f == _sobre) Pintura06AV.Rellenar(g, fondo, 7, Tema.Seleccion);
                else
                    using (var p = new Pen(Tema.Borde)) g.DrawLine(p, 21, y, 21, y + f.Alto);

                int x = 30;
                if (!string.IsNullOrEmpty(f.Tema.Codigo))
                {
                    int w = TextRenderer.MeasureText(f.Tema.Codigo, Tema.FuenteMini).Width + 8;
                    var chip = new Rectangle(x, y + (f.Alto - 18) / 2, w, 18);
                    Pintura06AV.Rellenar(g, chip, 5, elegido ? Tema.Primario : Tema.Borde);
                    TextRenderer.DrawText(g, f.Tema.Codigo, Tema.FuenteMini, chip, elegido ? Tema.TextoInvertido : Tema.Texto,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    x += w + 8;
                }
                TextRenderer.DrawText(g, f.Tema.Titulo, elegido ? Tema.FuenteBold : Tema.FuenteRegular,
                    new Rectangle(x, y, ancho - x - 4, f.Alto), elegido ? Tema.Primario : Tema.Texto,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
            DibujarBarra(g);
        }
    }

    // ===================================================================================
    // Vista del tema elegido: tarjetas dibujadas a mano.
    // ===================================================================================
    [DesignerCategory("Code")]
    internal sealed class VistaTema06AV : DesplazableAyuda06AV
    {
        private const int Pad = 28, AnchoMax = 860, PadTarjeta = 16;
        private const TextFormatFlags Envolver = TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;

        // íconos de Segoe MDL2 Assets (la misma fuente que usa el menú lateral)
        private static readonly Font FuenteIcono = new Font("Segoe MDL2 Assets", 11f);
        private const string IcoPersona = "\uE77B", IcoPantalla = "\uE7F4", IcoEscudo = "\uEA18", IcoCheck = "\uE73E",
                             IcoAlerta = "\uE7BA", IcoEnlace = "\uE71B", IcoLista = "\uE8FD", IcoFlujo = "\uE72A", IcoPrevio = "\uE73A";

        private TemaManual06AV _tema;
        private readonly List<KeyValuePair<Rectangle, string>> _enlaces = new List<KeyValuePair<Rectangle, string>>();
        private string _enlaceSobre;
        private Graphics _g;
        private bool _pintar;

        public event Action<string> TemaElegido;

        public void Mostrar(TemaManual06AV tema)
        {
            _tema = tema;
            Medir();
            IrArriba();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Medir(); }

        private void Medir()
        {
            using (var g = CreateGraphics())
            {
                _g = g;
                _pintar = false;
                AltoContenido = Dibujar();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Tema.FondoApp);
            Pintura06AV.Calidad(e.Graphics);
            _g = e.Graphics;
            _pintar = true;
            _g.TranslateTransform(0, -Desplazamiento);
            _enlaces.Clear();
            int alto = Dibujar();
            _g.ResetTransform();
            if (alto != AltoContenido) AltoContenido = alto;
            DibujarBarra(e.Graphics);
        }

        // -- primitivas: miden siempre, dibujan solo si _pintar --------------------------------

        private int AltoTexto(string s, Font f, int ancho) =>
            string.IsNullOrEmpty(s) ? 0 : TextRenderer.MeasureText(_g, s, f, new Size(ancho, int.MaxValue), Envolver).Height;

        private int Texto(string s, Font f, Color c, int x, int y, int ancho)
        {
            int h = AltoTexto(s, f, ancho);
            if (_pintar && h > 0) TextRenderer.DrawText(_g, s, f, new Rectangle(x, y, ancho, h), c, Envolver);
            return h;
        }

        private void Caja(Rectangle r, Color fondo, Color borde, Color? barra = null)
        {
            if (!_pintar) return;
            Pintura06AV.Rellenar(_g, r, 10, fondo);
            Pintura06AV.Borde(_g, r, 10, borde);
            if (barra.HasValue) Pintura06AV.Rellenar(_g, new Rectangle(r.X, r.Y + 8, 4, r.Height - 16), 2, barra.Value);
        }

        private int Chip(string s, Font f, Color fondo, Color texto, int x, int y, int alto = 22)
        {
            int w = TextRenderer.MeasureText(_g, s, f).Width + 14;
            if (_pintar)
            {
                var r = new Rectangle(x, y, w, alto);
                Pintura06AV.Rellenar(_g, r, alto / 2, fondo);
                TextRenderer.DrawText(_g, s, f, r, texto, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            }
            return w;
        }

        private int Encabezado(string icono, string s, Color color, int x, int y, int ancho)
        {
            int h = Math.Max(AltoTexto(s, Tema.FuenteSubtit, ancho - 26), 22);
            if (_pintar)
            {
                TextRenderer.DrawText(_g, icono, FuenteIcono, new Rectangle(x, y, 24, h), color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                TextRenderer.DrawText(_g, s, Tema.FuenteSubtit, new Rectangle(x + 26, y, ancho - 26, h), Tema.TextoFuerte, Envolver | TextFormatFlags.VerticalCenter);
            }
            return h;
        }

        // -- documento -------------------------------------------------------------------------

        private int Dibujar()
        {
            var t = GestorIdioma06AV.Instancia;
            if (_tema == null)
            {
                Texto(t.Obtener("man_vacio"), Tema.FuenteRegular, Tema.TextoSuave, Pad, Pad, Math.Max(100, Width - 2 * Pad));
                return Height;
            }

            int ancho = Math.Max(260, Math.Min(AnchoMax, Width - 2 * Pad - 10));
            int x = Pad, y = 22;

            // migas de pan
            y += Texto(_tema.Seccion.Titulo.ToUpperInvariant() + "  ›  " + _tema.Titulo, Tema.FuenteMini, Tema.TextoSuave, x, y, ancho) + 6;

            // título con el código del caso de uso
            int xt = x;
            if (!string.IsNullOrEmpty(_tema.Codigo))
                xt += Chip(_tema.Codigo, Tema.FuenteBold, Tema.Primario, Tema.TextoInvertido, x, y + 4, 26) + 10;
            y += Texto(_tema.Titulo, Tema.FuenteTitulo, Tema.TextoFuerte, xt, y, ancho - (xt - x)) + 8;

            // para qué sirve
            using (var fResumen = new Font(Tema.FuenteRegular.FontFamily, 10.5f))
                y += Texto(_tema.Resumen, fResumen, Tema.Texto, x, y, ancho) + 16;

            // quién y dónde
            if (!string.IsNullOrEmpty(_tema.Rol) || !string.IsNullOrEmpty(_tema.Pantalla))
                y = TarjetaQuienDonde(x, y, ancho) + 16;

            // recorrido del proceso (PN1/PN2)
            if (_tema.Seccion.Flujo)
                y = Flujo(x, y, ancho) + 18;

            if (_tema.Antes.Count > 0)
                y = Lista(t.Obtener("man_antes"), IcoPrevio, Tema.Primario, _tema.Antes, Tema.FondoPanel, Tema.Borde, null, x, y, ancho) + 16;

            if (_tema.Pasos.Count > 0)
                y = Pasos(x, y, ancho) + 16;

            if (_tema.Validaciones.Count > 0)
                y = Lista(t.Obtener("man_valida"), IcoEscudo, Tema.Acento, _tema.Validaciones, Tema.AcentoSuave, Tema.Borde, Tema.Acento, x, y, ancho) + 16;

            if (!string.IsNullOrEmpty(_tema.Resultado))
                y = Lista(t.Obtener("man_resultado"), IcoCheck, Tema.Exito, new List<string> { _tema.Resultado }, Tema.ExitoSuave, Tema.Borde, Tema.Exito, x, y, ancho, sinVinietas: true) + 16;

            if (_tema.Problemas.Count > 0)
                y = Problemas(x, y, ancho) + 16;

            if (_tema.Relacionados.Count > 0)
                y = Relacionados(x, y, ancho) + 16;

            return y + 12;
        }

        private int TarjetaQuienDonde(int x, int y, int ancho)
        {
            var t = GestorIdioma06AV.Instancia;
            int mitad = (ancho - 2 * PadTarjeta - 20) / 2;
            int hRol = AltoTexto(_tema.Rol, Tema.FuenteBold, mitad - 34);
            int hPan = AltoTexto(_tema.Pantalla, Tema.FuenteBold, mitad - 34);
            int alto = PadTarjeta * 2 + 18 + Math.Max(hRol, hPan);
            Caja(new Rectangle(x, y, ancho, alto), Tema.FondoPanel, Tema.Borde);

            int cy = y + PadTarjeta;
            Columna(IcoPersona, t.Obtener("man_quien"), _tema.Rol, x + PadTarjeta, cy, mitad);
            if (_pintar)
                using (var p = new Pen(Tema.Borde)) _g.DrawLine(p, x + PadTarjeta + mitad + 10, y + 12, x + PadTarjeta + mitad + 10, y + alto - 12);
            Columna(IcoPantalla, t.Obtener("man_donde"), _tema.Pantalla, x + PadTarjeta + mitad + 20, cy, mitad);
            return y + alto;
        }

        private void Columna(string icono, string etiqueta, string valor, int x, int y, int ancho)
        {
            if (string.IsNullOrEmpty(valor)) return;
            if (_pintar)
            {
                var circulo = new Rectangle(x, y + 2, 26, 26);
                Pintura06AV.Rellenar(_g, circulo, 13, Tema.PrimarioSuave);
                TextRenderer.DrawText(_g, icono, FuenteIcono, circulo, Tema.Primario, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
            Texto(etiqueta.ToUpperInvariant(), Tema.FuenteMini, Tema.TextoSuave, x + 34, y, ancho - 34);
            Texto(valor, Tema.FuenteBold, Tema.TextoFuerte, x + 34, y + 18, ancho - 34);
        }

        private int Flujo(int x, int y, int ancho)
        {
            var t = GestorIdioma06AV.Instancia;
            var casos = _tema.Seccion.Temas.Where(c => !string.IsNullOrEmpty(c.Codigo)).ToList();
            if (casos.Count == 0) return y;
            y += Encabezado(IcoFlujo, t.Obtener("man_flujo"), Tema.Primario, x, y, ancho) + 8;

            int cx = x, alto = 30, fila = y;
            for (int i = 0; i < casos.Count; i++)
            {
                var c = casos[i];
                bool actual = c == _tema;
                string s = c.Codigo + "  " + c.Titulo;
                int w = TextRenderer.MeasureText(_g, s, Tema.FuenteRegular).Width + 22;
                if (cx + w > x + ancho && cx > x) { cx = x; fila += alto + 8; }
                var r = new Rectangle(cx, fila, w, alto);
                if (_pintar)
                {
                    bool sobre = _enlaceSobre == c.Id;
                    Pintura06AV.Rellenar(_g, r, alto / 2, actual ? Tema.Primario : (sobre ? Tema.Seleccion : Tema.FondoPanel));
                    if (!actual) Pintura06AV.Borde(_g, r, alto / 2, Tema.Borde);
                    TextRenderer.DrawText(_g, s, actual ? Tema.FuenteBold : Tema.FuenteRegular, r,
                        actual ? Tema.TextoInvertido : Tema.Texto, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                    if (!actual) _enlaces.Add(new KeyValuePair<Rectangle, string>(r, c.Id));
                }
                cx += w;
                if (i < casos.Count - 1)
                {
                    var sig = casos[i + 1];
                    int wSig = TextRenderer.MeasureText(_g, sig.Codigo + "  " + sig.Titulo, Tema.FuenteRegular).Width + 22;
                    if (cx + 22 + wSig > x + ancho) { cx = x + ancho + 1; continue; }   // la próxima va abajo: sin flecha colgando
                    if (_pintar)
                        TextRenderer.DrawText(_g, "→", Tema.FuenteBold, new Rectangle(cx, fila, 22, alto), Tema.TextoSuave,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    cx += 22;
                }
            }
            return fila + alto;
        }

        private int Lista(string titulo, string icono, Color color, List<string> items, Color fondo, Color borde, Color? barra,
                          int x, int y, int ancho, bool sinVinietas = false)
        {
            int interior = ancho - 2 * PadTarjeta - 6;
            int hTit = Math.Max(AltoTexto(titulo, Tema.FuenteSubtit, interior - 26), 22);
            int alto = PadTarjeta + hTit + 8;
            foreach (var s in items) alto += AltoTexto(s, Tema.FuenteRegular, interior - (sinVinietas ? 0 : 18)) + 6;
            alto += PadTarjeta - 6;

            Caja(new Rectangle(x, y, ancho, alto), fondo, borde, barra);
            int ix = x + PadTarjeta + 6, iy = y + PadTarjeta;
            iy += Encabezado(icono, titulo, color, ix, iy, interior) + 8;
            foreach (var s in items)
            {
                if (!sinVinietas && _pintar)
                    Pintura06AV.Rellenar(_g, new Rectangle(ix + 3, iy + 7, 6, 6), 3, color);
                iy += Texto(s, Tema.FuenteRegular, Tema.Texto, ix + (sinVinietas ? 0 : 18), iy, interior - (sinVinietas ? 0 : 18)) + 6;
            }
            return y + alto;
        }

        private int Pasos(int x, int y, int ancho)
        {
            var t = GestorIdioma06AV.Instancia;
            int interior = ancho - 2 * PadTarjeta - 48;
            int hTit = Math.Max(AltoTexto(t.Obtener("man_pasos"), Tema.FuenteSubtit, ancho), 22);

            var alturas = _tema.Pasos.Select(p =>
                Math.Max(30, AltoTexto(p.Titulo, Tema.FuenteBold, interior) + 2 + AltoTexto(p.Detalle, Tema.FuenteRegular, interior))).ToList();
            int alto = PadTarjeta + hTit + 12 + alturas.Sum() + 14 * (alturas.Count - 1) + PadTarjeta;
            Caja(new Rectangle(x, y, ancho, alto), Tema.FondoPanel, Tema.Borde);

            int ix = x + PadTarjeta, iy = y + PadTarjeta;
            iy += Encabezado(IcoLista, t.Obtener("man_pasos"), Tema.Primario, ix, iy, ancho - 2 * PadTarjeta) + 12;
            for (int i = 0; i < _tema.Pasos.Count; i++)
            {
                var p = _tema.Pasos[i];
                if (_pintar)
                {
                    if (i < _tema.Pasos.Count - 1)
                        using (var pen = new Pen(Tema.Borde, 2)) _g.DrawLine(pen, ix + 14, iy + 30, ix + 14, iy + alturas[i] + 14);
                    var circulo = new Rectangle(ix, iy, 28, 28);
                    Pintura06AV.Rellenar(_g, circulo, 14, Tema.Primario);
                    TextRenderer.DrawText(_g, (i + 1).ToString(), Tema.FuenteBold, circulo, Tema.TextoInvertido,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                int ty = iy + 4;
                ty += Texto(p.Titulo, Tema.FuenteBold, Tema.TextoFuerte, ix + 44, ty, interior) + 2;
                Texto(p.Detalle, Tema.FuenteRegular, Tema.Texto, ix + 44, ty, interior);
                iy += alturas[i] + 14;
            }
            return y + alto;
        }

        private int Problemas(int x, int y, int ancho)
        {
            var t = GestorIdioma06AV.Instancia;
            int interior = ancho - 2 * PadTarjeta - 6;
            int hTit = Math.Max(AltoTexto(t.Obtener("man_problemas"), Tema.FuenteSubtit, interior), 22);
            int alto = PadTarjeta + hTit + 8;
            foreach (var p in _tema.Problemas)
                alto += AltoTexto(p.Sintoma, Tema.FuenteBold, interior) + 2 + AltoTexto(p.Solucion, Tema.FuenteRegular, interior - 18) + 10;
            alto += PadTarjeta - 10;

            Caja(new Rectangle(x, y, ancho, alto), Tema.PeligroSuave, Tema.Borde, Tema.Peligro);
            int ix = x + PadTarjeta + 6, iy = y + PadTarjeta;
            iy += Encabezado(IcoAlerta, t.Obtener("man_problemas"), Tema.Peligro, ix, iy, interior) + 8;
            foreach (var p in _tema.Problemas)
            {
                iy += Texto(p.Sintoma, Tema.FuenteBold, Tema.TextoFuerte, ix, iy, interior) + 2;
                if (_pintar) TextRenderer.DrawText(_g, "↳", Tema.FuenteRegular, new Point(ix, iy), Tema.Peligro);
                iy += Texto(p.Solucion, Tema.FuenteRegular, Tema.Texto, ix + 18, iy, interior - 18) + 10;
            }
            return y + alto;
        }

        private int Relacionados(int x, int y, int ancho)
        {
            var t = GestorIdioma06AV.Instancia;
            y += Encabezado(IcoEnlace, t.Obtener("man_relacionados"), Tema.Primario, x, y, ancho) + 8;
            int cx = x, alto = 30;
            foreach (var id in _tema.Relacionados)
            {
                var rel = ManualAyuda06AV.Tema(id);
                if (rel == null) continue;
                string s = rel.TituloCompleto + "  ›";
                int w = TextRenderer.MeasureText(_g, s, Tema.FuenteRegular).Width + 22;
                if (cx + w > x + ancho && cx > x) { cx = x; y += alto + 8; }
                var r = new Rectangle(cx, y, w, alto);
                if (_pintar)
                {
                    bool sobre = _enlaceSobre == rel.Id;
                    Pintura06AV.Rellenar(_g, r, 8, sobre ? Tema.PrimarioSuave : Tema.FondoPanel);
                    Pintura06AV.Borde(_g, r, 8, sobre ? Tema.Primario : Tema.Borde);
                    TextRenderer.DrawText(_g, s, Tema.FuenteRegular, r, Tema.Primario,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                    _enlaces.Add(new KeyValuePair<Rectangle, string>(r, rel.Id));
                }
                cx += w + 8;
            }
            return y + alto;
        }

        // -- enlaces ---------------------------------------------------------------------------

        private string EnlaceEn(Point p)
        {
            var q = new Point(p.X, p.Y + Desplazamiento);
            foreach (var e in _enlaces) if (e.Key.Contains(q)) return e.Value;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Arrastrando) return;
            string id = EnBarra(e.Location) ? null : EnlaceEn(e.Location);
            Cursor = id != null ? Cursors.Hand : Cursors.Default;
            if (id != _enlaceSobre) { _enlaceSobre = id; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (_enlaceSobre != null) { _enlaceSobre = null; Invalidate(); } }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            string id = EnlaceEn(e.Location);
            if (id != null) TemaElegido?.Invoke(id);
        }
    }
}
