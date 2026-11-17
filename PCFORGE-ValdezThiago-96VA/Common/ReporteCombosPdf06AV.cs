using BE;
using PCFORGE_ValdezThiago_96VA.Controles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class ReporteCombosPdf06AV : DocumentoPdf06AV
    {
        // Prioridad | Combo y piezas | Público | Precio | Desc./Margen | Prob. | Ingreso
        private static readonly float[] AnchosCombo = { 52f, 196f, 52f, 70f, 52f, 36f, 57f };
        // Código | Componente | Stock | Días | Últ. venta | Cobertura | Inmovilizado
        private static readonly float[] AnchosParado = { 60f, 175f, 40f, 40f, 62f, 58f, 80f };

        private readonly ReporteCombos06AV _r;
        private readonly IList<ComboSugerido06AV> _combos;

        private ReporteCombosPdf06AV(ReporteCombos06AV reporte, IList<ComboSugerido06AV> combos)
        {
            _r = reporte ?? throw new ArgumentNullException(nameof(reporte));
            _combos = combos ?? reporte.Combos;
        }

        public static void Guardar(string ruta, ReporteCombos06AV reporte, IList<ComboSugerido06AV> combos, Image logo)
        {
            new ReporteCombosPdf06AV(reporte, combos).Generar(ruta, logo);
        }

        protected override string Titulo => _t.Obtener("rcmb_doc_titulo");

        protected override string Numero => string.Empty;

        protected override string NumeroAlternativo =>
            _t.Obtener("rcmb_doc_parametros", _r.Parametros.UmbralDias, _r.Parametros.DiasVentana, _r.Parametros.MargenMinimo);

        protected override string ClavePie => "rpdf_generado_el";

        private void Generar(string ruta, Image logo)
        {
            _pdf.DefinirImagen(logo);
            NuevaPagina();
            float y = EncabezadoCompleto() + 20f;
            y = Indicadores(y);

            y = Seccion(_t.Obtener("rcmb_seccion_combos"), y + 24f);
            y = TablaCombos(y);

            if (y + 90f > LimiteContenido) y = PaginaContinuacion(); else y += 20f;
            y = Seccion(_t.Obtener("rcmb_seccion_parados"), y);
            y = TablaParados(y);

            y = Metodologia(y + 18f);
            if (!string.IsNullOrWhiteSpace(_r.GeneradoPor))
            {
                if (y + 30f > LimiteContenido) y = PaginaContinuacion();
                _pdf.Texto(_t.Obtener("rcmb_doc_pie", _r.GeneradoPor, _combos.Count, _r.Parados.Count), M, y + 18f, 8f,
                           FuentePdf06AV.Cursiva, Gris);
            }
            Finalizar(ruta);
        }

        private float Seccion(string titulo, float y)
        {
            _pdf.Rect(M, y, 3f, 14f, Naranja);
            _pdf.Texto(titulo.ToUpper(), M + 10f, y + 11f, 10f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Izquierda, 0.6f);
            return y + 22f;
        }

        private float Indicadores(float y)
        {
            var parados = _r.Parados;
            var tarjetas = new[]
            {
                new[] { _t.Obtener("rcmb_kpi_parados"), parados.Count.ToString(), _t.Obtener("rcmb_kpi_unidades", parados.Sum(f => f.StockLibre)) },
                new[] { _t.Obtener("rcmb_kpi_inmovilizado"), Plata(_r.CapitalInmovilizado),
                        _t.Obtener("rcmb_kpi_dias_prom", parados.Count == 0 ? 0 : (int)parados.Average(f => f.DiasSinRotar)) },
                new[] { _t.Obtener("rcmb_kpi_combos"), _combos.Count.ToString(),
                        _t.Obtener("rcmb_kpi_alta", _combos.Count(c => c.Prioridad == PrioridadPublicacion06AV.Alta)) },
                new[] { _t.Obtener("rcmb_kpi_ingreso"), Plata(_combos.Sum(c => c.IngresoPotencial)), _t.Obtener("rcmb_kpi_ingreso_pie") }
            };

            const float gap = 10f;
            float ancho = (AnchoUtil - gap * 3) / 4f;
            const float alto = 62f;
            for (int i = 0; i < tarjetas.Length; i++)
            {
                float x = M + i * (ancho + gap);
                _pdf.RectRedondeado(x, y, ancho, alto, 6f, Fondo, Borde);
                _pdf.Rect(x, y + 10f, 3f, alto - 20f, i == 1 ? Naranja : Cian);
                Etiqueta(tarjetas[i][0], x + 12f, y + 17f);
                float tam = _pdf.Medir(tarjetas[i][1], 14f, FuentePdf06AV.Negrita) > ancho - 22f ? 10f : 14f;
                _pdf.Texto(_pdf.Recortar(tarjetas[i][1], ancho - 22f, tam, FuentePdf06AV.Negrita), x + 12f, y + 38f, tam,
                           FuentePdf06AV.Negrita, Tinta);
                _pdf.Texto(_pdf.Recortar(tarjetas[i][2], ancho - 22f, 7.5f, FuentePdf06AV.Normal), x + 12f, y + 53f, 7.5f,
                           FuentePdf06AV.Normal, Gris);
            }
            return y + alto;
        }

        private float Encabezado(float y, float[] anchos, string[] titulos, int primeraDerecha)
        {
            _pdf.RectRedondeado(M, y, AnchoUtil, 24f, 4f, Grafito);
            _pdf.Rect(M, y + 12f, AnchoUtil, 12f, Grafito);
            float x = M;
            for (int i = 0; i < titulos.Length; i++)
            {
                bool derecha = i >= primeraDerecha;
                float tx = derecha ? x + anchos[i] - 8f : x + 8f;
                _pdf.Texto(_pdf.Recortar(titulos[i], anchos[i] - 10f, 7f, FuentePdf06AV.Negrita), tx, y + 15.5f, 7f,
                           FuentePdf06AV.Negrita, Blanco, derecha ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda, 0.5f);
                x += anchos[i];
            }
            return y + 24f;
        }

        private float EncabezadoCombos(float y) => Encabezado(y, AnchosCombo, new[]
        {
            _t.Obtener("rcmb_col_prioridad"), _t.Obtener("rcmb_col_combo"), _t.Obtener("rcmb_col_publico"),
            _t.Obtener("rcmb_col_precio"), _t.Obtener("rcmb_doc_desc_margen"), _t.Obtener("rcmb_doc_prob"),
            _t.Obtener("rcmb_col_ingreso")
        }, 3);

        private float TablaCombos(float y)
        {
            y = EncabezadoCombos(y);
            if (_combos.Count == 0) return SinDatos(y);

            for (int i = 0; i < _combos.Count; i++)
            {
                ComboSugerido06AV c = _combos[i];
                float alto = 26f + 10.5f * c.Items.Count;
                if (y + alto > LimiteContenido) y = EncabezadoCombos(PaginaContinuacion());
                if (i % 2 == 1) _pdf.Rect(M, y, AnchoUtil, alto, Fondo);
                _pdf.Linea(M, y + alto, Derecha, y + alto, Borde, 0.6f);

                float x = M;
                Color fondo = c.Prioridad == PrioridadPublicacion06AV.Alta ? RojoSuave
                            : c.Prioridad == PrioridadPublicacion06AV.Media ? NaranjaSuave : CianSuave;
                Color tinta = c.Prioridad == PrioridadPublicacion06AV.Alta ? Rojo
                            : c.Prioridad == PrioridadPublicacion06AV.Media ? NaranjaTexto : CianTexto;
                Pastilla(_t.Obtener("rcmb_prioridad_" + c.Prioridad.ToString().ToLowerInvariant()).ToUpper(), x + 6f, y + 7f, fondo, tinta);
                _pdf.Texto(c.Codigo, x + 8f, y + 34f, 7f, FuentePdf06AV.Negrita, Gris);
                x += AnchosCombo[0];

                float w = AnchosCombo[1] - 12f;
                _pdf.Texto(_pdf.Recortar(c.Nombre, w, 8.5f, FuentePdf06AV.Negrita), x + 6f, y + 14f, 8.5f, FuentePdf06AV.Negrita, Tinta);
                float ly = y + 25f;
                foreach (FilaStockParado06AV it in c.Items)
                {
                    string linea = "+ " + it.Codigo + "  " + it.Nombre;
                    _pdf.Texto(_pdf.Recortar(linea, w - 34f, 7f, FuentePdf06AV.Normal), x + 6f, ly, 7f, FuentePdf06AV.Normal,
                               it.BajaRotacion ? NaranjaTexto : Gris);
                    if (it.BajaRotacion)
                        _pdf.Texto(_t.Obtener("rcmb_doc_dias", it.DiasSinRotar), x + w + 6f, ly, 6.5f, FuentePdf06AV.Negrita,
                                   NaranjaTexto, AlineacionPdf06AV.Derecha);
                    ly += 10.5f;
                }
                x += AnchosCombo[1];

                _pdf.Texto(_t.Obtener("rcmb_publico_" + c.Publico.ToString().ToLowerInvariant()), x + 6f, y + 14f, 8f,
                           FuentePdf06AV.Negrita, Tinta);
                _pdf.Texto(_t.Obtener("rcmb_doc_unidades", c.Unidades), x + 6f, y + 25f, 7f, FuentePdf06AV.Normal, Gris);
                x += AnchosCombo[2];

                float d = x + AnchosCombo[3] - 8f;
                _pdf.Texto(Plata(c.PrecioCombo), d, y + 14f, 8.5f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                _pdf.Texto(Plata(c.PrecioLista), d, y + 25f, 7f, FuentePdf06AV.Normal, GrisClaro, AlineacionPdf06AV.Derecha);
                float tachado = _pdf.Medir(Plata(c.PrecioLista), 7f, FuentePdf06AV.Normal);
                _pdf.Linea(d - tachado, y + 22.5f, d, y + 22.5f, GrisClaro, 0.5f);
                x += AnchosCombo[3];

                d = x + AnchosCombo[4] - 8f;
                _pdf.Texto("-" + c.PorcentajeDescuento.ToString("0") + "%", d, y + 14f, 8.5f, FuentePdf06AV.Negrita, Verde, AlineacionPdf06AV.Derecha);
                _pdf.Texto(c.MargenPorcentaje.ToString("0.#") + "%" + (c.CostoEstimado ? "*" : ""), d, y + 25f, 7f,
                           FuentePdf06AV.Normal, Gris, AlineacionPdf06AV.Derecha);
                x += AnchosCombo[4];

                d = x + AnchosCombo[5] - 8f;
                _pdf.Texto((c.Probabilidad * 100m).ToString("0") + "%", d, y + 14f, 8.5f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                x += AnchosCombo[5];

                d = x + AnchosCombo[6] - 8f;
                _pdf.Texto(Plata(c.IngresoPotencial), d, y + 14f, 8f, FuentePdf06AV.Negrita, CianTexto, AlineacionPdf06AV.Derecha);
                y += alto;
            }

            y = y + 42f > LimiteContenido ? PaginaContinuacion() : y + 6f;
            _pdf.RectRedondeado(Derecha - AnchoResumen, y, AnchoResumen, 30f, 5f, Oscuro);
            _pdf.Rect(Derecha - AnchoResumen, y + 7f, 3.5f, 16f, Naranja);
            _pdf.Texto(_t.Obtener("rcmb_kpi_ingreso"), Derecha - AnchoResumen + 14f, y + 19f, 7.5f, FuentePdf06AV.Negrita, GrisClaro,
                       AlineacionPdf06AV.Izquierda, 0.8f);
            _pdf.Texto(Plata(_combos.Sum(c => c.IngresoPotencial)), Derecha - 12f, y + 20f, 12f, FuentePdf06AV.Negrita, Blanco,
                       AlineacionPdf06AV.Derecha);
            return y + 30f;
        }

        private float EncabezadoParados(float y) => Encabezado(y, AnchosParado, new[]
        {
            _t.Obtener("rcmb_col_codigo"), _t.Obtener("rcmb_col_componente"), _t.Obtener("rcmb_col_stock"),
            _t.Obtener("rcmb_col_dias"), _t.Obtener("rcmb_col_ultima_venta"), _t.Obtener("rcmb_col_cobertura"),
            _t.Obtener("rcmb_col_inmovilizado")
        }, 2);

        private float TablaParados(float y)
        {
            const float alto = 28f;
            y = EncabezadoParados(y);
            if (_r.Parados.Count == 0) return SinDatos(y);

            for (int i = 0; i < _r.Parados.Count; i++)
            {
                if (y + alto > LimiteContenido) y = EncabezadoParados(PaginaContinuacion());
                FilaStockParado06AV f = _r.Parados[i];
                if (i % 2 == 1) _pdf.Rect(M, y, AnchoUtil, alto, Fondo);
                _pdf.Linea(M, y + alto, Derecha, y + alto, Borde, 0.6f);

                float x = M;
                _pdf.Texto(_pdf.Recortar(f.Codigo, AnchosParado[0] - 12f, 8f, FuentePdf06AV.Negrita), x + 8f, y + 17f, 8f,
                           FuentePdf06AV.Negrita, CianTexto);
                x += AnchosParado[0];
                _pdf.Texto(_pdf.Recortar(f.Nombre, AnchosParado[1] - 12f, 8f, FuentePdf06AV.Negrita), x + 6f, y + 12f, 8f,
                           FuentePdf06AV.Negrita, Tinta);
                _pdf.Texto(ReporteCombosControl06AV.NombreTipo(f.Tipo), x + 6f, y + 22f, 7f, FuentePdf06AV.Normal, Gris);
                x += AnchosParado[1];

                CeldaDerecha(f.StockLibre.ToString(), ref x, 0, y, Tinta);
                CeldaDerecha(f.DiasSinRotar.ToString(), ref x, 1, y, f.DiasSinRotar >= 180 ? Rojo : NaranjaTexto);
                CeldaDerecha(f.UltimaVenta.HasValue ? f.UltimaVenta.Value.ToString("dd/MM/yyyy") : _t.Obtener("rcmb_nunca"), ref x, 2, y, Gris);
                CeldaDerecha(f.CoberturaMeses.HasValue ? _t.Obtener("rcmb_meses", f.CoberturaMeses.Value.ToString("0.#")) : _t.Obtener("rcmb_sin_ventas"),
                        ref x, 3, y, Gris);
                float d = x + AnchosParado[6] - 8f;
                _pdf.Texto(Plata(f.CostoInmovilizado), d, y + 12f, 8.5f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                _pdf.Texto(_t.Obtener("rcmb_doc_costo_unit", Plata(f.CostoUnitario)) + (f.CostoEstimado ? "*" : ""), d, y + 22f, 6.5f,
                           FuentePdf06AV.Normal, Gris, AlineacionPdf06AV.Derecha);
                y += alto;
            }
            return y;
        }

        private void CeldaDerecha(string texto, ref float x, int columnaRelativa, float y, Color color)
        {
            float ancho = AnchosParado[2 + columnaRelativa];
            _pdf.Texto(_pdf.Recortar(texto, ancho - 10f, 8f, FuentePdf06AV.Normal), x + ancho - 8f, y + 17f, 8f,
                       FuentePdf06AV.Normal, color, AlineacionPdf06AV.Derecha);
            x += ancho;
        }

        private float SinDatos(float y)
        {
            _pdf.Rect(M, y, AnchoUtil, 34f, Fondo);
            _pdf.Texto(_t.Obtener("rep_sin_datos"), M + 12f, y + 21f, 9f, FuentePdf06AV.Cursiva, Gris);
            return y + 34f;
        }

        private float Metodologia(float y)
        {
            var lineas = new List<string>();
            for (int i = 1; i <= 5; i++)
                lineas.AddRange(_pdf.Envolver(_t.Obtener("rcmb_doc_metodo_" + i), AnchoUtil - 28f, 7.5f, FuentePdf06AV.Normal));
            lineas.AddRange(_pdf.Envolver(_t.Obtener("rcmb_nota_costo"), AnchoUtil - 28f, 7.5f, FuentePdf06AV.Cursiva));

            float alto = 30f + lineas.Count * 10.5f;
            if (y + alto > LimiteContenido) y = PaginaContinuacion();
            _pdf.RectRedondeado(M, y, AnchoUtil, alto, 6f, CianSuave);
            Etiqueta(_t.Obtener("rcmb_doc_metodologia"), M + 14f, y + 17f);
            float ly = y + 31f;
            foreach (string l in lineas)
            {
                _pdf.Texto(l, M + 14f, ly, 7.5f, FuentePdf06AV.Normal, Tinta);
                ly += 10.5f;
            }
            return y + alto;
        }
    }
}
