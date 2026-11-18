using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    // Ayuda en línea: botón "?" de la barra superior, panel "Ayuda de esta pantalla",
    // ayuda por posición del mouse y atajos F1 / Shift+F1.
    public partial class FRMMain
    {
        private Button btnAyuda;
        private PanelAyuda06AV _panelAyuda;
        private ModoAyudaMouse06AV _modoAyuda;

        private void CrearBotonAyuda()
        {
            if (btnAyuda != null) return;

            btnAyuda = new Button
            {
                Size = new Size(40, 34),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false,
                Text = "?"
            };
            TematizarBotonAyuda();
            btnAyuda.Click += (s, e) => AlternarPanelAyuda();
            flpTopActions.Controls.Add(btnAyuda);
            flpTopActions.Controls.SetChildIndex(btnAyuda, 0);

            _modoAyuda = new ModoAyudaMouse06AV(() => panelPrincipal);
            _modoAyuda.Cambio += () =>
            {
                _panelAyuda?.ModoMouse(_modoAyuda.Activo);
                TematizarBotonAyuda();
            };
            FormClosed += (s, e) => _modoAyuda.Dispose();
            Deactivate += (s, e) => _modoAyuda.OcultarGlobo();
            ActualizarTooltipAyuda();
        }

        private void TematizarBotonAyuda()
        {
            if (btnAyuda == null) return;
            bool activo = _modoAyuda?.Activo == true;
            ConfigurarBotonTopbar(btnAyuda, activo ? Tema.Acento : Tema.FondoSidebar, activo ? Tema.TextoInvertido : Tema.Texto);
            btnAyuda.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
        }

        private void ActualizarTooltipAyuda()
        {
            if (btnAyuda != null)
                toolTipMain.SetToolTip(btnAyuda, GestorIdioma06AV.Instancia.Obtener("ayuda_btn_tooltip"));
        }

        private void AlternarPanelAyuda()
        {
            if (_panelAyuda != null && _panelAyuda.Visible) CerrarPanelAyuda();
            else AbrirPanelAyuda();
        }

        private void AbrirPanelAyuda()
        {
            if (_panelAyuda == null)
            {
                _panelAyuda = new PanelAyuda06AV { Visible = false };
                _panelAyuda.CerrarSolicitado += CerrarPanelAyuda;
                _panelAyuda.ModoMouseSolicitado += () => _modoAyuda.Alternar();
                _panelAyuda.GuiaSolicitada += id => { CerrarPanelAyuda(); AbrirGuia(id); };
                pnlShell.Controls.Add(_panelAyuda);
                panelPrincipal.BringToFront();
            }
            AyudaContextual06AV.ModuloActual = _claveActiva;
            _panelAyuda.Mostrar(_claveActiva, PantallaActual(), _modoAyuda.Activo);
            _panelAyuda.Visible = true;
        }

        private void CerrarPanelAyuda()
        {
            if (_panelAyuda != null) _panelAyuda.Visible = false;
        }

        private Control PantallaActual() => panelPrincipal.Controls.Count > 0 ? panelPrincipal.Controls[0] : null;

        // Se llama cada vez que cambia la pantalla del área de trabajo.
        private void AyudaPantallaCambiada()
        {
            AyudaContextual06AV.ModuloActual = _claveActiva;
            _modoAyuda?.OcultarGlobo();
            if (_panelAyuda != null && _panelAyuda.Visible)
                BeginInvoke(new Action(() => _panelAyuda.Mostrar(_claveActiva, PantallaActual(), _modoAyuda.Activo)));
        }

        // ── Recorridos guiados ─────────────────────────────────────────────────

        public const string RecorridoBienvenida = "bienvenida";

        /// <summary>Elementos de la ventana principal que el recorrido de bienvenida ilumina.</summary>
        public Control ElementoGuia(string id)
        {
            switch (id)
            {
                case "usuario": return panel4;
                case "inicio": return _itemInicio;
                case "menu": return _vistaMenu;
                case "contraer": return btnToggleSidebar;
                case "ayuda": return btnAyuda;
                case "avisos": return btnAvisos;
                case "tema": return btnTema;
                case "idioma": return btnIdioma;
                case "salir": return btnCerrarSesion;
                case "guia": return ItemSidebarPorClave("menu_grp_ayuda");
                default: return null;
            }
        }

        /// <summary>Abre el centro de ayuda; si se indica, en ese tema del manual.</summary>
        public void AbrirGuia(string temaId)
        {
            NavegarAModulo("menu_guia");
            if (!string.IsNullOrEmpty(temaId) && PantallaActual() is Controles.GuiaProcesosControl06AV guia)
                guia.SeleccionarTema(temaId);
        }

        /// <summary>Inicia un recorrido: "bienvenida", "pn1" o "pn2".</summary>
        public void IniciarRecorrido(string cual)
        {
            List<PasoCoach06AV> pasos;
            switch (cual)
            {
                case "pn1": pasos = CoachMarks06AV.TourPN1(); break;
                case "pn2": pasos = CoachMarks06AV.TourPN2(); break;
                default: pasos = CoachMarks06AV.TourBienvenida(); break;
            }
            CerrarPanelAyuda();
            _modoAyuda?.Desactivar();
            string usuario = UsuarioSesion06AV.Instancia().UsuarioActual?.Login;
            new CoachMarks06AV(this, pasos, () => CoachMarks06AV.MarcarVisto(cual, usuario)).Iniciar();
        }

        // El primer ingreso de cada usuario muestra el recorrido de bienvenida (una sola vez por usuario en esta PC).
        private void ProgramarBienvenida()
        {
            Shown += (s, e) =>
            {
                string usuario = UsuarioSesion06AV.Instancia().UsuarioActual?.Login;
                if (CoachMarks06AV.YaVisto(RecorridoBienvenida, usuario)) return;
                var espera = new Timer { Interval = 700 };
                espera.Tick += (s2, e2) =>
                {
                    espera.Stop();
                    espera.Dispose();
                    if (!IsDisposed && Visible) IniciarRecorrido(RecorridoBienvenida);
                };
                espera.Start();
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && CoachMarks06AV.CerrarActivo()) return true;
            {
                if (!_modoAyuda.Activo && !_modoAyuda.MostrarBajoMouse())
                    AbrirPanelAyuda();
                return true;
            }
            if (keyData == (Keys.Shift | Keys.F1) && _modoAyuda != null)
            {
                _modoAyuda.Alternar();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
