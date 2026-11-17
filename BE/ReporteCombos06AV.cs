using System;
using System.Collections.Generic;

namespace BE
{
    public class ParametrosReporteCombos06AV
    {
        public int UmbralDias { get; set; } = 60;
        public int DiasVentana { get; set; } = 90;
        public decimal CoberturaMaximaMeses { get; set; } = 6m;
        public decimal MargenMinimo { get; set; } = 10m;
        public decimal DescuentoMaximo { get; set; } = 25m;
        public int MaximoCombos { get; set; } = 12;
        // Sin costo de compra registrado se estima el costo como este % del precio de venta.
        public decimal CostoEstimadoPorcentaje { get; set; } = 70m;
    }

    public class ReporteCombos06AV
    {
        public ParametrosReporteCombos06AV Parametros { get; set; }
        public List<FilaStockParado06AV> Parados { get; set; } = new List<FilaStockParado06AV>();
        public List<ComboSugerido06AV> Combos { get; set; } = new List<ComboSugerido06AV>();
        public decimal CapitalInmovilizado { get; set; }
        public DateTime GeneradoEl { get; set; }
        public string GeneradoPor { get; set; }
    }
}
