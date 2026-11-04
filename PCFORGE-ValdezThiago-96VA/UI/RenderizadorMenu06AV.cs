using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal class RenderizadorMenu06AV : ToolStripProfessionalRenderer
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int atributo, ref int valor, int tamanio);

        private static readonly RenderizadorMenu06AV Instancia = new RenderizadorMenu06AV();

        private RenderizadorMenu06AV() { RoundedEdges = false; }

        public static void Aplicar(ToolStripDropDown menu)
        {
            if (menu.Renderer == Instancia) { Tematizar(menu); return; }
            menu.Renderer = Instancia;
            menu.Font = new Font("Segoe UI", 9.5f);
            menu.Padding = new Padding(4, 6, 4, 6);
            menu.DropShadowEnabled = true;
            if (menu is ToolStripDropDownMenu conMargen)
            {
                conMargen.ShowImageMargin = false;
                conMargen.ShowCheckMargin = false;
            }
            Tematizar(menu);
            menu.Opening += (s, e) => Tematizar(menu);
            menu.HandleCreated += (s, e) => RedondearVentana(menu.Handle);
            if (menu.IsHandleCreated) RedondearVentana(menu.Handle);
        }

        public static void PrepararItem(ToolStripItem item)
        {
            item.Padding = new Padding(6, 7, 18, 7);
        }

        private static void Tematizar(ToolStripDropDown menu)
        {
            menu.BackColor = Tema.FondoSidebar;
            menu.ForeColor = Tema.Texto;
            foreach (ToolStripItem item in menu.Items)
                if (item is ToolStripMenuItem && item.Padding.Top < 7) PrepararItem(item);
        }

        // Windows 11 redondea los menús propios del sistema; los ToolStripDropDown quedan rectos si no se pide.
        private static void RedondearVentana(IntPtr handle)
        {
            try
            {
                int preferencia = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preferencia, sizeof(int));
            }
            catch { }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var b = new SolidBrush(Tema.FondoSidebar))
                e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var p = new Pen(Tema.BordeSidebar))
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            Pintura06AV.Calidad(e.Graphics);
            var caja = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
            Pintura06AV.Rellenar(e.Graphics, caja, 6, Tema.SidebarHover);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Selected && e.TextColor == Tema.Texto) e.TextColor = Tema.TextoFuerte;
            if (!e.Item.Enabled) e.TextColor = Tema.TextoSuave;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var p = new Pen(Tema.BordeSidebar))
                e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Tema.TextoSuave;
            base.OnRenderArrow(e);
        }
    }
}
