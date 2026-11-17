using SER;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    // Panel lateral "Ayuda de esta pantalla": para qué sirve, los casos de uso que se
    // trabajan ahí (pasos de la guía) y las opciones de trabajo visibles con su explicación.
    [DesignerCategory("Code")]
    internal sealed class PanelAyuda06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly Label _lblTitulo = new Label { AutoSize = false, Height = 26, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Button _btnCerrar = new Button { Text = "✕", Width = 32, Height = 30, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabStop = false };
        private readonly Panel _cabecera = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(14, 8, 8, 8) };
        private readonly FlowLayoutPanel _cuerpo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
            Padding = new Padding(14, 6, 10, 12)
        };
        private readonly FlowLayoutPanel _pie = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            Padding = new Padding(12, 10, 8, 8)
        };
        private readonly Button _btnModo = new Button { Height = 32, AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
        private readonly Button _btnGuia = new Button { Height = 32, AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private string _clave;
        private Control _pantalla;
        private bool _modoActivo;

        public event Action CerrarSolicitado;
        public event Action ModoMouseSolicitado;
        public event Action GuiaSolicitada;

        public PanelAyuda06AV()
        {
            Width = 380;
            Dock = DockStyle.Right;
            _cabecera.Controls.Add(_lblTitulo);
            _cabecera.Controls.Add(_btnCerrar);
            _pie.Controls.Add(_btnModo);
            _pie.Controls.Add(_btnGuia);
            Controls.Add(_cuerpo);
            Controls.Add(_pie);
            Controls.Add(_cabecera);
            _btnCerrar.FlatAppearance.BorderSize = 0;
            _btnCerrar.Click += (s, e) => CerrarSolicitado?.Invoke();
            _btnModo.Click += (s, e) => ModoMouseSolicitado?.Invoke();
            _btnGuia.Click += (s, e) => GuiaSolicitada?.Invoke();
            _cuerpo.Resize += (s, e) => AjustarAnchos();
            Paint += (s, e) => { using (var p = new Pen(Tema.Borde)) e.Graphics.DrawLine(p, 0, 0, 0, Height); };

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) =>
            {
                GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
                Tema.TemaChanged -= AplicarTema;
            };
            AplicarTema();
        }

        public void Mostrar(string claveModulo, Control pantalla, bool modoMouseActivo)
        {
            _clave = claveModulo;
            _pantalla = pantalla;
            _modoActivo = modoMouseActivo;
            Renderizar();
        }

        public void ModoMouse(bool activo)
        {
            _modoActivo = activo;
            ActualizarBotones();
        }

        private void Renderizar()
        {
            var t = GestorIdioma06AV.Instancia;
            string pantalla = string.IsNullOrEmpty(_clave) ? "" : t.Obtener(_clave);
            _lblTitulo.Text = t.Obtener("ayuda_panel_titulo", pantalla);

            _cuerpo.SuspendLayout();
            foreach (Control c in _cuerpo.Controls.Cast<Control>().ToList()) c.Dispose();
            _cuerpo.Controls.Clear();

            _cuerpo.Controls.Add(Parrafo(AyudaContextual06AV.DescripcionPantalla(_clave), Tema.Texto, Tema.FuenteRegular, 2));

            var casos = AyudaContextual06AV.CasosDeUso(_clave).ToList();
            foreach (PasoGuia06AV cu in casos)
            {
                _cuerpo.Controls.Add(Parrafo(cu.Codigo + " · " + cu.Titulo, Tema.Primario, Tema.FuenteSubtit, 12));
                _cuerpo.Controls.Add(Parrafo(t.Obtener("guia_rol") + ": " + cu.Rol, Tema.TextoSuave, Tema.FuenteMini, 0));
                string[] pasos = cu.Pasos;
                for (int i = 0; i < pasos.Length; i++)
                    _cuerpo.Controls.Add(Parrafo((i + 1) + ".  " + pasos[i], Tema.Texto, Tema.FuenteRegular, 3));
                _cuerpo.Controls.Add(Parrafo("✔  " + cu.Resultado, Tema.Exito, Tema.FuenteRegular, 4));
            }

            string[] propios = AyudaContextual06AV.PasosPantalla(_clave);
            if (propios.Length > 0)
            {
                _cuerpo.Controls.Add(Parrafo(t.Obtener("ayuda_panel_como"), Tema.TextoFuerte, Tema.FuenteSubtit, 12));
                for (int i = 0; i < propios.Length; i++)
                    _cuerpo.Controls.Add(Parrafo((i + 1) + ".  " + propios[i], Tema.Texto, Tema.FuenteRegular, 3));
            }

            var opciones = AyudaContextual06AV.OpcionesPantalla(_pantalla);
            _cuerpo.Controls.Add(Parrafo(t.Obtener("ayuda_panel_opciones"), Tema.TextoFuerte, Tema.FuenteSubtit, 14));
            if (opciones.Count == 0)
                _cuerpo.Controls.Add(Parrafo(t.Obtener("ayuda_panel_sin_opciones"), Tema.TextoSuave, Tema.FuenteRegular, 2));
            foreach (TemaAyuda06AV o in opciones)
            {
                _cuerpo.Controls.Add(Parrafo(o.Titulo, Tema.TextoFuerte, Tema.FuenteBold, 6));
                _cuerpo.Controls.Add(Parrafo(o.Texto, Tema.Texto, Tema.FuenteRegular, 0));
            }
            _cuerpo.Controls.Add(Parrafo(t.Obtener("ayuda_panel_atajos"), Tema.TextoSuave, Tema.FuenteMini, 16));

            _cuerpo.ResumeLayout();
            AjustarAnchos();
            _cuerpo.AutoScrollPosition = new Point(0, 0);
            ActualizarBotones();
        }

        private Label Parrafo(string texto, Color color, Font fuente, int arriba) => new Label
        {
            Text = texto ?? "",
            AutoSize = true,
            ForeColor = color,
            Font = fuente,
            BackColor = Tema.FondoPanel,
            Margin = new Padding(0, arriba, 0, 2)
        };

        private void AjustarAnchos()
        {
            int ancho = _cuerpo.ClientSize.Width - _cuerpo.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            foreach (Control c in _cuerpo.Controls)
                c.MaximumSize = new Size(Math.Max(120, ancho), 0);
        }

        private void ActualizarBotones()
        {
            var t = GestorIdioma06AV.Instancia;
            _btnModo.Text = t.Obtener(_modoActivo ? "ayuda_panel_modo_on" : "ayuda_panel_modo");
            _btnGuia.Text = t.Obtener("ayuda_panel_guia");
            if (_modoActivo) Tema.AplicarBotonAcento(_btnModo); else Tema.AplicarBotonPrimario(_btnModo);
            Tema.AplicarBotonSecundario(_btnGuia);
        }

        public void AplicarIdioma()
        {
            if (_clave != null) Renderizar(); else ActualizarBotones();
        }

        public void AplicarTema()
        {
            BackColor = Tema.FondoPanel;
            foreach (Control c in new Control[] { _cabecera, _cuerpo, _pie }) c.BackColor = Tema.FondoPanel;
            _lblTitulo.BackColor = Tema.FondoPanel;
            _lblTitulo.ForeColor = Tema.TextoFuerte;
            _lblTitulo.Font = Tema.FuenteSubtit;
            _btnCerrar.BackColor = Tema.FondoPanel;
            _btnCerrar.ForeColor = Tema.TextoSuave;
            _btnCerrar.FlatAppearance.MouseOverBackColor = Tema.PeligroSuave;
            if (_clave != null) Renderizar(); else ActualizarBotones();
            Invalidate();
        }
    }
}
