using System;

namespace BE
{
    // Un componente con stock libre y sus datos de rotación (sp_Reporte_StockParado).
    public class FilaStockParado06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public TipoComponente06AV Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Stock { get; set; }
        public int StockReservado { get; set; }
        public DateTime? FechaAlta { get; set; }
        public DateTime? UltimoIngreso { get; set; }
        public DateTime? UltimaVenta { get; set; }
        public int VendidasVentana { get; set; }
        public int VendidasTotal { get; set; }
        public decimal? CostoUltimaCompra { get; set; }

        // Calculados por la BLL.
        public int DiasSinRotar { get; set; }
        public decimal? CoberturaMeses { get; set; }
        public decimal CostoUnitario { get; set; }
        public bool CostoEstimado { get; set; }
        public bool BajaRotacion { get; set; }

        public int StockLibre => Stock - StockReservado;
        public decimal CostoInmovilizado => StockLibre * CostoUnitario;
        public string Nombre => string.IsNullOrWhiteSpace(Marca) && string.IsNullOrWhiteSpace(Modelo)
            ? Descripcion
            : $"{Descripcion} - {Marca} {Modelo}".Trim();
    }
}
