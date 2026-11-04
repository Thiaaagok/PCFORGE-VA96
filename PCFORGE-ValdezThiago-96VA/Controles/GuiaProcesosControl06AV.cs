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
        private sealed class Paso06AV
        {
            public string Codigo;
            public string Titulo;
            public string Rol;
            public string Donde;
            public string ClaveModulo;
            public string[] Pasos;
            public string Resultado;
            public string Problema;
        }

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

        private readonly List<Paso06AV> _pn1 = new List<Paso06AV>();
        private readonly List<Paso06AV> _pn2 = new List<Paso06AV>();
        private readonly List<BotonPaso06AV> _botones = new List<BotonPaso06AV>();

        private bool _esPn1 = true;
        private int _indice;

        private List<Paso06AV> Actual => _esPn1 ? _pn1 : _pn2;

        public GuiaProcesosControl06AV()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ConstruirDatos();
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

        private void RenderizarDetalle(Paso06AV p)
        {
            var t = GestorIdioma06AV.Instancia;
            _flujo.SuspendLayout();
            _flujo.Controls.Clear();

            _flujo.Controls.Add(Titulo(p.Codigo + "  -  " + p.Titulo));
            _flujo.Controls.Add(Encabezado(t.Obtener("guia_quien_donde")));
            _flujo.Controls.Add(Parrafo(t.Obtener("guia_rol") + ": " + p.Rol));
            _flujo.Controls.Add(Parrafo(t.Obtener("guia_donde") + ": " + p.Donde));

            _flujo.Controls.Add(Encabezado(t.Obtener("guia_pasos")));
            for (int i = 0; i < p.Pasos.Length; i++)
                _flujo.Controls.Add(Parrafo((i + 1) + ".  " + p.Pasos[i]));

            _flujo.Controls.Add(Encabezado(t.Obtener("guia_resultado")));
            _flujo.Controls.Add(Parrafo(p.Resultado));

            if (!string.IsNullOrEmpty(p.Problema))
            {
                _flujo.Controls.Add(Encabezado(t.Obtener("guia_problema")));
                _flujo.Controls.Add(Parrafo(p.Problema));
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
            if (_botones.Count == 0) ConstruirBotonesLista();
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

        // -- Contenido de los procesos --------------------------------------------------------

        private void ConstruirDatos()
        {
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU01", Titulo = "Registrar cliente",
                Rol = "Ventas", Donde = "Maestro > Clientes", ClaveModulo = "pcf_menu_clientes",
                Pasos = new[]
                {
                    "Entra al menu Clientes y elegi la opcion para dar de alta un cliente nuevo.",
                    "Completa los datos obligatorios (nombre, apellido, documento, contacto). Los campos vacios o invalidos se marcan y el sistema no deja guardar.",
                    "El sistema controla que el documento no este ya cargado. Si el cliente existe, no se duplica: buscalo en la grilla y usa el registro existente.",
                    "Presiona Guardar."
                },
                Resultado = "Un mensaje de confirmacion y el cliente visible en la grilla, listo para elegirlo en una venta.",
                Problema = "Un mensaje de datos incompletos significa que falta un campo obligatorio; uno de cliente duplicado significa que ya estaba registrado."
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU03", Titulo = "Registrar venta",
                Rol = "Ventas", Donde = "Venta > Ventas", ClaveModulo = "pcf_menu_ventas",
                Pasos = new[]
                {
                    "Elegi Nueva venta y selecciona el cliente (debe estar cargado, CU01).",
                    "Defini la computadora: parti de un modelo predefinido o arma una configuracion a medida agregando componentes con su cantidad.",
                    "El sistema valida la compatibilidad entre componentes y que haya stock disponible de cada uno.",
                    "Revisa el precio pactado, que queda fijado en la venta aunque despues cambien los precios de lista.",
                    "Confirma la venta."
                },
                Resultado = "La venta queda pendiente de senia y el sistema reserva el stock de los componentes para que nadie mas los use.",
                Problema = "Si falta stock de un componente, el sistema lo informa y no confirma: cambia el componente o espera reposicion (PN2)."
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU02", Titulo = "Registrar senia y emitir recibo",
                Rol = "Ventas / Caja", Donde = "Venta > Ventas (o Recibos)", ClaveModulo = "pcf_menu_ventas",
                Pasos = new[]
                {
                    "Busca la venta en la grilla y elegi Registrar senia.",
                    "Ingresa el monto y el medio de pago. El monto no puede ser cero ni superar el saldo.",
                    "Confirma el pago.",
                    "Imprimi o guarda el recibo para entregarselo al cliente."
                },
                Resultado = "El pago queda asociado a la venta, se emite el recibo y la venta pasa a seniada, habilitada para produccion.",
                Problema = "Si rechaza el monto, revisa que no exceda lo adeudado; si la venta no aparece, verifica que no este ya seniada."
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU04", Titulo = "Generar orden de produccion",
                Rol = "Produccion", Donde = "Produccion > Produccion", ClaveModulo = "pcf_menu_produccion",
                Pasos = new[]
                {
                    "Abri la lista de ventas seniadas y elegi la que se va a fabricar.",
                    "Selecciona Generar orden de produccion.",
                    "Asigna una linea de ensamblaje libre (el sistema solo ofrece lineas disponibles).",
                    "Confirma."
                },
                Resultado = "Se crea la orden, la linea queda ocupada y los componentes reservados pasan a consumirse del stock.",
                Problema = "Si no hay lineas libres, espera a que se libere una (se libera al terminar un ensamblaje)."
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU05", Titulo = "Ensamblar computadora",
                Rol = "Produccion", Donde = "Produccion > Produccion", ClaveModulo = "pcf_menu_produccion",
                Pasos = new[]
                {
                    "Abri la orden y marca el inicio del ensamblaje.",
                    "Al terminar el armado fisico, registra la finalizacion de la orden."
                },
                Resultado = "La orden pasa a ensamblada y queda lista para el control de calidad. La linea de ensamblaje se libera.",
                Problema = null
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU06", Titulo = "Controlar calidad",
                Rol = "Calidad", Donde = "Produccion > Produccion", ClaveModulo = "pcf_menu_produccion",
                Pasos = new[]
                {
                    "Busca las ordenes ensambladas pendientes de control.",
                    "Abri la orden y completa el control con las verificaciones y observaciones.",
                    "Elegi el resultado: aprobado o rechazado."
                },
                Resultado = "Si se aprueba, la computadora queda lista para entrega; si se rechaza, vuelve a produccion y se repite el control.",
                Problema = "Un rechazo no es un error del sistema, es parte del proceso: anota siempre la causa en las observaciones."
            });
            _pn1.Add(new Paso06AV
            {
                Codigo = "CU07", Titulo = "Entregar computadora y emitir factura",
                Rol = "Ventas / Entregas", Donde = "Venta > Entregas", ClaveModulo = "pcf_menu_entregas",
                Pasos = new[]
                {
                    "Busca la venta lista para entregar.",
                    "Registra el cobro del saldo restante, si lo hay (no se habilita la entrega con deuda).",
                    "Confirma la entrega.",
                    "Imprimi la factura y entregasela al cliente junto con la computadora."
                },
                Resultado = "La venta pasa a entregada y el sistema emite la factura de venta. Con esto termina PN1.",
                Problema = "Si no permite entregar, revisa que la calidad este aprobada y que el saldo este cancelado."
            });

            _pn2.Add(new Paso06AV
            {
                Codigo = "CU08", Titulo = "Solicitar cotizacion",
                Rol = "Compras", Donde = "Compra > Cotizaciones", ClaveModulo = "pcf_menu_cotizaciones",
                Pasos = new[]
                {
                    "Elegi Nuevo pedido de cotizacion.",
                    "Selecciona el proveedor y agrega los componentes con la cantidad que necesitas.",
                    "Confirma."
                },
                Resultado = "El pedido queda registrado como enviado al proveedor, a la espera de su respuesta.",
                Problema = "Si el proveedor no aparece, primero hay que darlo de alta en el maestro de proveedores."
            });
            _pn2.Add(new Paso06AV
            {
                Codigo = "CU09", Titulo = "Registrar cotizacion y generar orden de compra",
                Rol = "Compras", Donde = "Compra > Cotizaciones / Compras", ClaveModulo = "pcf_menu_compras",
                Pasos = new[]
                {
                    "Abri el pedido de cotizacion pendiente.",
                    "Carga los precios unitarios que respondio el proveedor para cada componente.",
                    "Si la oferta conviene, elegi Generar orden de compra."
                },
                Resultado = "Se crea la orden de compra con los precios cotizados y queda emitida al proveedor.",
                Problema = "No se puede generar la orden si falta el precio de algun item."
            });
            _pn2.Add(new Paso06AV
            {
                Codigo = "CU10", Titulo = "Recibir mercaderia",
                Rol = "Compras / Deposito", Donde = "Compra > Compras", ClaveModulo = "pcf_menu_compras",
                Pasos = new[]
                {
                    "Cuando llega la mercaderia, abri la orden de compra correspondiente.",
                    "Verifica que lo recibido coincida con lo pedido y registra la recepcion."
                },
                Resultado = "El stock de cada componente aumenta con las cantidades recibidas y la orden pasa a recibida.",
                Problema = "Si la mercaderia no coincide, no confirmes la recepcion y reclama al proveedor."
            });
            _pn2.Add(new Paso06AV
            {
                Codigo = "CU11", Titulo = "Registrar factura de compra",
                Rol = "Compras / Administracion", Donde = "Compra > Compras", ClaveModulo = "pcf_menu_compras",
                Pasos = new[]
                {
                    "Abri la orden de compra ya recibida.",
                    "Carga los datos de la factura del proveedor (numero, fecha e importe).",
                    "El sistema compara el importe con el total de la orden; si no coincide, avisa la diferencia.",
                    "Confirma."
                },
                Resultado = "La factura queda asociada a la orden y a la compra.",
                Problema = "Ante una diferencia de importe, revisa los precios cotizados en CU09 antes de forzar el registro."
            });
            _pn2.Add(new Paso06AV
            {
                Codigo = "CU12", Titulo = "Cerrar la compra",
                Rol = "Compras / Administracion", Donde = "Compra > Compras", ClaveModulo = "pcf_menu_compras",
                Pasos = new[]
                {
                    "Busca la compra con mercaderia recibida y factura registrada.",
                    "Verifica que todo este conciliado y elegi Cerrar compra."
                },
                Resultado = "La compra pasa a cerrada y ya no admite cambios. El stock repuesto queda disponible para nuevas ventas en PN1.",
                Problema = "El sistema no deja cerrar si falta la recepcion o la factura."
            });
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
