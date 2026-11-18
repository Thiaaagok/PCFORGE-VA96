using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    // Botones de la ventana (minimizar, pantalla completa / restaurar y cerrar) en la barra
    // superior. La ventana no tiene borde de Windows, así que estos botones reemplazan a
    // los del sistema operativo.
    public partial class FRMMain
    {
        // Íconos de Segoe MDL2 Assets (los mismos que usa Windows en sus ventanas)
        private const string IcoMinimizar = "", IcoMaximizar = "", IcoRestaurar = "", IcoCerrar = "";

        private Button btnMinimizar, btnMaximizar, btnCerrarVentana;

        private void CrearBotonesVentana()
        {
            if (btnCerrarVentana != null) return;

            btnMinimizar = BotonVentana(IcoMinimizar);
            btnMaximizar = BotonVentana(IcoMaximizar);
            btnCerrarVentana = BotonVentana(IcoCerrar);
            btnMinimizar.Margin = new Padding(16, 0, 0, 0);   // separa los botones de ventana del resto

            btnMinimizar.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMaximizar.Click += (s, e) => AlternarPantallaCompleta();
            btnCerrarVentana.Click += (s, e) => SalirDelSistema();
            Resize += (s, e) => ActualizarBotonMaximizar();
            btnCerrarVentana.MouseEnter += (s, e) => btnCerrarVentana.ForeColor = Color.White;
            btnCerrarVentana.MouseLeave += (s, e) => btnCerrarVentana.ForeColor = Tema.Texto;

            flpTopActions.Controls.Add(btnMinimizar);
            flpTopActions.Controls.Add(btnMaximizar);
            flpTopActions.Controls.Add(btnCerrarVentana);

            TematizarBotonesVentana();
            ActualizarTooltipsVentana();
        }

        private static Button BotonVentana(string icono) => new Button
        {
            Text = icono,
            Size = new Size(40, 34),
            Margin = new Padding(2, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TabStop = false
        };

        private bool EnPantallaCompleta => Bounds == AreaUtil();

        private void ActualizarBotonMaximizar()
        {
            if (btnMaximizar == null || WindowState == FormWindowState.Minimized) return;
            btnMaximizar.Text = EnPantallaCompleta ? IcoRestaurar : IcoMaximizar;
            ActualizarTooltipsVentana();
        }

        private void TematizarBotonesVentana()
        {
            if (btnCerrarVentana == null) return;
            var fuente = new Font("Segoe MDL2 Assets", 10f, FontStyle.Regular);
            foreach (var b in new[] { btnMinimizar, btnMaximizar, btnCerrarVentana })
            {
                ConfigurarBotonTopbar(b, Tema.FondoSidebar, Tema.Texto);
                b.Font = fuente;
            }
            // cerrar: rojo al pasar el mouse, como en Windows
            btnCerrarVentana.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
            btnCerrarVentana.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 32, 20);
            ActualizarBotonMaximizar();
        }

        private void ActualizarTooltipsVentana()
        {
            if (btnCerrarVentana == null) return;
            var t = GestorIdioma06AV.Instancia;
            toolTipMain.SetToolTip(btnMinimizar, t.Obtener("ventana_minimizar"));
            toolTipMain.SetToolTip(btnMaximizar, t.Obtener(EnPantallaCompleta ? "ventana_restaurar" : "ventana_maximizar"));
            toolTipMain.SetToolTip(btnCerrarVentana, t.Obtener("ventana_cerrar"));
        }

        /// <summary>Cierra el sistema: pide confirmación, registra la salida en la bitácora y termina la aplicación.</summary>
        private void SalirDelSistema()
        {
            var t = GestorIdioma06AV.Instancia;
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("ventana_cerrar_confirmar"),
                titulo: t.Obtener("ventana_cerrar"),
                textoSi: t.Obtener("ventana_cerrar_si"),
                textoNo: t.Obtener("cancelar"),
                owner: this);
            if (!confirmado) return;

            var usuarioActual = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuarioActual != null)
            {
                try { new UsuariosBLL06AV().CambiarIdioma(usuarioActual.Dni, t.IdiomaActual); } catch { }
                try { new BitacoraBLL06AV().Logout(usuarioActual.Dni); } catch { }
            }
            try { UsuarioSesion06AV.Instancia().CerrarSesion(); } catch { }
            Application.Exit();
        }
    }
}
