using BE;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Detecta situaciones que no nacen de una acción puntual (atrasos, stock bajo) y las
    /// publica como notificación una sola vez, gracias a la clave única de cada una.
    /// </summary>
    public class AvisosBLL06AV
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);
        private static DateTime _ultimaDeteccion = DateTime.MinValue;

        private readonly ComprasMPP06AV _compras = new ComprasMPP06AV();
        private readonly ComponentesMPP06AV _componentes = new ComponentesMPP06AV();
        private readonly VentasMPP06AV _ventas = new VentasMPP06AV();
        private readonly ProduccionMPP06AV _produccion = new ProduccionMPP06AV();

        public void Detectar(bool forzar = false)
        {
            if (!forzar && DateTime.Now - _ultimaDeteccion < Intervalo) return;
            _ultimaDeteccion = DateTime.Now;

            try
            {
                if (UsuarioSesion06AV.Instancia()?.UsuarioActual == null) return;
            }
            catch { return; }

            List<OrdenCompra06AV> ordenes = Leer(() => _compras.ObtenerOrdenesCompra());
            StockBajo(ordenes);
            ComprasAtrasadas(ordenes);
            ProduccionAtrasada(Leer(() => _produccion.ObtenerTodas()));
            EntregasVencidas(Leer(() => _ventas.ObtenerTodas()));
        }

        private static List<T> Leer<T>(Func<List<T>> lectura)
        {
            try { return lectura() ?? new List<T>(); }
            catch { return new List<T>(); }
        }

        private void StockBajo(List<OrdenCompra06AV> ordenes)
        {
            List<Componente06AV> bajos = Leer(() => _componentes.ObtenerBajoStock());
            var enCurso = new HashSet<string>(
                ordenes.Where(o => o.EnCurso).SelectMany(o => o.ComponentesFaltantes)
                       .Where(d => d.Componente != null).Select(d => d.Componente.Codigo),
                StringComparer.OrdinalIgnoreCase);

            // Una vez por semana por componente mientras siga bajo y sin pedir.
            var cal = CultureInfo.InvariantCulture.Calendar;
            string semana = DateTime.Today.Year + "-" + cal.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            foreach (var c in bajos.Where(c => !c.BajaLogica && !enCurso.Contains(c.Codigo)))
                NotificacionesBLL06AV.PublicarUnica($"stock:{c.Codigo}:{semana}", TipoNotificacion06AV.StockBajo,
                    PatenteEnum06AV.RegistrarOrdenCompra, NotificacionesBLL06AV.DestinoCompras,
                    c.Descripcion, c.Stock, c.StockMinimo);
        }

        private static void ComprasAtrasadas(List<OrdenCompra06AV> ordenes)
        {
            foreach (var o in ordenes.Where(o => o.EnCurso && o.FechaLimite.Date < DateTime.Today))
                NotificacionesBLL06AV.PublicarUnica($"oc-atrasada:{o.Id}", TipoNotificacion06AV.OrdenCompraAtrasada,
                    PatenteEnum06AV.GestionarCompras, NotificacionesBLL06AV.DestinoCompras,
                    o.NumeroCompra, o.FechaLimite.ToString("dd/MM"));
        }

        private static readonly EstadoOrdenProduccion06AV[] OrdenesAbiertas =
        {
            EstadoOrdenProduccion06AV.Pendiente, EstadoOrdenProduccion06AV.Planificada,
            EstadoOrdenProduccion06AV.EnEnsamblaje, EstadoOrdenProduccion06AV.EnRevision
        };

        private static void ProduccionAtrasada(List<OrdenProduccion06AV> ordenes)
        {
            foreach (var o in ordenes.Where(o => OrdenesAbiertas.Contains(o.Estado) && o.FechaEntregaEstimada.Date < DateTime.Today))
                NotificacionesBLL06AV.PublicarUnica($"op-atrasada:{o.NumeroOrden}", TipoNotificacion06AV.ProduccionAtrasada,
                    PatenteEnum06AV.GestionarProduccion, NotificacionesBLL06AV.DestinoProduccion,
                    o.NumeroOrden, o.Cliente?.NombreCompleto ?? "—", o.FechaEntregaEstimada.ToString("dd/MM"));
        }

        // RFN1: la venta sigue comprometida con el cliente hasta la entrega (CU07).
        private static void EntregasVencidas(List<Venta06AV> ventas)
        {
            foreach (var v in ventas.Where(v => v.Estado != EstadoVenta06AV.Entregada && v.Estado != EstadoVenta06AV.Anulada &&
                                                v.FechaEntregaEstimada.Date < DateTime.Today))
                NotificacionesBLL06AV.PublicarUnica($"venta-vencida:{v.NumeroVenta}", TipoNotificacion06AV.EntregaVencida,
                    PatenteEnum06AV.GestionarVentas, NotificacionesBLL06AV.DestinoVentas,
                    v.NumeroVenta, v.Cliente?.NombreCompleto ?? "—", v.FechaEntregaEstimada.ToString("dd/MM"));
        }
    }
}
