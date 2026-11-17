using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    /// <summary>
    /// Guia de uso embebida en el sistema. Lleva al usuario paso a paso por los procesos de
    /// negocio PN1 (Venta de computadoras) y PN2 (Compra de insumos), con el detalle de cada
    /// caso de uso (quien, donde, pasos, resultado esperado y que hacer si algo falla) y un
    /// boton para abrir directamente la pantalla real de cada paso. Se dibuja por codigo y
    /// sigue el tema claro/oscuro y el idioma activo (claves guia_* de Resources\Idiomas).
    /// </summary>
    [DesignerCategory("Code")]
    public class GuiaProcesosControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly Label _lblTitulo = new Label { AutoSize = true };
        private readonly Label _lblSubtitulo = new Label { AutoSize = true };
        private readonly Button _btnPN1 = CrearToggle();
        private readonly Button _btnPN2 = CrearToggle();
        private readonly Button _btnRecorrido = CrearBoton();
        private readonly FlowLayoutPanel _lista = new FlowLayoutPanel();
        private readonly Panel _detalle = new Panel();
        private readonly FlowLayoutPanel _flujo = new FlowLayoutPanel();
        private readonly Button _btnAtras = CrearBoton();
        private readonly Button _btnSiguiente = CrearBoton();
        private readonly Button _btnAbrir = CrearBoton();
        private readonly Label _lblProgreso = new Label { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

        private readonly List<BotonPaso06AV> _botones = new List<BotonPaso06AV>();

        private bool _esPn1 = true;
        private int _indice;

        private IReadOnlyList<PasoGuia06AV> Actual => _esPn1 ? DatosGuia06AV.Pn1 : DatosGuia06AV.Pn2;

        public GuiaProcesosControl06AV()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ConstruirLayout();

            AplicarIdioma();
            AplicarTema();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) =>
            {
                GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
                Tema.TemaChanged -= AplicarTema;
            };
        }

        // -- Construccion de la interfaz ------------------------------------------------------

        private void ConstruirLayout()
        {
            var cabecera = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 16, 20, 2)
            };
            _lblSubtitulo.Margin = new Padding(2, 4, 0, 0);
            cabecera.Controls.Add(_lblTitulo);
            cabecera.Controls.Add(_lblSubtitulo);

            var procesos = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 6, 20, 10)
            };
            _btnPN1.Click += (s, e) => CambiarProceso(true);
            _btnPN2.Click += (s, e) => CambiarProceso(false);
            _btnRecorrido.Click += (s, e) => IniciarRecorrido();
            procesos.Controls.Add(_btnPN1);
            procesos.Controls.Add(_btnPN2);
            procesos.Controls.Add(_btnRecorrido);

            var barra = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(20, 12, 20, 12) };
            _btnAtras.Dock = DockStyle.Left;
            _btnAtras.Click += (s, e) => IrA(_indice - 1);
            var derecha = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            _btnAbrir.Margin = new Padding(0, 0, 10, 0);
            _btnAbrir.Click += (s, e) => AbrirPantalla();
            _btnSiguiente.Click += (s, e) => IrA(_indice + 1);
            derecha.Controls.Add(_btnAbrir);
            derecha.Controls.Add(_btnSiguiente);
            _lblProgreso.Dock = DockStyle.Fill;
            barra.Controls.Add(_lblProgreso);
            barra.Controls.Add(derecha);
            barra.Controls.Add(_btnAtras);

            _lista.Dock = DockStyle.Left;
            _lista.Width = 300;
            _lista.FlowDirection = FlowDirection.TopDown;
            _lista.WrapContents = false;
            _lista.AutoScroll = true;
            _lista.Padding = new Padding(16, 6, 10, 16);

            _flujo.FlowDirection = FlowDirection.TopDown;
            _flujo.WrapContents = false;
            _flujo.AutoSize = true;
            _flujo.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _flujo.Padding = new Padding(22, 8, 22, 20);
            _detalle.Dock = DockStyle.Fill;
            _detalle.AutoScroll = true;
            _detalle.Controls.Add(_flujo);
            _detalle.Resize += (s, e) => RefrescarAnchos();

            var centro = new Panel { Dock = DockStyle.Fill };
            centro.Controls.Add(_detalle);
            centro.Controls.Add(_lista);

            Controls.Add(centro);
            Controls.Add(barra);
            Controls.Add(procesos);
            Controls.Add(cabecera);
        }

        private void ConstruirBotonesLista()
        {
            _lista.SuspendLayout();
            foreach (var b in _botones) b.Dispose();
            _botones.Clear();
            _lista.Controls.Clear();
            var pasos = Actual;
            for (int i = 0; i < pasos.Count; i++)
            {
                var b = new BotonPaso06AV(pasos[i].Codigo, pasos[i].Titulo)
                {
                    Width = _lista.ClientSize.Width - _lista.Padding.Horizontal,
                    Indice = i
                };
                int local = i;
                b.Click += (s, e) => IrA(local);
                _botones.Add(b);
                _lista.Controls.Add(b);
            }
            _lista.ResumeLayout();
        }

        // -- Navegacion -----------------------------------------------------------------------

        private void CambiarProceso(bool pn1)
        {
            if (_esPn1 == pn1 && _botones.Count > 0) return;
            _esPn1 = pn1;
            _indice = 0;
            ConstruirBotonesLista();
            IrA(0);
            ActualizarToggles();
        }

        private void IniciarRecorrido()
        {
            var main = FindForm() as FRMMain;
            if (main == null) return;
            var tour = _esPn1 ? CoachMarks06AV.TourPN1() : CoachMarks06AV.TourPN2();
            new CoachMarks06AV(main, tour).Iniciar();
        }

        private void IrA(int indice)
        {
            var pasos = Actual;
            if (pasos.Count == 0) return;
            _indice = Math.Max(0, Math.Min(pasos.Count - 1, indice));
            for (int i = 0; i < _botones.Count; i++)
            {
                _botones[i].Hecho = i < _indice;
                _botones[i].Activo = i == _indice;
            }
            RenderizarDetalle(pasos[_indice]);
            ActualizarBarra();
            _detalle.AutoScrollPosition = new Point(0, 0);
        }

        private void AbrirPantalla()
        {
            var paso = Actual[_indice];
            if (string.IsNullOrEmpty(paso.ClaveModulo)) return;
            (FindForm() as FRMMain)?.NavegarAModulo(paso.ClaveModulo);
        }

        // -- Dibujo del detalle ---------------------------------------------------------------

        private void RenderizarDetalle(PasoGuia06AV p)
        {
            var t = GestorIdioma06AV.Instancia;
            _flujo.SuspendLayout();
            _flujo.Controls.Clear();

            _flujo.Controls.Add(Titulo(p.Codigo + "  -  " + p.Titulo));
            _flujo.Controls.Add(Encabezado(t.Obtener("guia_quien_donde")));
            _flujo.Controls.Add(Parrafo(t.Obtener("guia_rol") + ": " + p.Rol));
            _flujo.Controls.Add(Parrafo(t.Obtener("guia_donde") + ": " + p.Donde));

            _flujo.Controls.Add(Encabezado(t.Obtener("guia_pasos")));
            string[] pasos = p.Pasos;
            for (int i = 0; i < pasos.Length; i++)
                _flujo.Controls.Add(Parrafo((i + 1) + ".  " + pasos[i]));

            _flujo.Controls.Add(Encabezado(t.Obtener("guia_resultado")));
            _flujo.Controls.Add(Parrafo(p.Resultado));

            string problema = p.Problema;
            if (!string.IsNullOrEmpty(problema))
            {
                _flujo.Controls.Add(Encabezado(t.Obtener("guia_problema")));
                _flujo.Controls.Add(Parrafo(problema));
            }

            _flujo.ResumeLayout();
            RefrescarAnchos();
        }

        private Label Titulo(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = Tema.FuenteTitulo,
                ForeColor = Tema.TextoFuerte,
                Margin = new Padding(0, 4, 0, 10)
            };
        }

        private Label Encabezado(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = Tema.FuenteSubtit,
                ForeColor = Tema.Acento,
                Margin = new Padding(0, 12, 0, 4)
            };
        }

        private Label Parrafo(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = Tema.FuenteRegular,
                ForeColor = Tema.Texto,
                Margin = new Padding(0, 1, 0, 3),
                MaximumSize = new Size(AnchoParrafo(), 0)
            };
        }

        private int AnchoParrafo()
        {
            int w = _detalle.ClientSize.Width - _flujo.Padding.Horizontal - 6;
            return Math.Max(240, w);
        }

        private void RefrescarAnchos()
        {
            int ancho = AnchoParrafo();
            foreach (Control c in _flujo.Controls)
                if (c is Label l && l.MaximumSize.Width > 0)
                    l.MaximumSize = new Size(ancho, 0);
            foreach (var b in _botones)
                b.Width = _lista.ClientSize.Width - _lista.Padding.Horizontal;
        }

        private void ActualizarToggles()
        {
            PintarToggle(_btnPN1, _esPn1);
            PintarToggle(_btnPN2, !_esPn1);
        }

        private void ActualizarBarra()
        {
            var t = GestorIdioma06AV.Instancia;
            var pasos = Actual;
            _btnAtras.Visible = _indice > 0;
            _btnSiguiente.Visible = _indice < pasos.Count - 1;
            _lblProgreso.Text = t.Obtener("guia_progreso", _indice + 1, pasos.Count);

            bool puede = !string.IsNullOrEmpty(pasos[_indice].ClaveModulo)
                         && ((FindForm() as FRMMain)?.PuedeNavegarAModulo(pasos[_indice].ClaveModulo) ?? false);
            _btnAbrir.Visible = puede;

            Tema.AplicarBotonSecundario(_btnAtras);
            Tema.AplicarBotonPrimario(_btnSiguiente);
            if (puede) Tema.AplicarBotonSecundario(_btnAbrir);
        }

        // -- Idioma / Tema --------------------------------------------------------------------

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            _lblTitulo.Text = t.Obtener("guia_titulo");
            _lblSubtitulo.Text = t.Obtener("guia_subtitulo");
            _btnPN1.Text = t.Obtener("guia_pn1");
            _btnPN2.Text = t.Obtener("guia_pn2");
            _btnAtras.Text = t.Obtener("guia_anterior");
            _btnSiguiente.Text = t.Obtener("guia_siguiente");
            _btnAbrir.Text = t.Obtener("guia_abrir");
            _btnRecorrido.Text = t.Obtener("guia_recorrido");
            ConstruirBotonesLista();
            IrA(_indice);
            ActualizarToggles();
        }

        public void AplicarTema()
        {
            BackColor = Tema.FondoApp;
            foreach (Control c in Controls) PintarFondo(c, Tema.FondoApp);
            _lista.BackColor = Tema.FondoPanel;
            _detalle.BackColor = Tema.FondoApp;
            _flujo.BackColor = Tema.FondoApp;

            _lblTitulo.Font = Tema.FuenteTitulo;
            _lblTitulo.ForeColor = Tema.TextoFuerte;
            _lblSubtitulo.Font = Tema.FuenteRegular;
            _lblSubtitulo.ForeColor = Tema.TextoSuave;
            _lblProgreso.Font = Tema.FuenteBold;
            _lblProgreso.ForeColor = Tema.TextoSuave;

            Tema.AplicarBotonPrimario(_btnRecorrido);
            foreach (var b in _botones) b.Invalidate();
            ActualizarToggles();
            if (Actual.Count > 0) RenderizarDetalle(Actual[_indice]);
            ActualizarBarra();
        }

        private static void PintarFondo(Control raiz, Color color)
        {
            foreach (Control c in raiz.Controls)
            {
                if (c is Button || c is BotonPaso06AV) continue;
                c.BackColor = color;
                PintarFondo(c, color);
            }
        }

        private void PintarToggle(Button b, bool activo)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = activo ? Tema.Primario : Tema.Borde;
            b.BackColor = activo ? Tema.Primario : Tema.FondoPanel;
            b.ForeColor = activo ? Color.White : Tema.Texto;
            b.Font = activo ? Tema.FuenteBold : Tema.FuenteRegular;
        }

        private static Button CrearToggle()
        {
            return new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(220, 38),
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(14, 0, 14, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
        }

        private static Button CrearBoton()
        {
            return new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(150, 36),
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
        }
    }

    /// <summary>Item de la lista de pasos de la guia (codigo + titulo), con estado hecho/activo/pendiente.</summary>
    internal class BotonPaso06AV : Control
    {
        private bool _hot;

        public BotonPaso06AV(string codigo, string titulo)
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Codigo = codigo;
            Titulo = titulo;
            Height = 58;
            Margin = new Padding(0, 0, 0, 8);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public string Codigo { get; }
        public string Titulo { get; }
        public int Indice { get; set; }
        public bool Hecho { get; set; }
        public bool Activo { get; set; }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            g.Clear(Parent != null ? Parent.BackColor : Tema.FondoPanel);

            var caja = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fondo = Activo ? Tema.PrimarioSuave : _hot ? Tema.SidebarHover : Tema.FondoPanel;
            Pintura06AV.Rellenar(g, caja, 10, fondo);
            Pintura06AV.Borde(g, caja, 10, Activo ? Tema.Primario : Tema.Borde);

            var badge = new Rectangle(12, (Height - 30) / 2, 30, 30);
            Color colBadge = Hecho ? Tema.Exito : Activo ? Tema.Primario : Tema.Borde;
            Pintura06AV.Rellenar(g, badge, 15, colBadge);
            Color tintaBadge = (Hecho || Activo) ? Color.White : Tema.TextoSuave;
            if (Hecho)
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check, new RectangleF(badge.X + 8, badge.Y + 8, 14, 14), tintaBadge, 2f);
            else
                Pintura06AV.TextoCentrado(g, (Indice + 1).ToString(), Tema.FuenteBold, tintaBadge, badge);

            var rCod = new Rectangle(badge.Right + 10, 8, Width - badge.Right - 16, 18);
            var rTit = new Rectangle(badge.Right + 10, 28, Width - badge.Right - 16, 22);
            Pintura06AV.TextoIzquierda(g, Codigo, Tema.FuenteBold, Activo ? Tema.Primario : Tema.TextoSuave, rCod);
            Pintura06AV.TextoIzquierda(g, Titulo, Tema.FuenteRegular, Tema.Texto, rTit);
        }
    }
}
