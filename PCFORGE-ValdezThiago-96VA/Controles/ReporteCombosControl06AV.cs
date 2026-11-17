using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using SER.Exportacion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    // Reporte inteligente: combos y ofertas por stock inmovilizado (ver ReporteCombosBLL06AV).
    [DesignerCategory("Code")]
    public class ReporteCombosControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly ReporteCombosBLL06AV _bll = new ReporteCombosBLL06AV();
        private ReporteCombos06AV _reporte;
        private List<ComboSugerido06AV> _combosVisibles = new List<ComboSugerido06AV>();

        private Label lblTitulo, lblSubtitulo, lblUmbral, lblVentana, lblPublico, lblPrioridad, lblCombos, lblParados, lblNota;
        private NumericUpDown nudUmbral, nudVentana;
        private ComboBox cboPublico, cboPrioridad;
        private Button btnGenerar, btnPdf, btnExcel;
        private FlowLayoutPanel flpKpis;
        private readonly List<Kpi> _kpis = new List<Kpi>();
        private DataGridView gridCombos, gridParados;
        private Panel pnlCabecera, pnlFiltros;
        private TableLayoutPanel tlpCuerpo;

        public ReporteCombosControl06AV()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            Generar();
        }

        private void ConstruirUI()
        {
            var defecto = new ParametrosReporteCombos06AV();

            lblTitulo = new Label { AutoSize = true, Location = new Point(6, 12) };
            lblSubtitulo = new Label { AutoSize = true, Location = new Point(8, 42) };
            pnlCabecera = new Panel { Dock = DockStyle.Top, Height = 68 };
            pnlCabecera.Controls.Add(lblSubtitulo);
            pnlCabecera.Controls.Add(lblTitulo);

            lblUmbral = Rotulo();
            nudUmbral = new NumericUpDown { Width = 64, Minimum = 1, Maximum = 3650, Value = defecto.UmbralDias, Margin = new Padding(0, 4, 14, 0) };
            lblVentana = Rotulo();
            nudVentana = new NumericUpDown { Width = 64, Minimum = 7, Maximum = 730, Value = defecto.DiasVentana, Margin = new Padding(0, 4, 14, 0) };
            lblPublico = Rotulo();
            cboPublico = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 14, 0) };
            cboPublico.SelectedIndexChanged += (s, e) => Filtrar();
            lblPrioridad = Rotulo();
            cboPrioridad = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 14, 0) };
            cboPrioridad.SelectedIndexChanged += (s, e) => Filtrar();

            btnGenerar = Boton(110);
            btnPdf = Boton(150);
            btnExcel = Boton(150);
            btnGenerar.Click += (s, e) => Generar();
            btnPdf.Click += (s, e) => ExportarPdf();
            btnExcel.Click += (s, e) => ExportarExcel();

            var flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(6, 10, 0, 0)
            };
            flpFiltros.Controls.AddRange(new Control[] { lblUmbral, nudUmbral, lblVentana, nudVentana, lblPublico, cboPublico, lblPrioridad, cboPrioridad });
            var flpBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 14, 8, 0)
            };
            flpBotones.Controls.AddRange(new Control[] { btnGenerar, btnPdf, btnExcel });
            pnlFiltros = new Panel { Dock = DockStyle.Top, Height = 46 };
            pnlFiltros.Controls.Add(flpFiltros);
            pnlCabecera.Controls.Add(flpBotones);

            flpKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 92, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4, 8, 4, 8)
            };
            for (int i = 0; i < 4; i++)
            {
                var k = new Kpi(i == 1);
                _kpis.Add(k);
                flpKpis.Controls.Add(k.Panel);
            }

            lblCombos = new Label { AutoSize = true, Margin = new Padding(6, 8, 0, 4) };
            lblParados = new Label { AutoSize = true, Margin = new Padding(6, 10, 0, 4) };
            lblNota = new Label { AutoSize = true, Margin = new Padding(6, 4, 0, 4) };
            gridCombos = CrearGrilla();
            gridCombos.DataBindingComplete += (s, e) => FormatearCombos();
            gridParados = CrearGrilla();
            gridParados.DataBindingComplete += (s, e) => FormatearParados();

            tlpCuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            tlpCuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpCuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            tlpCuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpCuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            tlpCuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpCuerpo.Controls.Add(lblCombos, 0, 0);
            tlpCuerpo.Controls.Add(gridCombos, 0, 1);
            tlpCuerpo.Controls.Add(lblParados, 0, 2);
            tlpCuerpo.Controls.Add(gridParados, 0, 3);
            tlpCuerpo.Controls.Add(lblNota, 0, 4);

            Controls.Add(tlpCuerpo);
            Controls.Add(flpKpis);
            Controls.Add(pnlFiltros);
            Controls.Add(pnlCabecera);
        }

        private static DataGridView CrearGrilla() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, Margin = new Padding(4, 0, 4, 0)
        };

        private static Label Rotulo() => new Label { AutoSize = true, Margin = new Padding(0, 8, 4, 0) };

        private static Button Boton(int ancho) => new Button
        {
            Width = ancho, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private void Generar()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                _reporte = _bll.Generar(new ParametrosReporteCombos06AV
                {
                    UmbralDias = (int)nudUmbral.Value,
                    DiasVentana = (int)nudVentana.Value
                });
                Cursor = Cursors.Default;
                Filtrar();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                _reporte = null;
                Filtrar();
                Aviso(ex.Message);
            }
        }

        private void Filtrar()
        {
            if (gridCombos == null || cboPublico == null || cboPrioridad == null) return;
            var t = GestorIdioma06AV.Instancia;

            _combosVisibles = (_reporte?.Combos ?? new List<ComboSugerido06AV>())
                .Where(c => cboPublico.SelectedIndex <= 0 || (int)c.Publico == cboPublico.SelectedIndex - 1)
                .Where(c => cboPrioridad.SelectedIndex <= 0 || (int)c.Prioridad == cboPrioridad.SelectedIndex - 1)
                .OrderBy(c => c.Prioridad).ThenByDescending(c => c.IngresoPotencial).ToList();

            gridCombos.DataSource = null;
            gridCombos.DataSource = _combosVisibles.Select(c => new ComboVm
            {
                Prioridad = NombrePrioridad(c.Prioridad),
                Codigo = c.Codigo,
                Combo = string.Join(" + ", c.Items.Select(i => i.Descripcion)),
                Publico = NombrePublico(c.Publico),
                Unidades = c.Unidades,
                Lista = c.PrecioLista.ToString("C0"),
                Precio = c.PrecioCombo.ToString("C0"),
                Descuento = c.PorcentajeDescuento.ToString("0") + " %",
                Margen = c.MargenPorcentaje.ToString("0.#") + " %" + (c.CostoEstimado ? " *" : ""),
                Probabilidad = (c.Probabilidad * 100m).ToString("0") + " %",
                Ingreso = c.IngresoPotencial.ToString("C0"),
                Fila = c
            }).ToList();

            List<FilaStockParado06AV> parados = _reporte?.Parados ?? new List<FilaStockParado06AV>();
            gridParados.DataSource = null;
            gridParados.DataSource = parados.Select(f => new ParadoVm
            {
                Codigo = f.Codigo,
                Componente = f.Nombre,
                Tipo = NombreTipo(f.Tipo),
                StockLibre = f.StockLibre,
                Dias = f.DiasSinRotar,
                UltimaVenta = f.UltimaVenta.HasValue ? f.UltimaVenta.Value.ToString("dd/MM/yyyy") : t.Obtener("rcmb_nunca"),
                Cobertura = f.CoberturaMeses.HasValue ? t.Obtener("rcmb_meses", f.CoberturaMeses.Value.ToString("0.#")) : t.Obtener("rcmb_sin_ventas"),
                Costo = f.CostoUnitario.ToString("C0") + (f.CostoEstimado ? " *" : ""),
                Inmovilizado = f.CostoInmovilizado.ToString("C0")
            }).ToList();

            ActualizarKpis();
            bool hay = _reporte != null && (_reporte.Combos.Count > 0 || _reporte.Parados.Count > 0);
            btnPdf.Enabled = hay;
            btnExcel.Enabled = _combosVisibles.Count > 0;
            AplicarEstiloBotones();
        }

        private void ActualizarKpis()
        {
            var t = GestorIdioma06AV.Instancia;
            var parados = _reporte?.Parados ?? new List<FilaStockParado06AV>();
            var combos = _reporte?.Combos ?? new List<ComboSugerido06AV>();
            _kpis[0].Definir(t.Obtener("rcmb_kpi_parados"), parados.Count.ToString(),
                             t.Obtener("rcmb_kpi_unidades", parados.Sum(f => f.StockLibre)));
            _kpis[1].Definir(t.Obtener("rcmb_kpi_inmovilizado"), (_reporte?.CapitalInmovilizado ?? 0m).ToString("C0"),
                             t.Obtener("rcmb_kpi_dias_prom", parados.Count == 0 ? 0 : (int)parados.Average(f => f.DiasSinRotar)));
            _kpis[2].Definir(t.Obtener("rcmb_kpi_combos"), combos.Count.ToString(),
                             t.Obtener("rcmb_kpi_alta", combos.Count(c => c.Prioridad == PrioridadPublicacion06AV.Alta)));
            _kpis[3].Definir(t.Obtener("rcmb_kpi_ingreso"), combos.Sum(c => c.IngresoPotencial).ToString("C0"),
                             t.Obtener("rcmb_kpi_ingreso_pie"));
        }

        private void FormatearCombos()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key, float peso, bool derecha = false)
            {
                if (gridCombos.Columns[col] == null) return;
                gridCombos.Columns[col].HeaderText = t.Obtener(key);
                gridCombos.Columns[col].FillWeight = peso;
                if (derecha) gridCombos.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            H("Prioridad", "rcmb_col_prioridad", 55);
            H("Codigo", "rcmb_col_codigo", 55);
            H("Combo", "rcmb_col_combo", 230);
            H("Publico", "rcmb_col_publico", 60);
            H("Unidades", "rcmb_col_unidades", 45, true);
            H("Lista", "rcmb_col_lista", 70, true);
            H("Precio", "rcmb_col_precio", 70, true);
            H("Descuento", "rcmb_col_descuento", 50, true);
            H("Margen", "rcmb_col_margen", 55, true);
            H("Probabilidad", "rcmb_col_probabilidad", 55, true);
            H("Ingreso", "rcmb_col_ingreso", 75, true);

            foreach (DataGridViewRow row in gridCombos.Rows)
            {
                if (!(row.DataBoundItem is ComboVm vm) || gridCombos.Columns["Prioridad"] == null) continue;
                Color c = vm.Fila.Prioridad == PrioridadPublicacion06AV.Alta ? Tema.Peligro
                        : vm.Fila.Prioridad == PrioridadPublicacion06AV.Media ? Tema.Advertencia : Tema.TextoSuave;
                row.Cells["Prioridad"].Style.ForeColor = c;
                row.Cells["Prioridad"].Style.Font = Tema.FuenteMini;
                row.Cells["Combo"].ToolTipText = string.Join(Environment.NewLine,
                    vm.Fila.Items.Select(i => $"{i.Codigo} · {i.Nombre} · {i.PrecioUnitario:C0}" +
                                              (i.BajaRotacion ? "  [" + t.Obtener("rcmb_parado_dias", i.DiasSinRotar) + "]" : "")));
            }
        }

        private void FormatearParados()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key, float peso, bool derecha = false)
            {
                if (gridParados.Columns[col] == null) return;
                gridParados.Columns[col].HeaderText = t.Obtener(key);
                gridParados.Columns[col].FillWeight = peso;
                if (derecha) gridParados.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            H("Codigo", "rcmb_col_codigo", 60);
            H("Componente", "rcmb_col_componente", 200);
            H("Tipo", "rcmb_col_tipo", 80);
            H("StockLibre", "rcmb_col_stock", 55, true);
            H("Dias", "rcmb_col_dias", 55, true);
            H("UltimaVenta", "rcmb_col_ultima_venta", 70, true);
            H("Cobertura", "rcmb_col_cobertura", 70, true);
            H("Costo", "rcmb_col_costo", 65, true);
            H("Inmovilizado", "rcmb_col_inmovilizado", 80, true);
        }

        private string NombrePrioridad(PrioridadPublicacion06AV p) =>
            GestorIdioma06AV.Instancia.Obtener("rcmb_prioridad_" + p.ToString().ToLowerInvariant());

        private string NombrePublico(PublicoObjetivo06AV p) =>
            GestorIdioma06AV.Instancia.Obtener("rcmb_publico_" + p.ToString().ToLowerInvariant());

        internal static string NombreTipo(TipoComponente06AV tipo) =>
            GestorIdioma06AV.Instancia.Obtener("rcmb_tipo_" + tipo.ToString().ToLowerInvariant());

        private void ExportarPdf()
        {
            if (_reporte == null) return;
            var t = GestorIdioma06AV.Instancia;
            using (var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"Reporte_Combos_StockParado_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    Image logo = null;
                    try { logo = Properties.Resources.pcforge_logo1; } catch { }
                    ReporteCombosPdf06AV.Guardar(sfd.FileName, _reporte, _combosVisibles, logo);
                    Cursor = Cursors.Default;
                    if (ConfirmacionForm.Mostrar(t.Obtener("pdf_generado"), t.Obtener("rcmb_titulo"),
                            ConfirmacionForm.TipoConfirmacion.Info, t.Obtener("pcf_oc_abrir"), t.Obtener("aceptar"), FindForm()))
                        ComprobanteCompras06AV.Abrir(sfd.FileName);
                }
                catch (Exception ex) { Cursor = Cursors.Default; Aviso(ex.Message); }
            }
        }

        private void ExportarExcel()
        {
            if (_combosVisibles.Count == 0) return;
            var t = GestorIdioma06AV.Instancia;
            using (var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"Reporte_Combos_StockParado_{DateTime.Now:yyyyMMdd_HHmmss}.csv" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    var columnas = new Dictionary<string, Func<ComboSugerido06AV, object>>
                    {
                        { t.Obtener("rcmb_col_prioridad"), c => NombrePrioridad(c.Prioridad) },
                        { t.Obtener("rcmb_col_codigo"), c => c.Codigo },
                        { t.Obtener("rcmb_col_combo"), c => string.Join(" + ", c.Items.Select(i => i.Codigo + " " + i.Descripcion)) },
                        { t.Obtener("rcmb_col_publico"), c => NombrePublico(c.Publico) },
                        { t.Obtener("rcmb_col_unidades"), c => c.Unidades },
                        { t.Obtener("rcmb_col_lista"), c => c.PrecioLista.ToString("0.00", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_precio"), c => c.PrecioCombo.ToString("0.00", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_descuento"), c => c.PorcentajeDescuento.ToString("0", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_margen"), c => c.MargenPorcentaje.ToString("0.#", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_probabilidad"), c => (c.Probabilidad * 100m).ToString("0", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_ingreso"), c => c.IngresoPotencial.ToString("0.00", CultureInfo.CurrentCulture) },
                        { t.Obtener("rcmb_col_inmovilizado"), c => c.CostoInmovilizado.ToString("0.00", CultureInfo.CurrentCulture) }
                    };
                    new ExportacionEXCEL().Exportar(_combosVisibles, columnas, sfd.FileName);
                    ConfirmacionForm.MostrarInfo(t.Obtener("archivo_generado"), t.Obtener("rcmb_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                }
                catch (Exception ex) { Aviso(ex.Message); }
            }
        }

        private void Aviso(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private void AplicarEstiloBotones()
        {
            Tema.AplicarBotonSecundario(btnGenerar);
            if (btnPdf.Enabled) Tema.AplicarBotonPrimario(btnPdf); else Tema.AplicarBotonDeshabilitado(btnPdf);
            if (btnExcel.Enabled) Tema.AplicarBotonSecundario(btnExcel); else Tema.AplicarBotonDeshabilitado(btnExcel);
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;
            foreach (Control c in new Control[] { pnlCabecera, pnlFiltros, flpKpis, tlpCuerpo })
            {
                c.BackColor = Tema.FondoApp;
                foreach (Control h in c.Controls) if (h is FlowLayoutPanel) h.BackColor = Tema.FondoApp;
            }
            Tema.AplicarTitulo(lblTitulo);
            lblSubtitulo.Font = Tema.FuenteRegular;
            lblSubtitulo.ForeColor = Tema.TextoSuave;
            foreach (Label l in new[] { lblTitulo, lblSubtitulo, lblCombos, lblParados, lblNota })
                l.BackColor = Tema.FondoApp;
            foreach (Label l in new[] { lblCombos, lblParados })
            {
                l.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
                l.ForeColor = Tema.TextoFuerte;
            }
            lblNota.Font = Tema.FuenteMini;
            lblNota.ForeColor = Tema.TextoSuave;
            foreach (Label l in new[] { lblUmbral, lblVentana, lblPublico, lblPrioridad })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
            }
            foreach (Control c in new Control[] { nudUmbral, nudVentana, cboPublico, cboPrioridad }) Tema.AplicarEntrada(c);
            Tema.AplicarGrilla(gridCombos);
            Tema.AplicarGrilla(gridParados);
            foreach (Kpi k in _kpis) k.AplicarTema();
            AplicarEstiloBotones();
            if (gridCombos.DataSource != null) FormatearCombos();
            if (gridParados.DataSource != null) FormatearParados();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("rcmb_titulo");
            lblSubtitulo.Text = t.Obtener("rcmb_subtitulo");
            lblUmbral.Text = t.Obtener("rcmb_umbral");
            lblVentana.Text = t.Obtener("rcmb_ventana");
            lblPublico.Text = t.Obtener("rcmb_col_publico");
            lblPrioridad.Text = t.Obtener("rcmb_col_prioridad");
            lblCombos.Text = t.Obtener("rcmb_seccion_combos");
            lblParados.Text = t.Obtener("rcmb_seccion_parados");
            lblNota.Text = t.Obtener("rcmb_nota_costo");
            btnGenerar.Text = t.Obtener("rcmb_generar");
            btnPdf.Text = t.Obtener("rep_pdf");
            btnExcel.Text = t.Obtener("rep_excel");

            int publico = cboPublico.SelectedIndex;
            cboPublico.Items.Clear();
            cboPublico.Items.Add(t.Obtener("rep_todos"));
            foreach (PublicoObjetivo06AV p in Enum.GetValues(typeof(PublicoObjetivo06AV))) cboPublico.Items.Add(NombrePublico(p));
            cboPublico.SelectedIndex = publico >= 0 ? publico : 0;

            int prioridad = cboPrioridad.SelectedIndex;
            cboPrioridad.Items.Clear();
            cboPrioridad.Items.Add(t.Obtener("rep_todos"));
            foreach (PrioridadPublicacion06AV p in Enum.GetValues(typeof(PrioridadPublicacion06AV))) cboPrioridad.Items.Add(NombrePrioridad(p));
            cboPrioridad.SelectedIndex = prioridad >= 0 ? prioridad : 0;

            Filtrar();
        }

        private sealed class Kpi
        {
            private readonly Label _titulo, _valor, _pie;
            private readonly bool _acento;
            public Panel Panel { get; }

            public Kpi(bool acento)
            {
                _acento = acento;
                Panel = new Panel { Width = 230, Height = 74, Margin = new Padding(6, 0, 6, 0) };
                _titulo = new Label { AutoSize = false, Location = new Point(14, 8), Size = new Size(210, 16) };
                _valor = new Label { AutoSize = false, Location = new Point(14, 26), Size = new Size(210, 26), AutoEllipsis = true };
                _pie = new Label { AutoSize = false, Location = new Point(14, 52), Size = new Size(210, 16), AutoEllipsis = true };
                Panel.Controls.AddRange(new Control[] { _titulo, _valor, _pie });
                Panel.Paint += (s, e) =>
                {
                    using (var b = new SolidBrush(_acento ? Tema.Acento : Tema.Primario))
                        e.Graphics.FillRectangle(b, 0, 10, 4, Panel.Height - 20);
                    using (var p = new Pen(Tema.Borde))
                        e.Graphics.DrawRectangle(p, 0, 0, Panel.Width - 1, Panel.Height - 1);
                };
            }

            public void Definir(string titulo, string valor, string pie)
            {
                _titulo.Text = (titulo ?? "").ToUpper();
                _valor.Text = valor;
                _pie.Text = pie;
            }

            public void AplicarTema()
            {
                Panel.BackColor = Tema.FondoPanel;
                _titulo.Font = Tema.FuenteMini;
                _titulo.ForeColor = Tema.TextoSuave;
                _valor.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
                _valor.ForeColor = Tema.TextoFuerte;
                _pie.Font = new Font("Segoe UI", 8.25f);
                _pie.ForeColor = Tema.TextoSuave;
                foreach (Control c in Panel.Controls) c.BackColor = Tema.FondoPanel;
                Panel.Invalidate();
            }
        }

        private class ComboVm
        {
            public string Prioridad { get; set; }
            public string Codigo { get; set; }
            public string Combo { get; set; }
            public string Publico { get; set; }
            public int Unidades { get; set; }
            public string Lista { get; set; }
            public string Precio { get; set; }
            public string Descuento { get; set; }
            public string Margen { get; set; }
            public string Probabilidad { get; set; }
            public string Ingreso { get; set; }
            [Browsable(false)] public ComboSugerido06AV Fila { get; set; }
        }

        private class ParadoVm
        {
            public string Codigo { get; set; }
            public string Componente { get; set; }
            public string Tipo { get; set; }
            public int StockLibre { get; set; }
            public int Dias { get; set; }
            public string UltimaVenta { get; set; }
            public string Cobertura { get; set; }
            public string Costo { get; set; }
            public string Inmovilizado { get; set; }
        }
    }
}
