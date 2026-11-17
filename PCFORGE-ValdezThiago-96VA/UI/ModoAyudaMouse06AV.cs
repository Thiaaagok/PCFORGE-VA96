using SER;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>
    /// Modo "¿Qué es esto?": mientras está activo, el cursor pasa a ser el de ayuda y al
    /// posicionar el mouse sobre cualquier botón, campo o grilla de la pantalla se muestra
    /// su explicación. Los clics dentro de la pantalla no ejecutan la acción: fijan la ayuda.
    /// Se sale con Esc, con el botón "?" o haciendo clic fuera del área de trabajo.
    /// </summary>
    internal sealed class ModoAyudaMouse06AV : IMessageFilter, IDisposable
    {
        private const int WM_KEYDOWN = 0x100, WM_SETCURSOR = 0x20, WM_MOUSEMOVE = 0x200,
                          WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202, WM_LBUTTONDBLCLK = 0x203,
                          WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205;

        private readonly Func<Control> _area;
        private readonly GloboAyuda06AV _globo = new GloboAyuda06AV();
        private readonly Timer _espera = new Timer { Interval = 350 };
        private Control _pendiente;
        private Control _mostrado;

        public bool Activo { get; private set; }
        public event Action Cambio;

        public ModoAyudaMouse06AV(Func<Control> area)
        {
            _area = area;
            _espera.Tick += (s, e) => { _espera.Stop(); MostrarDe(_pendiente, false); };
        }

        public void Alternar() { if (Activo) Desactivar(); else Activar(); }

        public void Activar()
        {
            if (Activo) return;
            Activo = true;
            Application.AddMessageFilter(this);
            _mostrado = null;
            Cambio?.Invoke();
        }

        public void Desactivar()
        {
            if (!Activo) return;
            Activo = false;
            Application.RemoveMessageFilter(this);
            _espera.Stop();
            _globo.Ocultar();
            Cursor.Current = Cursors.Default;
            Cambio?.Invoke();
        }

        // F1 fuera del modo: ayuda del control bajo el mouse. Devuelve false si no hay ayuda específica.
        public bool MostrarBajoMouse()
        {
            Control c = ControlBajoMouse();
            if (c == null) return false;
            TemaAyuda06AV tema = AyudaContextual06AV.DeControl(c, _area());
            if (tema == null || !tema.Especifica) return false;
            _globo.Mostrar(tema, Control.MousePosition, GestorIdioma06AV.Instancia.Obtener("ayuda_globo_pie_f1"));
            return true;
        }

        public void OcultarGlobo() => _globo.Ocultar();

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_KEYDOWN:
                    if ((Keys)(int)m.WParam == Keys.Escape) { Desactivar(); return true; }
                    return false;

                case WM_SETCURSOR:
                    if (DentroDelArea(Control.FromChildHandle(m.WParam)))
                    {
                        Cursor.Current = Cursors.Help;
                        return true;
                    }
                    return false;

                case WM_MOUSEMOVE:
                {
                    Control c = Control.FromChildHandle(m.HWnd);
                    if (!DentroDelArea(c)) return false;
                    Cursor.Current = Cursors.Help;
                    if (c != _pendiente)
                    {
                        _pendiente = c;
                        _espera.Stop();
                        _espera.Start();
                    }
                    return false;
                }

                case WM_LBUTTONDOWN:
                case WM_LBUTTONDBLCLK:
                case WM_RBUTTONDOWN:
                {
                    Control c = Control.FromChildHandle(m.HWnd);
                    if (DentroDelArea(c))
                    {
                        _espera.Stop();
                        MostrarDe(c, true);
                        return true;
                    }
                    if (c is GloboAyuda06AV) return true;
                    Desactivar();
                    return false;
                }

                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                    return DentroDelArea(Control.FromChildHandle(m.HWnd));
            }
            return false;
        }

        private void MostrarDe(Control c, bool forzar)
        {
            if (c == null || (!forzar && c == _mostrado)) return;
            _mostrado = c;
            TemaAyuda06AV tema = AyudaContextual06AV.DeControl(c, _area());
            if (tema == null) { _globo.Ocultar(); return; }
            _globo.Mostrar(tema, Control.MousePosition, GestorIdioma06AV.Instancia.Obtener("ayuda_globo_pie_modo"));
        }

        private bool DentroDelArea(Control c)
        {
            Control area = _area();
            if (c == null || area == null) return false;
            for (Control p = c; p != null; p = p.Parent)
                if (p == area) return true;
            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);

        private static Control ControlBajoMouse() => Control.FromChildHandle(WindowFromPoint(Control.MousePosition));

        public void Dispose()
        {
            Desactivar();
            _espera.Dispose();
            _globo.Dispose();
        }
    }
}
