using BE;
using BLL.Armado;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Reporte inteligente de combos y ofertas por stock inmovilizado.
    ///
    /// 1. Rotación: por componente, días sin rotar (desde la última venta no anulada o,
    ///    si nunca se vendió, desde el alta) y meses de cobertura (stock libre / consumo
    ///    mensual de la ventana). Sin ventas se cuenta desde la última recepción de mercadería. Es "stock parado" si supera el umbral de días o la
    ///    cobertura máxima.
    /// 2. Combo: cada componente parado (ancla, de mayor a menor capital inmovilizado)
    ///    se completa con piezas del tipo que lo vuelve vendible (plantilla por tipo),
    ///    que tengan stock libre y que pasen las reglas de compatibilidad del armado.
    ///    Se prefieren complementos también parados y los que ya se vendieron con el ancla.
    /// 3. Precio: descuento escalonado por antigüedad, tope configurable y nunca por
    ///    debajo del margen mínimo sobre el costo (última cotización aprobada o estimado).
    /// 4. Probabilidad de venta: puntaje explicable (demanda del tipo, descuento,
    ///    historial de venta conjunta, completitud y antigüedad).
    /// 5. Prioridad de publicación: ranking por capital inmovilizado × probabilidad.
    /// </summary>
    public class ReporteCombosBLL06AV
    {
        private readonly ReportesMPP06AV _mpp = new ReportesMPP06AV();

        private static readonly Dictionary<TipoComponente06AV, TipoComponente06AV[]> Plantillas =
            new Dictionary<TipoComponente06AV, TipoComponente06AV[]>
            {
                { TipoComponente06AV.Procesador,    new[] { TipoComponente06AV.PlacaMadre, TipoComponente06AV.MemoriaRAM } },
                { TipoComponente06AV.PlacaMadre,    new[] { TipoComponente06AV.Procesador, TipoComponente06AV.MemoriaRAM } },
                { TipoComponente06AV.MemoriaRAM,    new[] { TipoComponente06AV.PlacaMadre } },
                { TipoComponente06AV.PlacaDeVideo,  new[] { TipoComponente06AV.Fuente } },
                { TipoComponente06AV.Fuente,        new[] { TipoComponente06AV.Gabinete } },
                { TipoComponente06AV.Gabinete,      new[] { TipoComponente06AV.Fuente, TipoComponente06AV.Refrigeracion } },
                { TipoComponente06AV.Disco,         new[] { TipoComponente06AV.MemoriaRAM } },
                { TipoComponente06AV.Refrigeracion, new[] { TipoComponente06AV.Procesador } },
                { TipoComponente06AV.Otro,          new TipoComponente06AV[0] }
            };

        public ReporteCombos06AV Generar(ParametrosReporteCombos06AV p)
        {
            ExigirPermiso();
            Validar(p);

            List<FilaStockParado06AV> todas;
            Dictionary<string, int> pares;
            try
            {
                todas = _mpp.ObtenerStockParado(p.DiasVentana) ?? new List<FilaStockParado06AV>();
                pares = _mpp.ObtenerComponentesVendidosJuntos() ?? new Dictionary<string, int>();
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo generar el reporte de combos por stock parado.", ex); }

            new CompatibilidadBLL06AV().Recargar();
            Dictionary<string, Componente06AV> componentes = new ComponentesBLL06AV().ObtenerTodos()
                .GroupBy(c => c.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            DateTime hoy = DateTime.Today;
            foreach (FilaStockParado06AV f in todas)
                CalcularRotacion(f, p, hoy);

            List<FilaStockParado06AV> conStock = todas.Where(f => f.StockLibre > 0).ToList();
            List<FilaStockParado06AV> parados = conStock.Where(f => f.BajaRotacion)
                .OrderByDescending(f => f.CostoInmovilizado).ThenByDescending(f => f.DiasSinRotar).ToList();

            Dictionary<TipoComponente06AV, decimal> demanda = DemandaPorTipo(todas);
            List<ComboSugerido06AV> combos = ArmarCombos(parados, conStock, componentes, pares, demanda, p);
            AsignarPrioridades(combos);

            return new ReporteCombos06AV
            {
                Parametros = p,
                Parados = parados,
                Combos = combos,
                CapitalInmovilizado = parados.Sum(f => f.CostoInmovilizado),
                GeneradoEl = DateTime.Now,
                GeneradoPor = UsuarioActual()
            };
        }

        public static void Validar(ParametrosReporteCombos06AV p)
        {
            if (p == null) throw new ValidacionException06AV("parametros", "Faltan los parámetros del reporte.");
            if (p.UmbralDias < 1 || p.UmbralDias > 3650)
                throw new ValidacionException06AV("umbral", "El umbral de días sin rotar debe estar entre 1 y 3650.");
            if (p.DiasVentana < 7 || p.DiasVentana > 730)
                throw new ValidacionException06AV("ventana", "La ventana de ventas debe estar entre 7 y 730 días.");
            if (p.DescuentoMaximo < 0 || p.DescuentoMaximo > 90)
                throw new ValidacionException06AV("descuento", "El descuento máximo debe estar entre 0 y 90 %.");
            if (p.MargenMinimo < 0 || p.MargenMinimo > 90)
                throw new ValidacionException06AV("margen", "El margen mínimo debe estar entre 0 y 90 %.");
            if (p.MaximoCombos < 1 || p.MaximoCombos > 100)
                throw new ValidacionException06AV("combos", "La cantidad de combos debe estar entre 1 y 100.");
        }

        private static void CalcularRotacion(FilaStockParado06AV f, ParametrosReporteCombos06AV p, DateTime hoy)
        {
            // Componentes_C guarda solo la versión vigente y la anterior: su fecha más vieja no
            // es el alta real, por eso se usa como último recurso.
            DateTime? referencia = f.UltimaVenta ?? f.UltimoIngreso ?? f.FechaAlta;
            f.DiasSinRotar = referencia.HasValue ? Math.Max(0, (hoy - referencia.Value.Date).Days) : 0;

            decimal consumoMensual = f.VendidasVentana / (p.DiasVentana / 30m);
            f.CoberturaMeses = consumoMensual > 0 ? decimal.Round(f.StockLibre / consumoMensual, 1) : (decimal?)null;

            f.BajaRotacion = f.StockLibre > 0 &&
                             (f.DiasSinRotar >= p.UmbralDias ||
                              (f.CoberturaMeses.HasValue && f.CoberturaMeses.Value >= p.CoberturaMaximaMeses));

            f.CostoEstimado = !f.CostoUltimaCompra.HasValue;
            f.CostoUnitario = f.CostoUltimaCompra ?? decimal.Round(f.PrecioUnitario * p.CostoEstimadoPorcentaje / 100m, 2);
        }

        // Unidades vendidas del tipo en la ventana / stock libre del tipo, acotado a 1.
        private static Dictionary<TipoComponente06AV, decimal> DemandaPorTipo(List<FilaStockParado06AV> todas)
        {
            return todas.GroupBy(f => f.Tipo).ToDictionary(
                g => g.Key,
                g => Math.Min(1m, g.Sum(f => f.VendidasVentana) / (decimal)Math.Max(1, g.Sum(f => Math.Max(0, f.StockLibre)))));
        }

        private static List<ComboSugerido06AV> ArmarCombos(
            List<FilaStockParado06AV> parados, List<FilaStockParado06AV> conStock,
            Dictionary<string, Componente06AV> componentes, Dictionary<string, int> pares,
            Dictionary<TipoComponente06AV, decimal> demanda, ParametrosReporteCombos06AV p)
        {
            var combos = new List<ComboSugerido06AV>();
            var restante = conStock.ToDictionary(f => f.Codigo, f => f.StockLibre, StringComparer.OrdinalIgnoreCase);
            var usadosComoParados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FilaStockParado06AV ancla in parados)
            {
                if (combos.Count >= p.MaximoCombos) break;
                if (usadosComoParados.Contains(ancla.Codigo) || restante[ancla.Codigo] <= 0) continue;

                var items = new List<FilaStockParado06AV> { ancla };
                var piezas = new List<Componente06AV>();
                AgregarPieza(piezas, componentes, ancla);

                TipoComponente06AV[] plantilla = Plantillas.TryGetValue(ancla.Tipo, out var t) ? t : new TipoComponente06AV[0];
                foreach (TipoComponente06AV tipo in plantilla)
                {
                    FilaStockParado06AV elegido = conStock
                        .Where(f => f.Tipo == tipo && restante[f.Codigo] > 0 && !items.Contains(f))
                        .Where(f => Compatible(piezas, componentes, f))
                        .OrderByDescending(f => f.BajaRotacion && !usadosComoParados.Contains(f.Codigo))
                        .ThenByDescending(f => VecesJuntos(pares, ancla.Codigo, f.Codigo))
                        .ThenBy(f => Math.Abs(f.PrecioUnitario - ancla.PrecioUnitario))
                        .FirstOrDefault();
                    if (elegido == null) continue;
                    items.Add(elegido);
                    AgregarPieza(piezas, componentes, elegido);
                }

                int unidades = Math.Max(1, items.Min(i => restante[i.Codigo]));
                foreach (FilaStockParado06AV i in items)
                {
                    restante[i.Codigo] = Math.Max(0, restante[i.Codigo] - unidades);
                    if (i.BajaRotacion) usadosComoParados.Add(i.Codigo);
                }

                var combo = new ComboSugerido06AV
                {
                    Numero = combos.Count + 1,
                    Ancla = ancla,
                    Items = items,
                    Unidades = unidades,
                    Publico = Publico(piezas),
                    VendidosJuntosAntes = items.Any(a => items.Any(b => a != b && VecesJuntos(pares, a.Codigo, b.Codigo) > 0))
                };
                combo.PorcentajeDescuento = Descuento(combo, p);
                combo.Probabilidad = Probabilidad(combo, plantilla.Length, demanda, p);
                combos.Add(combo);
            }
            return combos;
        }

        private static void AgregarPieza(List<Componente06AV> piezas, Dictionary<string, Componente06AV> componentes, FilaStockParado06AV f)
        {
            if (componentes.TryGetValue(f.Codigo, out Componente06AV c)) piezas.Add(c);
        }

        private static bool Compatible(List<Componente06AV> piezas, Dictionary<string, Componente06AV> componentes, FilaStockParado06AV candidato)
        {
            if (!componentes.TryGetValue(candidato.Codigo, out Componente06AV c)) return false;
            var prueba = new List<Componente06AV>(piezas) { c };
            return !ReglasCompatibilidad06AV.EvaluarEquipo(prueba, false).EsIncompatible;
        }

        private static int VecesJuntos(Dictionary<string, int> pares, string a, string b)
        {
            // El SP ordena el par con la intercalación de la base: se prueban los dos órdenes.
            if (pares.TryGetValue(a + "|" + b, out int n)) return n;
            return pares.TryGetValue(b + "|" + a, out n) ? n : 0;
        }

        // Escalonado por antigüedad del ítem más parado, +5 % si liquida dos o más piezas paradas.
        // Nunca deja el combo por debajo del margen mínimo sobre el costo.
        private static decimal Descuento(ComboSugerido06AV combo, ParametrosReporteCombos06AV p)
        {
            int dias = combo.Items.Where(i => i.BajaRotacion).Select(i => i.DiasSinRotar).DefaultIfEmpty(0).Max();
            decimal d = dias >= 180 ? 20m : dias >= 90 ? 15m : 10m;
            if (combo.Items.Count(i => i.BajaRotacion) >= 2) d += 5m;
            d = Math.Min(d, p.DescuentoMaximo);

            decimal lista = combo.PrecioLista;
            if (lista > 0)
            {
                decimal tope = (1m - combo.CostoCombo / (lista * (1m - p.MargenMinimo / 100m))) * 100m;
                d = Math.Min(d, Math.Floor(tope));
            }
            return Math.Max(0m, d);
        }

        private static decimal Probabilidad(ComboSugerido06AV combo, int largoPlantilla,
                                            Dictionary<TipoComponente06AV, decimal> demanda, ParametrosReporteCombos06AV p)
        {
            decimal demandaTipo = demanda.TryGetValue(combo.Ancla.Tipo, out decimal dt) ? dt : 0m;
            decimal descuento = p.DescuentoMaximo > 0 ? combo.PorcentajeDescuento / p.DescuentoMaximo : 0m;
            decimal completitud = combo.Items.Count / (decimal)(largoPlantilla + 1);
            decimal antiguedad = Math.Min(1m, combo.Ancla.DiasSinRotar / 365m);

            decimal prob = 0.20m
                           + 0.25m * demandaTipo
                           + 0.25m * descuento
                           + (combo.VendidosJuntosAntes ? 0.15m : 0m)
                           + 0.10m * completitud
                           - 0.10m * antiguedad;
            return decimal.Round(Math.Max(0.05m, Math.Min(0.95m, prob)), 2);
        }

        private static PublicoObjetivo06AV Publico(List<Componente06AV> piezas)
        {
            bool gamer = piezas.Any(c =>
                c.Tipo == TipoComponente06AV.PlacaDeVideo ||
                (c.Tipo == TipoComponente06AV.Procesador && c.Especificacion?.Desbloqueado == true) ||
                (c.Tipo == TipoComponente06AV.Fuente && (c.Especificacion?.PotenciaW ?? 0) >= 650) ||
                (c.Tipo == TipoComponente06AV.Refrigeracion && (c.Especificacion?.TdpSoportadoW ?? 0) >= 200));
            if (gamer) return PublicoObjetivo06AV.Gamer;

            bool diseno = piezas.Any(c =>
                (c.Tipo == TipoComponente06AV.MemoriaRAM && (c.Especificacion?.CapacidadGb ?? 0) >= 32) ||
                (c.Tipo == TipoComponente06AV.Procesador && (c.Especificacion?.ConsumoW ?? 0) >= 105));
            return diseno ? PublicoObjetivo06AV.Diseno : PublicoObjetivo06AV.Oficina;
        }

        private static void AsignarPrioridades(List<ComboSugerido06AV> combos)
        {
            var orden = combos.OrderByDescending(c => c.CostoInmovilizado * c.Probabilidad).ToList();
            int n = orden.Count;
            int corteAlta = (int)Math.Ceiling(n / 3m), corteMedia = (int)Math.Ceiling(2 * n / 3m);
            for (int i = 0; i < n; i++)
                orden[i].Prioridad = i < corteAlta ? PrioridadPublicacion06AV.Alta
                                   : i < corteMedia ? PrioridadPublicacion06AV.Media
                                   : PrioridadPublicacion06AV.Baja;
        }

        private static void ExigirPermiso()
        {
            if (!UsuarioSesion06AV.Instancia().TienePermiso(PatenteEnum06AV.VerReporteVentas))
                throw new ValidacionException06AV("permiso",
                    "No tenés permiso para ver el reporte de combos por stock parado (patente VerReporteVentas).");
        }

        private static string UsuarioActual()
        {
            try
            {
                var sesion = UsuarioSesion06AV.Instancia();
                string nombre = sesion.NombreCompleto();
                return string.IsNullOrWhiteSpace(nombre) ? sesion.UsuarioActual?.Dni : nombre;
            }
            catch { return null; }
        }
    }
}
