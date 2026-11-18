using SER;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    /// <summary>Un síntoma y su solución ("Si algo falla").</summary>
    internal sealed class ProblemaAyuda06AV
    {
        public string Sintoma;
        public string Solucion;
    }

    /// <summary>Un paso numerado de un tema del manual.</summary>
    internal sealed class PasoManual06AV
    {
        public string Titulo;
        public string Detalle;
    }

    /// <summary>Un tema del manual (una hoja del árbol del centro de ayuda).</summary>
    internal sealed class TemaManual06AV
    {
        public string Id, Codigo, Titulo, Rol, Pantalla, Modulo, Recorrido, Resumen, Resultado;
        public SeccionManual06AV Seccion;
        public readonly List<string> Antes = new List<string>();
        public readonly List<PasoManual06AV> Pasos = new List<PasoManual06AV>();
        public readonly List<string> Validaciones = new List<string>();
        public readonly List<ProblemaAyuda06AV> Problemas = new List<ProblemaAyuda06AV>();
        public readonly List<string> Relacionados = new List<string>();

        /// <summary>Título con el código del caso de uso adelante, si tiene.</summary>
        public string TituloCompleto => string.IsNullOrEmpty(Codigo) ? Titulo : Codigo + " · " + Titulo;

        /// <summary>Todo el texto del tema, para la búsqueda.</summary>
        public string TextoBuscable()
        {
            var partes = new List<string> { Codigo, Titulo, Rol, Pantalla, Resumen, Resultado };
            partes.AddRange(Antes);
            partes.AddRange(Pasos.Select(p => p.Titulo + " " + p.Detalle));
            partes.AddRange(Validaciones);
            partes.AddRange(Problemas.Select(p => p.Sintoma + " " + p.Solucion));
            return string.Join(" ", partes.Where(x => !string.IsNullOrEmpty(x)));
        }
    }

    /// <summary>Un grupo del árbol (Primeros pasos, PN1, PN2…).</summary>
    internal sealed class SeccionManual06AV
    {
        public string Id, Titulo;
        public bool Flujo;
        public readonly List<TemaManual06AV> Temas = new List<TemaManual06AV>();
    }

    /// <summary>
    /// Manual de uso del sistema. El contenido está en Resources\Ayuda\manual.{idioma}.xml
    /// (uno por idioma, misma estructura) y se carga según el idioma activo. Si falta el
    /// archivo del idioma se usa el español.
    /// </summary>
    internal static class ManualAyuda06AV
    {
        private static string _idiomaCargado;
        private static List<SeccionManual06AV> _secciones = new List<SeccionManual06AV>();
        private static Dictionary<string, TemaManual06AV> _porId = new Dictionary<string, TemaManual06AV>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<SeccionManual06AV> Secciones { get { Asegurar(); return _secciones; } }

        public static IEnumerable<TemaManual06AV> Temas => Secciones.SelectMany(s => s.Temas);

        public static TemaManual06AV Tema(string id)
        {
            Asegurar();
            return id != null && _porId.TryGetValue(id, out var t) ? t : null;
        }

        /// <summary>Temas que se trabajan en una pantalla (por su clave de menú). Primero los casos de uso.</summary>
        public static IEnumerable<TemaManual06AV> DeModulo(string claveModulo)
        {
            if (string.IsNullOrEmpty(claveModulo)) return Enumerable.Empty<TemaManual06AV>();
            return Temas.Where(t => string.Equals(t.Modulo, claveModulo, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(t => string.IsNullOrEmpty(t.Codigo) ? 1 : 0);
        }

        /// <summary>Busca en todo el texto de los temas. Todas las palabras deben aparecer (sin tildes ni mayúsculas).</summary>
        public static IEnumerable<TemaManual06AV> Buscar(string texto)
        {
            var palabras = Normalizar(texto).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (palabras.Length == 0) return Temas;
            return Temas.Where(t =>
            {
                string todo = Normalizar(t.TextoBuscable());
                return palabras.All(p => todo.Contains(p));
            });
        }

        public static string Normalizar(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var d = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(d.Length);
            foreach (char c in d)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString();
        }

        private static void Asegurar()
        {
            string idioma = (GestorIdioma06AV.Instancia.IdiomaActual ?? "es").ToLowerInvariant();
            if (idioma == _idiomaCargado) return;
            _idiomaCargado = idioma;
            _secciones = new List<SeccionManual06AV>();
            _porId = new Dictionary<string, TemaManual06AV>(StringComparer.OrdinalIgnoreCase);

            string ruta = Ruta(idioma) ?? Ruta("es");
            if (ruta == null) return;
            try { Cargar(XDocument.Load(ruta)); }
            catch { _secciones.Clear(); _porId.Clear(); }
        }

        private static string Ruta(string idioma)
        {
            string nombre = "manual." + idioma + ".xml";
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 5 && dir != null; i++, dir = dir.Parent)
                {
                    string candidato = Path.Combine(dir.FullName, "Resources", "Ayuda", nombre);
                    if (File.Exists(candidato)) return candidato;
                }
            }
            catch { }
            return null;
        }

        private static string Txt(XElement e) => e == null ? null : string.Join(" ",
            e.Value.Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())).Trim();

        private static void Cargar(XDocument doc)
        {
            foreach (var xs in doc.Root.Elements("seccion"))
            {
                var s = new SeccionManual06AV
                {
                    Id = (string)xs.Attribute("id"),
                    Titulo = (string)xs.Attribute("titulo"),
                    Flujo = (string)xs.Attribute("flujo") == "true"
                };
                foreach (var xt in xs.Elements("tema"))
                {
                    var t = new TemaManual06AV
                    {
                        Seccion = s,
                        Id = (string)xt.Attribute("id"),
                        Codigo = (string)xt.Attribute("codigo"),
                        Titulo = (string)xt.Attribute("titulo"),
                        Rol = (string)xt.Attribute("rol"),
                        Pantalla = (string)xt.Attribute("pantalla"),
                        Modulo = (string)xt.Attribute("modulo"),
                        Recorrido = (string)xt.Attribute("recorrido"),
                        Resumen = Txt(xt.Element("resumen")),
                        Resultado = Txt(xt.Element("resultado"))
                    };
                    t.Antes.AddRange(xt.Elements("antes").Select(Txt));
                    t.Pasos.AddRange(xt.Elements("paso").Select(x => new PasoManual06AV { Titulo = (string)x.Attribute("titulo"), Detalle = Txt(x) }));
                    t.Validaciones.AddRange(xt.Elements("valida").Select(Txt));
                    t.Problemas.AddRange(xt.Elements("problema").Select(x => new ProblemaAyuda06AV { Sintoma = (string)x.Attribute("sintoma"), Solucion = Txt(x) }));
                    t.Relacionados.AddRange(xt.Elements("ver").Select(Txt));
                    s.Temas.Add(t);
                    if (!string.IsNullOrEmpty(t.Id)) _porId[t.Id] = t;
                }
                _secciones.Add(s);
            }
        }
    }
}
