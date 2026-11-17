using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public enum PublicoObjetivo06AV
    {
        Oficina,
        Gamer,
        Diseno
    }

    public enum PrioridadPublicacion06AV
    {
        Alta,
        Media,
        Baja
    }

    // Combo propuesto por el reporte inteligente: un componente parado (ancla)
    // más los complementos compatibles que lo hacen vendible.
    public class ComboSugerido06AV
    {
        public int Numero { get; set; }
        public FilaStockParado06AV Ancla { get; set; }
        public List<FilaStockParado06AV> Items { get; set; } = new List<FilaStockParado06AV>();
        public int Unidades { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal Probabilidad { get; set; }
        public PublicoObjetivo06AV Publico { get; set; }
        public PrioridadPublicacion06AV Prioridad { get; set; }
        public bool VendidosJuntosAntes { get; set; }
        public string Motivo { get; set; }

        public decimal PrecioLista => Items.Sum(i => i.PrecioUnitario);
        public decimal PrecioCombo => decimal.Round(PrecioLista * (1m - PorcentajeDescuento / 100m), 2);
        public decimal CostoCombo => Items.Sum(i => i.CostoUnitario);
        public bool CostoEstimado => Items.Any(i => i.CostoEstimado);
        public decimal MargenPorcentaje => PrecioCombo <= 0 ? 0 : decimal.Round((PrecioCombo - CostoCombo) * 100m / PrecioCombo, 1);
        public decimal CostoInmovilizado => Items.Where(i => i.BajaRotacion).Sum(i => i.CostoUnitario) * Unidades;
        public decimal IngresoPotencial => decimal.Round(PrecioCombo * Unidades * Probabilidad, 2);
        public string Nombre => Ancla?.Descripcion ?? "";
        public string Codigo => "CMB-" + Numero.ToString("000");
    }
}
