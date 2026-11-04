using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.Controles;
using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public partial class FRMMain : Form
    {
        private const int SidebarAnchoExpandido = 260;
        private const int SidebarAnchoColapsado = 72;
        private bool _sidebarExpandido = true;
        private ItemSidebar06AV _moduloActivo;
        private string _claveActiva = "menu_inicio";
        private ToolStripMenuItem cambiarIdiomaToolStripMenuItem;
        private ContextMenuStrip cmsIdiomas;
        private ContextMenuStrip _menuFlotante;
        private Button btnTema;
        private ItemSidebar06AV _itemInicio;

        private const int IntervaloAutoBackupMs = 3 * 60 * 60 * 1000;
        private System.Windows.Forms.Timer _timerAutoBackup;
        private ToolStripMenuItem gestionBackupsToolStripMenuItem;

        public Usuario06AV Usuario = new Usuario06AV();

        public FRMMain()
        {
            InitializeComponent();
            ConfigurarInterfaz();
            CargarSesionEnEncabezado();
            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            Tema.TemaChanged += RefrescarTema;
            FormClosed += (s, e) => Tema.TemaChanged -= RefrescarTema;

            MostrarInicio();

            ConfigurarGestionBackups();
            IniciarAvisos();

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += PantallaCambiada;
            FormClosed += (s, e) => Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= PantallaCambiada;
        }

        // ── Tamaño de la ventana según la pantalla ──────────────────────────────
        // La ventana no tiene borde (FormBorderStyle.None): Windows no la achica ni deja
        // redimensionarla. En pantallas chicas (o con la escala de Windows al 125/150 %)
        // los 1300×788 del diseñador se salían por abajo y las barras de botones de cada
        // pantalla (Confirmar, Volver, Registrar entrega…) quedaban cortadas.
        // Ahora la ventana nunca pasa del área útil del monitor (sin tapar la barra de
        // tareas) y con doble clic en la barra superior se alterna entre ocupar toda la
        // pantalla y el tamaño normal.
        private Size _tamanioDisenio;          // el del diseñador (ya escalado por Windows)
        private Rectangle? _limitesNormales;
        private int _ultimoClicBarra;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _tamanioDisenio = Size;
            AjustarAPantalla();
        }

        private void PantallaCambiada(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed) BeginInvoke((Action)AjustarAPantalla);
        }

        private Rectangle AreaUtil() => Screen.FromControl(this).WorkingArea;

        private void AjustarAPantalla()
        {
            Rectangle area = AreaUtil();
            MinimumSize = new Size(Math.Min(900, area.Width), Math.Min(560, area.Height));
            MaximizedBounds = area;   // si Windows la maximiza, que respete la barra de tareas

            if (Width > area.Width || Height > area.Height)
            {
                // No entra: ocupa toda el área útil.
                Bounds = area;
                return;
            }
            if (!area.Contains(Bounds))
                Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }

        private void AlternarPantallaCompleta()
        {
            Rectangle area = AreaUtil();
            if (Bounds != area)
            {
                _limitesNormales = Bounds;
                Bounds = area;
                return;
            }

            // Volver al tamaño normal: el que tenía antes o el de diseño, siempre dentro de la pantalla.
            Size tam = _limitesNormales?.Size ?? _tamanioDisenio;
            if (tam.Width >= area.Width || tam.Height >= area.Height)
                tam = new Size(Math.Max(MinimumSize.Width, area.Width * 85 / 100),
                               Math.Max(MinimumSize.Height, area.Height * 85 / 100));
            tam = new Size(Math.Min(tam.Width, area.Width), Math.Min(tam.Height, area.Height));
            Bounds = new Rectangle(area.Left + (area.Width - tam.Width) / 2,
                                   area.Top + (area.Height - tam.Height) / 2,
                                   tam.Width, tam.Height);
        }


        private void MostrarInicio()
        {
            if (_itemInicio != null) SeleccionarModulo(_itemInicio);
            MostrarControl(new Controles.HomeControl06AV());
        }

        private void ConstruirMenuAgrupado()
        {
            flpModulos.SuspendLayout();
            var anteriores = flpModulos.Controls.Cast<Control>().ToList();
            flpModulos.Controls.Clear();
            foreach (var c in anteriores) c.Dispose();
            _navegacion.Clear();
            _moduloActivo = null;

            _itemInicio = CrearItem(GestorIdioma06AV.Instancia.Obtener("menu_inicio"), "\uE80F", false, "menu_inicio");
            _itemInicio.Click += (s, e) => MostrarInicio();
            flpModulos.Controls.Add(_itemInicio);

            AgregarGrupo("menu_grp_admin", "\uE713", new List<ItemMenu>
            {
                Item("usuarios", "\uE716", null, false, () => new Controles.UsuariosControl()),
                Item("titulo_roles", "\uE902", PatenteEnum06AV.GestionarRoles, false, () => new Controles.RolesControl()),
                Item("titulo_familias", "\uE8FD", PatenteEnum06AV.GestionarFamilias, false, () => new Controles.FamiliasControl()),
                Item("titulo_patentes", "\uE8D7", PatenteEnum06AV.GestionarPatentes, false, () => new Controles.PatentesControl()),
                Item("bitacora", "\uE9D5", null, true, () => new Controles.BitacoraControl()),
                ItemAccion("backup_menu", "\uE777", null, true, () => { using (var f = new FRMGestionBackup()) f.ShowDialog(this); }),
                Item("menu_serializacion", "\uE74E", PatenteEnum06AV.Serializar, false, () => new Controles.SerializacionControl06AV()),
            });

            AgregarGrupo("menu_grp_maestro", "\uE8F1", new List<ItemMenu>
            {
                Item("pcf_menu_clientes", "\uE716", PatenteEnum06AV.GestionarClientes, false, () => new Controles.ClientesControl()),
                Item("pcf_menu_proveedores", "\uE8D7", PatenteEnum06AV.GestionarProveedores, false, () => new Controles.ProveedoresControl()),
                Item("pcf_menu_componentes", "\uE950", PatenteEnum06AV.GestionarComponentes, false, () => new Controles.ComponentesControl()),
                Item("pcf_menu_bitacora_comp", "\uE81C", PatenteEnum06AV.GestionarComponentes, false, () => new Controles.BitacoraComponentesControl()),
                Item("pcf_menu_modelos", "\uE8A4", PatenteEnum06AV.GestionarModelosEstandar, false, () => new Controles.ModelosEstandarControl()),
                Item("cmp_menu_reglas", "\uE71C", PatenteEnum06AV.GestionarCompatibilidad, false, () => new Controles.ReglasCompatibilidadControl06AV()),
            });

            AgregarGrupo("menu_grp_compra", "\uE7BF", new List<ItemMenu>
            {
                Item("pcf_menu_compras", "\uE9D5", PatenteEnum06AV.GestionarCompras, false, () => new Controles.ComprasControl()),
                Item("pcf_menu_cotizaciones", "\uE8EC", PatenteEnum06AV.GestionarCotizaciones, false, () => new Controles.CotizacionesControl()),
                Item("menu_consultar_stock", "\uE7B8", PatenteEnum06AV.GestionarComponentes, false, () => new Controles.ConsultarStockControl()),
            });

            AgregarGrupo("menu_grp_venta", "\uE719", new List<ItemMenu>
            {
                Item("pcf_menu_ventas", "\uE719", PatenteEnum06AV.GestionarVentas, false, () => new Controles.VentasControl()),
                Item("pcf_menu_entregas", "\uE7B8", PatenteEnum06AV.GestionarEntregas, false, () => new Controles.EntregasControl()),
                Item("pcf_menu_recibos", "\uE8A5", PatenteEnum06AV.GestionarVentas, false, () => new Controles.RecibosControl()),
                Item("menu_facturas", "\uE8A5", PatenteEnum06AV.GestionarVentas, false, () => new Controles.FacturasControl()),
            });

            AgregarGrupo("menu_grp_produccion", "\uE713", new List<ItemMenu>
            {
                Item("pcf_menu_produccion", "\uE713", PatenteEnum06AV.GestionarProduccion, false, () => new Controles.ProduccionControl()),
                Item("pcf_menu_lineas", "\uE9F5", PatenteEnum06AV.GestionarLineasEnsamblaje, false, () => new Controles.LineasEnsamblajeControl()),
            });

            AgregarGrupo("menu_grp_reportes", "\uE9D2", new List<ItemMenu>
            {
                Item("menu_rep_ventas", "\uE9D2", PatenteEnum06AV.VerReporteVentas, false, () => new Controles.ReporteVentasControl()),
                Item("menu_rep_proveedores", "\uE8F1", PatenteEnum06AV.VerReporteProveedores, false, () => new Controles.ReporteProveedoresControl06AV()),
            });

            AgregarGrupo("menu_grp_ayuda", "\uE897", new List<ItemMenu>
            {
                Item("menu_guia", "\uE82D", null, false, () => new Controles.GuiaProcesosControl06AV()),
                ItemAccion("menu_acerca_de", "\uE946", null, false, MostrarAcercaDe),
            });

            var activo = flpModulos.Controls.OfType<ItemSidebar06AV>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, _claveActiva, StringComparison.OrdinalIgnoreCase));
            SeleccionarModulo(activo ?? _itemInicio);
            AjustarSidebar();
            flpModulos.ResumeLayout();
        }

        private ItemSidebar06AV CrearItem(string texto, string icono, bool esHijo, string clave)
        {
            var item = new ItemSidebar06AV(texto, icono, esHijo)
            {
                Tag = clave,
                Width = AnchoBotonSidebar(),
                Colapsado = !_sidebarExpandido
            };
            toolTipMain.SetToolTip(item, texto);
            item.MouseWheel += (s, e) => DesplazarMenu(e.Delta);
            return item;
        }

        // El scroll nativo de WinForms no se puede tematizar y, al aparecer la barra vertical,
        // achica el ancho útil y dispara también la horizontal; se reemplaza por uno propio.
        private const int AnchoBarraMenu = 4;
        private const int SeparacionBarraMenu = 6;
        private Panel _vistaMenu;
        private Panel _barraMenu;
        private int _desplazamientoMenu;
        private int? _arrastreBarraY;

        private void ConfigurarScrollMenu()
        {
            int indice = pnlSidebar.Controls.GetChildIndex(flpModulos);
            _vistaMenu = new Panel { Dock = DockStyle.Fill };
            pnlSidebar.Controls.Remove(flpModulos);
            pnlSidebar.Controls.Add(_vistaMenu);
            pnlSidebar.Controls.SetChildIndex(_vistaMenu, indice);

            flpModulos.AutoScroll = false;
            flpModulos.Dock = DockStyle.None;
            flpModulos.AutoSize = true;
            flpModulos.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpModulos.Location = Point.Empty;
            flpModulos.Margin = Padding.Empty;
            _vistaMenu.Controls.Add(flpModulos);

            _barraMenu = new Panel { Width = AnchoBarraMenu, Visible = false, Cursor = Cursors.Hand };
            _barraMenu.Paint += (s, e) =>
            {
                Pintura06AV.Calidad(e.Graphics);
                e.Graphics.Clear(_vistaMenu.BackColor);
                Pintura06AV.Rellenar(e.Graphics, new Rectangle(0, 0, _barraMenu.Width, _barraMenu.Height),
                                     AnchoBarraMenu / 2, Tema.SidebarLinea);
            };
            _barraMenu.MouseDown += (s, e) => _arrastreBarraY = e.Y;
            _barraMenu.MouseUp += (s, e) => _arrastreBarraY = null;
            _barraMenu.MouseMove += (s, e) =>
            {
                if (_arrastreBarraY == null) return;
                int recorrido = _vistaMenu.ClientSize.Height - _barraMenu.Height;
                int maximo = flpModulos.Height - _vistaMenu.ClientSize.Height;
                if (recorrido <= 0 || maximo <= 0) return;
                int nuevoTop = _barraMenu.Top + e.Y - _arrastreBarraY.Value;
                _desplazamientoMenu = nuevoTop * maximo / recorrido;
                ActualizarScrollMenu();
            };
            _vistaMenu.Controls.Add(_barraMenu);
            _barraMenu.BringToFront();

            _vistaMenu.MouseWheel += (s, e) => DesplazarMenu(e.Delta);
            flpModulos.MouseWheel += (s, e) => DesplazarMenu(e.Delta);
            _vistaMenu.ClientSizeChanged += (s, e) => { AjustarAnchosModulos(); ActualizarScrollMenu(); };
            flpModulos.SizeChanged += (s, e) => ActualizarScrollMenu();
        }

        private void DesplazarMenu(int delta)
        {
            _desplazamientoMenu -= delta * ItemSidebar06AV.AltoRaiz / 120;
            ActualizarScrollMenu();
        }

        private void ActualizarScrollMenu()
        {
            if (_vistaMenu == null) return;
            int vista = _vistaMenu.ClientSize.Height;
            int maximo = Math.Max(0, flpModulos.Height - vista);
            _desplazamientoMenu = Math.Max(0, Math.Min(_desplazamientoMenu, maximo));
            flpModulos.Top = -_desplazamientoMenu;

            _barraMenu.Visible = maximo > 0;
            if (maximo == 0) return;
            int alto = Math.Max(28, vista * vista / flpModulos.Height);
            int top = (vista - alto) * _desplazamientoMenu / maximo;
            _barraMenu.SetBounds(_vistaMenu.ClientSize.Width - AnchoBarraMenu, top, AnchoBarraMenu, alto);
            _barraMenu.Invalidate();
        }

        private static ItemMenu Item(string clave, string icono, PatenteEnum06AV? patente, bool soloAdmin, Func<UserControl> crear)
            => new ItemMenu { Clave = clave, Icono = icono, Patente = patente, SoloAdmin = soloAdmin, Crear = crear };

        private static ItemMenu ItemAccion(string clave, string icono, PatenteEnum06AV? patente, bool soloAdmin, Action accion)
            => new ItemMenu { Clave = clave, Icono = icono, Patente = patente, SoloAdmin = soloAdmin, Accion = accion };

        private void AgregarGrupo(string claveHeader, string icono, List<ItemMenu> items)
        {
            var sesion = UsuarioSesion06AV.Instancia();
            var visibles = items.Where(it =>
                (it.Patente == null || sesion.TienePermiso(it.Patente.Value)) &&
                (!it.SoloAdmin || EsAdministrador())).ToList();
            if (visibles.Count == 0) return;

            var grupo = CrearItem(GestorIdioma06AV.Instancia.Obtener(claveHeader), icono, false, claveHeader);
            grupo.EsGrupo = true;
            var hijos = new List<ItemSidebar06AV>();
            grupo.Click += (s, e) =>
            {
                if (!_sidebarExpandido) { MostrarMenuFlotante(grupo, hijos); return; }
                ExpandirGrupo(grupo, hijos, !grupo.Expandido);
            };
            flpModulos.Controls.Add(grupo);

            foreach (var it in visibles)
            {
                var hijo = CrearItem(GestorIdioma06AV.Instancia.Obtener(it.Clave), it.Icono, true, it.Clave);
                hijo.Padre = grupo;
                hijo.Visible = false;
                var itLocal = it;
                Action ejecutar = () =>
                {
                    if (itLocal.Crear != null) { SeleccionarModulo(hijo); MostrarControl(itLocal.Crear()); }
                    else itLocal.Accion?.Invoke();
                };
                if (itLocal.Crear != null) _navegacion[itLocal.Clave] = ejecutar;
                hijo.Click += (s, e) => ejecutar();
                hijos.Add(hijo);
                flpModulos.Controls.Add(hijo);
            }
            hijos[hijos.Count - 1].EsUltimoHijo = true;
            grupo.Tag = new GrupoSidebar(claveHeader, hijos, () => ExpandirGrupo(grupo, hijos, true));
        }

        private void ExpandirGrupo(ItemSidebar06AV grupo, List<ItemSidebar06AV> hijos, bool expandir)
        {
            grupo.Expandido = expandir;
            flpModulos.SuspendLayout();
            foreach (var h in hijos) h.Visible = expandir && _sidebarExpandido;
            flpModulos.ResumeLayout();
        }

        private void MostrarMenuFlotante(ItemSidebar06AV grupo, List<ItemSidebar06AV> hijos)
        {
            _menuFlotante?.Dispose();
            _menuFlotante = new ContextMenuStrip();
            RenderizadorMenu06AV.Aplicar(_menuFlotante);
            foreach (var h in hijos)
            {
                var hijo = h;
                var item = new ToolStripMenuItem(hijo.Texto);
                RenderizadorMenu06AV.PrepararItem(item);
                if (hijo == _moduloActivo) { item.Font = new Font(_menuFlotante.Font, FontStyle.Bold); item.ForeColor = Tema.Acento; }
                item.Click += (s, e) => hijo.InvokeClick();
                _menuFlotante.Items.Add(item);
            }
            _menuFlotante.Show(grupo, new Point(grupo.Width + 8, 0));
        }

        private int AnchoBotonSidebar() =>
            Math.Max(1, _vistaMenu.ClientSize.Width - (_sidebarExpandido ? SeparacionBarraMenu + AnchoBarraMenu : 0));

        private void CrearBotonTema()
        {
            if (btnTema != null) return;

            btnTema = new Button
            {
                Size = new Size(40, 34),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnTema.Click += (s, e) => Tema.ToggleTema();
            ConfigurarBotonTopbar(btnTema, Tema.FondoSidebar, Tema.Texto);
            btnTema.Font = new Font("Segoe UI Symbol", 12f);
            ActualizarTextoBotonTema();

            flpTopActions.Controls.Add(btnTema);
            flpTopActions.Controls.SetChildIndex(btnTema, 0);
            toolTipMain.SetToolTip(btnTema, GestorIdioma06AV.Instancia.Obtener("cambiar_tema"));
        }

        private void ActualizarTextoBotonTema()
        {
            if (btnTema != null)
                btnTema.Text = Tema.EsOscuro ? "☀" : "☾";
        }

        private void RefrescarTema()
        {
            BackColor = Tema.FondoApp;
            pnlTopBar.BackColor = Tema.FondoSidebar;
            pnlShell.BackColor = Tema.FondoApp;
            panelPrincipal.BackColor = Tema.FondoApp;
            AplicarColoresSidebar();
            lblSistema.ForeColor = Tema.TextoFuerte;

            ConfigurarBotonTopbar(btnToggleSidebar, Tema.FondoSidebar, Tema.Texto);
            btnToggleSidebar.Font = new Font("Segoe MDL2 Assets", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
            ConfigurarBotonTopbar(opcionesUsuarioBTN, Tema.FondoSidebar, Tema.Texto);
            ConfigurarBotonTopbar(btnIdioma, Tema.PrimarioSuave, Tema.Primario);
            ConfigurarBotonTopbar(btnCerrarSesion, Tema.FondoSidebar, Tema.Peligro);
            if (btnTema != null)
            {
                ConfigurarBotonTopbar(btnTema, Tema.FondoSidebar, Tema.Texto);
                btnTema.Font = new Font("Segoe UI Symbol", 12f);
                ActualizarTextoBotonTema();
            }
            TematizarBotonAvisos();

            ConfigurarPanelUsuario();
            ConfigurarMenuUsuario();

            if (panelPrincipal.Controls.Count > 0 && panelPrincipal.Controls[0] is UserControl uc)
                Tema.AplicarControl(uc);

            Invalidate(true);
        }

        private void MostrarAcercaDe()
        {
            ConfirmacionForm.MostrarInfo(
                "PC Forge / Cl\u00EDnica\nSistema de gesti\u00F3n de ensamblaje de PC.\nTrabajo Pr\u00E1ctico de Ingenier\u00EDa de Software.",
                GestorIdioma06AV.Instancia.Obtener("menu_grp_ayuda"),
                ConfirmacionForm.TipoConfirmacion.Info, this);
        }

        private void ConfigurarGestionBackups()
        {
            if (!EsAdministrador()) return;

            var t = GestorIdioma06AV.Instancia;

            gestionBackupsToolStripMenuItem = new ToolStripMenuItem(t.Obtener("backup_menu"));
            gestionBackupsToolStripMenuItem.Click += (s, e) =>
            {
                using (var f = new FRMGestionBackup())
                    f.ShowDialog(this);
            };
            ctxMenuUsuario.Items.Add(gestionBackupsToolStripMenuItem);

            _timerAutoBackup = new System.Windows.Forms.Timer { Interval = IntervaloAutoBackupMs };
            _timerAutoBackup.Tick += (s, e) => EjecutarBackupAutomatico();
            _timerAutoBackup.Start();
            FormClosed += (s, e) => _timerAutoBackup?.Stop();
        }

        private void EjecutarBackupAutomatico()
        {
            try
            {
                new IntegridadBLL06AV().RespaldarEnCarpetaPorDefecto();
            }
            catch
            {
            }
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            Text = "PC Forge";
            lblMenuPrincipal.Text = t.Obtener("menu_principal");
            lblSidebarFooter.Text = t.Obtener("sidebar_footer");

            ConstruirMenuAgrupado();
            CargarSesionEnEncabezado();

            cambiarContraseñaToolStripMenuItem.Text = t.Obtener("cambiar_contrasenia");
            reloginToolStripMenuItem.Text = t.Obtener("relogin");
            if (gestionBackupsToolStripMenuItem != null)
                gestionBackupsToolStripMenuItem.Text = t.Obtener("backup_menu");
            btnCerrarSesion.Text = t.Obtener("cerrar_sesion");
            btnIdioma.Text = t.IdiomaActual;

            if (cambiarIdiomaToolStripMenuItem == null)
            {
                cambiarIdiomaToolStripMenuItem = new ToolStripMenuItem
                {
                    Name = "cambiarIdiomaToolStripMenuItem",
                    Text = t.Obtener("cambiar_idioma")
                };
                cambiarIdiomaToolStripMenuItem.Click += cambiarIdiomaToolStripMenuItem_Click;
            }
            else
            {
                cambiarIdiomaToolStripMenuItem.Text = t.Obtener("cambiar_idioma");
            }

            ConstruirMenuIdiomas();

            toolTipMain.SetToolTip(btnIdioma, t.Obtener("cambiar_idioma"));
            toolTipMain.SetToolTip(btnCerrarSesion, t.Obtener("cerrar_sesion"));
            ActualizarTooltipAvisos();
            toolTipMain.SetToolTip(opcionesUsuarioBTN, t.Obtener("usuario"));
        }

        private void ConfigurarInterfaz()
        {
            DoubleBuffered = true;
            ConfigurarScrollMenu();

            pnlTopBar.BackColor = Tema.FondoSidebar;
            pnlShell.BackColor = Tema.FondoApp;
            panelPrincipal.BackColor = Tema.FondoApp;
            panelPrincipal.Padding = new Padding(20);

            pnlTopBar.Height = 60;
            pnlTopBar.Padding = new Padding(20, 0, 16, 0);
            flpTopActions.Padding = new Padding(0, 13, 0, 0);
            var familiaMarca = FRMLogin.FamiliaTitulos(out bool marcaPrivada);
            lblSistema.Font = familiaMarca != null
                ? new Font(familiaMarca, 30f, FontStyle.Regular, GraphicsUnit.Pixel)
                : new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
            lblSistema.UseCompatibleTextRendering = marcaPrivada;
            lblSistema.ForeColor = Tema.TextoFuerte;
            lblSistema.Width = SidebarAnchoExpandido - pnlTopBar.Padding.Left;
            lblSistema.Text = "PCFORGE";
            btnToggleSidebar.Dock = DockStyle.None;
            btnToggleSidebar.Size = new Size(36, 34);
            btnToggleSidebar.Location = new Point(lblSistema.Right + 16, 13);
            pnlTopBar.Paint += DibujarLineaInferior;

            lblSidebarFooter.Visible = false;
            lblMenuPrincipal.Height = 34;
            lblMenuPrincipal.TextAlign = ContentAlignment.BottomLeft;
            lblMenuPrincipal.Padding = new Padding(10, 0, 0, 6);
            pnlSidebar.Paint += DibujarBordeSidebar;
            pnlSidebar.Resize += (s, e) => pnlSidebar.Invalidate();
            lblAvatar.Cursor = Cursors.Hand;
            lblAvatar.Click += opcionesUsuarioBTN_Click;
            panel4.Resize += (s, e) => UbicarEncabezadoSidebar();
            AplicarColoresSidebar();

            ConfigurarBotonTopbar(btnToggleSidebar, Tema.FondoSidebar, Tema.Texto);
            btnToggleSidebar.Font = new Font("Segoe MDL2 Assets", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
            ConfigurarBotonTopbar(opcionesUsuarioBTN, Tema.FondoSidebar, Tema.Texto);
            ConfigurarBotonTopbar(btnIdioma, Tema.PrimarioSuave, Tema.Primario);
            ConfigurarBotonTopbar(btnCerrarSesion, Tema.FondoSidebar, Tema.Peligro);

            CrearBotonTema();
            CrearBotonAvisos();

            ConfigurarPanelUsuario();
            ConfigurarMenuUsuario();
            AjustarSidebar();


            HabilitarArrastreVentana(pnlTopBar);
            HabilitarArrastreVentana(lblSistema);
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void HabilitarArrastreVentana(Control control)
        {
            if (control == null) return;
            control.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                // Doble clic detectado a mano: el arrastre de la ventana se "come" el segundo clic.
                int ahora = Environment.TickCount;
                if (ahora - _ultimoClicBarra <= SystemInformation.DoubleClickTime)
                {
                    _ultimoClicBarra = 0;
                    AlternarPantallaCompleta();
                    return;
                }
                _ultimoClicBarra = ahora;
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            };
        }

        private bool _ajustandoAnchos;

        private void AjustarAnchosModulos()
        {
            if (_ajustandoAnchos) return;
            _ajustandoAnchos = true;
            flpModulos.SuspendLayout();
            int ancho = AnchoBotonSidebar();
            foreach (Control c in flpModulos.Controls)
                c.Width = ancho;
            flpModulos.ResumeLayout();
            _ajustandoAnchos = false;
        }

        private void ConfigurarPanelUsuario()
        {
            panel4.BackColor = Tema.FondoSidebar;
            lblAvatar.BackColor = Tema.Primario;
            lblAvatar.ForeColor = Tema.TextoInvertido;
            lblAvatar.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            lblRol.Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
            lblUsuario.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            lblRol.ForeColor = Tema.Acento;
            lblUsuario.ForeColor = Tema.TextoFuerte;
            lblRol.BackColor = lblUsuario.BackColor = Tema.FondoSidebar;
            opcionesUsuarioBTN.Text = "";
            opcionesUsuarioBTN.Font = new Font("Segoe MDL2 Assets", 8f);
            opcionesUsuarioBTN.FlatAppearance.BorderSize = 0;
            opcionesUsuarioBTN.BackColor = Tema.FondoSidebar;
            opcionesUsuarioBTN.ForeColor = Tema.TextoSuave;
            opcionesUsuarioBTN.FlatAppearance.MouseOverBackColor = Tema.SidebarHover;
            AplicarAvatarCircular();
        }

        private void AplicarColoresSidebar()
        {
            pnlSidebar.BackColor = Tema.FondoSidebar;
            flpModulos.BackColor = Tema.FondoSidebar;
            _vistaMenu.BackColor = Tema.FondoSidebar;
            _barraMenu.Invalidate();
            lblMenuPrincipal.BackColor = Tema.FondoSidebar;
            lblMenuPrincipal.ForeColor = Tema.TextoSuave;
            lblMenuPrincipal.Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold);
            foreach (Control c in flpModulos.Controls) c.Invalidate();
            pnlSidebar.Invalidate();
        }

        private void DibujarBordeSidebar(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Tema.BordeSidebar))
                e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
        }

        private void UbicarEncabezadoSidebar()
        {
            const int avatar = 40;
            lblAvatar.Size = new Size(avatar, avatar);
            AplicarAvatarCircular();
            lblRol.Visible = lblUsuario.Visible = opcionesUsuarioBTN.Visible = _sidebarExpandido;

            if (!_sidebarExpandido)
            {
                lblAvatar.Location = new Point((panel4.Width - avatar) / 2, (panel4.Height - avatar) / 2);
                return;
            }

            lblAvatar.Location = new Point(4, (panel4.Height - avatar) / 2);
            int x = lblAvatar.Right + 10;
            int ancho = Math.Max(10, panel4.Width - x - 30);
            lblRol.SetBounds(x, lblAvatar.Top + 1, ancho, 15);
            lblUsuario.SetBounds(x, lblRol.Bottom, ancho, 20);
            opcionesUsuarioBTN.SetBounds(panel4.Width - 26, (panel4.Height - 26) / 2, 24, 26);
        }

        private void ConfigurarMenuUsuario()
        {
            RenderizadorMenu06AV.Aplicar(ctxMenuUsuario);
        }

        private readonly HashSet<Button> _botonesRedondeados = new HashSet<Button>();

        private void ConfigurarBotonTopbar(Button btn, Color fondo, Color texto)
        {
            btn.BackColor = fondo;
            btn.ForeColor = texto;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.BorderColor = fondo;
            btn.FlatAppearance.MouseOverBackColor = fondo == Tema.FondoSidebar ? Tema.SidebarHover : Pintura06AV.Mezclar(fondo, Tema.TextoFuerte, 0.08f);
            btn.FlatAppearance.MouseDownBackColor = Tema.SidebarActivo;
            btn.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.UseVisualStyleBackColor = false;

            if (_botonesRedondeados.Add(btn))
                btn.SizeChanged += (s, e) => RedondearBoton(btn);
            RedondearBoton(btn);
        }

        private static void RedondearBoton(Button btn)
        {
            var anterior = btn.Region;
            using (var path = Pintura06AV.Redondeado(new Rectangle(0, 0, btn.Width, btn.Height), 8))
                btn.Region = new Region(path);
            anterior?.Dispose();
        }

        private void AplicarAvatarCircular()
        {
            lblAvatar.Region?.Dispose();
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, lblAvatar.Width - 1, lblAvatar.Height - 1);
                lblAvatar.Region = new Region(path);
            }
        }

        private void DibujarLineaInferior(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Tema.BordeSidebar))
            {
                e.Graphics.DrawLine(pen, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            }
        }

        private void SeleccionarModulo(Control control)
        {
            if (!(control is ItemSidebar06AV item)) return;

            _moduloActivo = item;
            _claveActiva = item.Tag as string;
            foreach (var i in flpModulos.Controls.OfType<ItemSidebar06AV>())
                i.Activo = i == item || i == item.Padre;

            if (item.Padre?.Tag is GrupoSidebar grupo && !item.Padre.Expandido)
                grupo.Expandir();
        }

        public void MostrarControl(UserControl control)
        {
            foreach (Control c in panelPrincipal.Controls)
            {
                c.Dispose();
            }

            panelPrincipal.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelPrincipal.Controls.Add(control);

            if (control is IIdiomaAplicable06AV aplicable)
                aplicable.AplicarIdioma();
        }

        /// <summary>True si el modulo indicado por su clave esta disponible para el usuario actual (tiene permiso).</summary>
        public bool PuedeNavegarAModulo(string clave)
            => !string.IsNullOrEmpty(clave) && _navegacion.ContainsKey(clave);

        /// <summary>Abre el modulo indicado por su clave de menu, como si el usuario hiciera clic en el item del sidebar.</summary>
        public void NavegarAModulo(string clave)
        {
            if (!string.IsNullOrEmpty(clave) && _navegacion.TryGetValue(clave, out Action ir)) ir();
        }

        /// <summary>Devuelve el item del menu lateral con esa clave (para resaltarlo en la guia interactiva).</summary>
        public Control ItemSidebarPorClave(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return null;
            foreach (var it in flpModulos.Controls.OfType<ItemSidebar06AV>())
                if (string.Equals(it.Tag as string, clave, StringComparison.OrdinalIgnoreCase)) return it;
            return null;
        }

        /// <summary>El area donde se cargan las pantallas (para resaltarla en la guia interactiva).</summary>
        public Control AreaContenido() { return panelPrincipal; }

        private void bitacoraBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(bitacoraBTN);
            MostrarControl(new BitacoraControl());
        }

        private void usuariosBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(usuariosBTN);
            MostrarControl(new UsuariosControl());
        }

        private void rolesBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(rolesBTN);
            MostrarControl(new RolesControl());
        }

        private void familiasBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(familiasBTN);
            MostrarControl(new FamiliasControl());
        }

        private void patentesBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(patentesBTN);
            MostrarControl(new PatentesControl());
        }

        private void btnToggleSidebar_Click(object sender, EventArgs e)
        {
            _sidebarExpandido = !_sidebarExpandido;
            AjustarSidebar();
        }

        private void AjustarSidebar()
        {
            pnlSidebar.SuspendLayout();
            flpModulos.SuspendLayout();
            pnlSidebar.Width = _sidebarExpandido ? SidebarAnchoExpandido : SidebarAnchoColapsado;
            pnlSidebar.Padding = new Padding(14, 12, 15, 16);
            lblMenuPrincipal.Visible = _sidebarExpandido;
            btnToggleSidebar.Text = _sidebarExpandido ? "" : "";

            foreach (var item in flpModulos.Controls.OfType<ItemSidebar06AV>())
            {
                item.Colapsado = !_sidebarExpandido;
                if (item.EsHijo) item.Visible = _sidebarExpandido && item.Padre.Expandido;
            }
            flpModulos.ResumeLayout();
            pnlSidebar.ResumeLayout();
            AjustarAnchosModulos();
            UbicarEncabezadoSidebar();
            pnlSidebar.Invalidate();
        }

        private void opcionesUsuarioBTN_Click(object sender, EventArgs e)
        {
            ctxMenuUsuario.Show(panel4, new Point(0, panel4.Height + 4));
        }

        private void btnIdioma_Click(object sender, EventArgs e)
        {
            cmsIdiomas.Show(btnIdioma, new Point(btnIdioma.Width, btnIdioma.Height + 6), ToolStripDropDownDirection.BelowLeft);
        }

        private void cambiarIdiomaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmsIdiomas.Show(Cursor.Position);
        }

        private void ConstruirMenuIdiomas()
        {
            if (cmsIdiomas != null) return;

            cmsIdiomas = new ContextMenuStrip();

            var nombres = new System.Collections.Generic.Dictionary<string, string>
            {
                { GestorIdioma06AV.ES, "Español" },
                { GestorIdioma06AV.EN, "English" },
                { GestorIdioma06AV.PT, "Português" }
            };

            foreach (string codigo in GestorIdioma06AV.IdiomasDisponibles)
            {
                string texto = nombres.TryGetValue(codigo, out var n) ? n : codigo;
                var item = new ToolStripMenuItem(texto) { Tag = codigo };
                item.Click += (s, e) => CambiarIdiomaA(((ToolStripMenuItem)s).Tag.ToString());
                cmsIdiomas.Items.Add(item);
            }

            RenderizadorMenu06AV.Aplicar(cmsIdiomas);
            cmsIdiomas.Opening += (s, e) =>
            {
                foreach (ToolStripItem item in cmsIdiomas.Items)
                {
                    bool actual = (string)item.Tag == GestorIdioma06AV.Instancia.IdiomaActual;
                    item.Font = new Font(cmsIdiomas.Font, actual ? FontStyle.Bold : FontStyle.Regular);
                    item.ForeColor = actual ? Tema.Acento : Tema.Texto;
                }
            };
        }

        private void CambiarIdiomaA(string nuevo)
        {
            var t = GestorIdioma06AV.Instancia;
            if (t.IdiomaActual == nuevo) return;

            try
            {
                t.CambiarIdioma(nuevo);

                ConfirmacionForm.MostrarInfo(
                    t.Obtener("idioma_cambiado_sesion"),
                    titulo: t.Obtener("idioma"),
                    owner: this);
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            cerrarSesiónToolStripMenuItem_Click(sender, e);
        }

        private void cambiarContraseñaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var usuarioActual = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuarioActual == null)
            {
                CerrarSesionYVolverALogin();
                return;
            }

            using (var f = new FRMCambiarContrasenia(usuarioActual.Dni))
            {
                f.ShowDialog(this);

                if (f.ContraseniaCambiada)
                {
                    CerrarSesionYVolverALogin();
                }
            }
        }

        private void CerrarSesionYVolverALogin()
        {
            UsuarioSesion06AV.Instancia().CerrarSesion();
            var login = new FRMLogin();
            login.Show();
            Close();
        }

        public void ForzarReloginPorCambioDeRol()
        {
            var t = GestorIdioma06AV.Instancia;
            ConfirmacionForm.MostrarInfo(
                t.Obtener("rol_modificado_relogin"),
                titulo: t.Obtener("aviso"),
                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                owner: this);

            CerrarSesionYVolverALogin();
        }

        private void reloginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_relogin"),
                titulo: t.Obtener("relogin"),
                textoSi: t.Obtener("si_relogin"),
                textoNo: t.Obtener("cancelar"),
                owner: this);

            if (!confirmado) return;

            Hide();

            var login = new FRMLogin();
            login.FormClosed += (s, args) => Close();
            login.Show();
        }

        private void cerrarSesiónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_cerrar_sesion"),
                titulo: t.Obtener("cerrar_sesion"),
                textoSi: t.Obtener("si_cerrar"),
                textoNo: t.Obtener("cancelar"),
                owner: this);

            if (!confirmado) return;

            Hide();

            var usuarioActual = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuarioActual != null)
            {
                try
                {
                    new UsuariosBLL06AV().CambiarIdioma(usuarioActual.Dni, t.IdiomaActual);
                }
                catch
                {
                }

                new BitacoraBLL06AV().Logout(usuarioActual.Dni);
            }

            UsuarioSesion06AV.Instancia().CerrarSesion();
            var login = new FRMLogin();
            login.FormClosed += (s, args) => Close();
            login.Show();
        }

        private void CargarSesionEnEncabezado()
        {
            var sesion = UsuarioSesion06AV.Instancia();
            Usuario = sesion.UsuarioActual ?? new Usuario06AV();

            lblUsuario.Text = sesion.UsuarioActual != null
                ? sesion.NombreCompleto()
                : GestorIdioma06AV.Instancia.Obtener("usuario");
            lblRol.Text = "PCFORGE";
            lblAvatar.Text = ObtenerIniciales(sesion.UsuarioActual);

            string detalle = lblUsuario.Text + (sesion.Rol != null ? "\n" + sesion.Rol.Descripcion : string.Empty);
            foreach (Control c in new Control[] { panel4, lblAvatar, lblUsuario, lblRol })
                toolTipMain.SetToolTip(c, detalle);
        }

        private bool EsAdministrador()
        {
            var rol = UsuarioSesion06AV.Instancia().Rol;
            return rol != null &&
                   string.Equals(rol.Descripcion, "Administrador", StringComparison.OrdinalIgnoreCase);
        }

        private string ObtenerIniciales(Usuario06AV usuario)
        {
            if (usuario == null)
            {
                return "US";
            }

            string nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? "U" : usuario.Nombre.Trim().Substring(0, 1);
            string apellido = string.IsNullOrWhiteSpace(usuario.Apellido) ? "S" : usuario.Apellido.Trim().Substring(0, 1);
            return (nombre + apellido).ToUpperInvariant();
        }

        private sealed class ItemMenu
        {
            public string Clave;
            public string Icono;
            public PatenteEnum06AV? Patente;
            public bool SoloAdmin;
            public Func<UserControl> Crear;
            public Action Accion;
        }

        private sealed class GrupoSidebar
        {
            public GrupoSidebar(string clave, List<ItemSidebar06AV> hijos, Action expandir)
            {
                Clave = clave;
                Hijos = hijos;
                Expandir = expandir;
            }

            public string Clave { get; }
            public List<ItemSidebar06AV> Hijos { get; }
            public Action Expandir { get; }
        }

    }
}
