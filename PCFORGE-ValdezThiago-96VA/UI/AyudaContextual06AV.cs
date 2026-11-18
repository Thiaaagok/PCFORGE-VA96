using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    internal sealed class TemaAyuda06AV
    {
        public string Titulo;
        public string Texto;
        public bool Especifica;
    }

    /// <summary>
    /// Ayuda en línea sensible al contexto. Resuelve la explicación del control que está bajo
    /// el mouse sin que cada pantalla tenga que registrarla:
    ///   1. Clave registrada a mano con Registrar(control, clave).
    ///   2. Texto visible del control → su clave de idioma → "ayuda_ctl_&lt;clave&gt;".
    ///   3. Campos de entrada: el rótulo (Label) que tienen a la izquierda o arriba.
    ///   4. Explicación genérica según el tipo de control (grilla, lista, fecha...).
    /// La ayuda de cada pantalla sale de "ayuda_pant_&lt;claveMenu&gt;" + los casos de uso de la
    /// guía que se trabajan en esa pantalla + las opciones (botones) que tiene visibles.
    /// </summary>
    internal static class AyudaContextual06AV
    {
        public const string PrefijoControl = "ayuda_ctl_";
        public const string PrefijoPantalla = "ayuda_pant_";

        private static readonly ConditionalWeakTable<Control, string> Registradas = new ConditionalWeakTable<Control, string>();

        private static GestorIdioma06AV T => GestorIdioma06AV.Instancia;

        public static void Registrar(Control control, string claveAyuda)
        {
            if (control == null || string.IsNullOrEmpty(claveAyuda)) return;
            Registradas.Remove(control);
            Registradas.Add(control, claveAyuda);
        }

        // ── Ayuda de un control ──────────────────────────────────────

        public static TemaAyuda06AV DeControl(Control control, Control raiz)
        {
            for (Control c = control; c != null && c != raiz?.Parent; c = c.Parent)
            {
                TemaAyuda06AV tema = Especifica(c);
                if (tema != null) return tema;
                if (c == raiz) break;
            }
            return Generica(control);
        }

        private static TemaAyuda06AV Especifica(Control c)
        {
            if (!c.Visible) return null;

            if (Registradas.TryGetValue(c, out string registrada) && T.Existe(registrada))
                return new TemaAyuda06AV { Titulo = Rotulo(c), Texto = T.Obtener(registrada), Especifica = true };

            if (EsAccionable(c))
            {
                string clave = ClaveAyudaDeTexto(c.Text);
                if (clave != null)
                    return new TemaAyuda06AV { Titulo = Rotulo(c), Texto = T.Obtener(clave), Especifica = true };
            }

            if (EsEntrada(c))
            {
                Label rotulo = RotuloAsociado(c);
                string clave = rotulo != null ? ClaveAyudaDeTexto(rotulo.Text) : null;
                if (clave != null)
                    return new TemaAyuda06AV { Titulo = Rotulo(rotulo), Texto = T.Obtener(clave), Especifica = true };
            }
            return null;
        }

        private static TemaAyuda06AV Generica(Control c)
        {
            if (c == null) return null;
            string clave;
            if (c is DataGridView || c is ListView) clave = "ayuda_gen_grilla";
            else if (c is DateTimePicker) clave = "ayuda_gen_fecha";
            else if (c is NumericUpDown) clave = "ayuda_gen_numero";
            else if (c is ComboBox || c is ListBox) clave = "ayuda_gen_lista";
            else if (c is CheckBox || c is RadioButton) clave = "ayuda_gen_casilla";
            else if (c is TextBoxBase) clave = "ayuda_gen_campo";
            else if (c is ButtonBase) clave = "ayuda_gen_boton";
            else return null;

            string titulo = (EsEntrada(c) ? Rotulo(RotuloAsociado(c)) : null)
                            ?? (c is TextBoxBase || c is ComboBox ? null : Rotulo(c))
                            ?? T.Obtener(clave + "_tit");
            return new TemaAyuda06AV { Titulo = titulo, Texto = T.Obtener(clave, titulo), Especifica = false };
        }

        private static bool EsAccionable(Control c) =>
            c is ButtonBase || c is Label || c.Cursor == Cursors.Hand;

        private static bool EsEntrada(Control c) =>
            c is TextBoxBase || c is ComboBox || c is NumericUpDown || c is DateTimePicker || c is ListBox;

        private static string Rotulo(Control c)
        {
            if (c == null) return null;
            string texto = GestorIdioma06AV.NormalizarTexto(c.Text);
            return texto.Length == 0 ? null : texto;
        }

        // Pantalla activa (clave del menú): desempata textos iguales con ayudas distintas,
        // p. ej. "Registrar orden" en Compras (CU08) y en Producción (CU04).
        public static string ModuloActual { get; set; }

        private static readonly Dictionary<string, string[]> Contexto = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "pcf_asis_confirmar", new[] { "pcf_menu_compras" } },
            { "pcf_asis_limpiar", new[] { "pcf_menu_compras" } },
            { "pcf_cotp_volver", new[] { "pcf_menu_compras", "pcf_menu_cotizaciones" } },
            { "pcf_registrar_orden", new[] { "pcf_menu_produccion" } },
            { "ser_ninguno", new[] { "menu_serializacion" } }
        };

        public static string ClaveAyudaDeTexto(string texto)
        {
            var candidatas = T.ClavesConTexto(texto).Where(c => T.Existe(PrefijoControl + c)).ToList();
            if (candidatas.Count == 0) return null;
            string elegida =
                candidatas.FirstOrDefault(c => Contexto.TryGetValue(c, out var m) &&
                                               m.Contains(ModuloActual ?? "", StringComparer.OrdinalIgnoreCase))
                ?? candidatas.FirstOrDefault(c => !Contexto.ContainsKey(c))
                ?? candidatas[0];
            return PrefijoControl + elegida;
        }

        // El Label más cercano a la izquierda en la misma fila o, si no hay, el de arriba.
        private static Label RotuloAsociado(Control c)
        {
            Control padre = c?.Parent;
            if (padre == null) return null;
            var labels = padre.Controls.OfType<Label>().Where(l => l.Visible && !string.IsNullOrWhiteSpace(l.Text)).ToList();

            Label izquierda = labels
                .Where(l => Math.Abs(Centro(l) - Centro(c)) <= 14 && l.Right <= c.Left + 6)
                .OrderBy(l => c.Left - l.Right).FirstOrDefault();
            if (izquierda != null) return izquierda;

            return labels
                .Where(l => l.Bottom <= c.Top + 6 && c.Top - l.Bottom <= 40 && l.Left < c.Right && l.Right > c.Left - 20)
                .OrderBy(l => c.Top - l.Bottom).FirstOrDefault();
        }

        private static int Centro(Control c) => c.Top + c.Height / 2;

        // ── Ayuda de una pantalla ────────────────────────────────────

        public static string DescripcionPantalla(string claveModulo)
        {
            string clave = PrefijoPantalla + (claveModulo ?? "");
            return T.Existe(clave) ? T.Obtener(clave) : T.Obtener("ayuda_pant_sin_descripcion");
        }

        public static string[] PasosPantalla(string claveModulo)
        {
            string clave = PrefijoPantalla + (claveModulo ?? "") + "_pasos";
            return T.Existe(clave)
                ? T.Obtener(clave).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];
        }

        public static IEnumerable<TemaManual06AV> CasosDeUso(string claveModulo) => ManualAyuda06AV.DeModulo(claveModulo);

        // Opciones de trabajo: botones y casillas visibles de la pantalla que tienen ayuda propia.
        public static List<TemaAyuda06AV> OpcionesPantalla(Control pantalla)
        {
            var lista = new List<TemaAyuda06AV>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Control c in Recorrer(pantalla))
            {
                if (!(c is ButtonBase) || !c.Visible) continue;
                string titulo = Rotulo(c);
                if (titulo == null || !vistos.Add(titulo)) continue;
                TemaAyuda06AV tema = Especifica(c);
                if (tema != null) lista.Add(tema);
            }
            return lista;
        }

        private static IEnumerable<Control> Recorrer(Control raiz)
        {
            if (raiz == null) yield break;
            foreach (Control c in raiz.Controls)
            {
                yield return c;
                foreach (Control h in Recorrer(c)) yield return h;
            }
        }
    }
}
