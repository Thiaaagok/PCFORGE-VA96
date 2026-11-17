using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class ReportesMPP06AV
    {
        private readonly ReportesDAL06AV _dal = new ReportesDAL06AV();

        public List<FilaReporteVentas06AV> ObtenerVentasProduccion(FiltroReporteVentas06AV filtro)
        {
            DataTable t = _dal.ObtenerVentasProduccion(
                filtro.Desde, filtro.Hasta,
                filtro.Estado.HasValue ? (int)filtro.Estado.Value : (int?)null,
                filtro.Tipo.HasValue ? (int)filtro.Tipo.Value : (int?)null,
                filtro.Cliente);

            var lista = new List<FilaReporteVentas06AV>();
            foreach (DataRow r in t.Rows)
                lista.Add(MapearFila(r));
            return lista;
        }

        private static FilaReporteVentas06AV MapearFila(DataRow r) => new FilaReporteVentas06AV
        {
            NumeroVenta = Convert.ToInt32(r["NumeroVenta"]),
            FechaVenta = Convert.ToDateTime(r["FechaVenta"]),
            ClienteDni = Texto(r, "Dni"),
            ClienteNombre = Texto(r, "Cliente"),
            Equipo = Texto(r, "Equipo"),
            TipoConfiguracion = (TipoConfiguracion06AV)Convert.ToInt32(r["TipoConfiguracion"]),
            ModeloOrigen = Nulo(r, "ModeloOrigen") ? null : r["ModeloOrigen"].ToString(),
            Total = Decimal(r, "Total"),
            Sena = Decimal(r, "Sena"),
            SaldoCobrado = Decimal(r, "SaldoCobrado"),
            EstadoVenta = (EstadoVenta06AV)Convert.ToInt32(r["EstadoVenta"]),
            UsuarioRegistro = Texto(r, "UsuarioRegistro"),
            NumeroOrden = Nulo(r, "NumeroOrden") ? (int?)null : Convert.ToInt32(r["NumeroOrden"]),
            EstadoOrden = Nulo(r, "EstadoOrden") ? (EstadoOrdenProduccion06AV?)null
                                                 : (EstadoOrdenProduccion06AV)Convert.ToInt32(r["EstadoOrden"]),
            Linea = Nulo(r, "Linea") ? null : r["Linea"].ToString(),
            NumeroSerie = Nulo(r, "NumeroSerie") ? null : r["NumeroSerie"].ToString(),
            FechaEntregaComprometida = Convert.ToDateTime(r["FechaEntregaComprometida"]),
            FechaCierre = Nulo(r, "FechaCierre") ? (DateTime?)null : Convert.ToDateTime(r["FechaCierre"]),
            FechaEntrega = Nulo(r, "FechaEntrega") ? (DateTime?)null : Convert.ToDateTime(r["FechaEntrega"])
        };

        public List<FilaStockParado06AV> ObtenerStockParado(int diasVentana)
        {
            DataTable t = _dal.ObtenerStockParado(diasVentana);
            var lista = new List<FilaStockParado06AV>();
            foreach (DataRow r in t.Rows)
            {
                lista.Add(new FilaStockParado06AV
                {
                    Codigo = Texto(r, "Codigo"),
                    Descripcion = Texto(r, "Descripcion"),
                    Tipo = (TipoComponente06AV)Convert.ToInt32(r["Tipo"]),
                    Marca = Texto(r, "Marca"),
                    Modelo = Texto(r, "Modelo"),
                    PrecioUnitario = Decimal(r, "PrecioUnitario"),
                    Stock = Convert.ToInt32(r["Stock"]),
                    StockReservado = Convert.ToInt32(r["StockReservado"]),
                    FechaAlta = Fecha(r, "FechaAlta"),
                    UltimoIngreso = Fecha(r, "UltimoIngreso"),
                    UltimaVenta = Fecha(r, "UltimaVenta"),
                    VendidasVentana = Convert.ToInt32(r["VendidasVentana"]),
                    VendidasTotal = Convert.ToInt32(r["VendidasTotal"]),
                    CostoUltimaCompra = Nulo(r, "CostoUltimaCompra") ? (decimal?)null : Convert.ToDecimal(r["CostoUltimaCompra"])
                });
            }
            return lista;
        }

        // Clave "codigoA|codigoB" (A < B) → cantidad de computadoras vendidas con ambos.
        public Dictionary<string, int> ObtenerComponentesVendidosJuntos()
        {
            DataTable t = _dal.ObtenerComponentesVendidosJuntos();
            var pares = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in t.Rows)
                pares[Texto(r, "CodigoA") + "|" + Texto(r, "CodigoB")] = Convert.ToInt32(r["Veces"]);
            return pares;
        }

        private static DateTime? Fecha(DataRow r, string col) => Nulo(r, col) ? (DateTime?)null : Convert.ToDateTime(r[col]);

        private static bool Nulo(DataRow r, string col) =>
            !r.Table.Columns.Contains(col) || r[col] == DBNull.Value;

        private static string Texto(DataRow r, string col) => Nulo(r, col) ? "" : r[col].ToString();

        private static decimal Decimal(DataRow r, string col) => Nulo(r, col) ? 0m : Convert.ToDecimal(r[col]);
    }
}
