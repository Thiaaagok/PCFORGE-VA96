using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.UI
{
    // Un caso de uso de la guía (numeración de la tabla G04 de la documentación). Los textos
    // salen de las claves guia_cuNN_* de Resources\Idiomas, así siguen el idioma activo.
    internal sealed class PasoGuia06AV
    {
        public PasoGuia06AV(string codigo, string claveModulo)
        {
            Codigo = codigo;
            ClaveModulo = claveModulo;
        }

        public string Codigo { get; }
        public string ClaveModulo { get; }

        private string Clave(string campo) => "guia_" + Codigo.ToLowerInvariant() + "_" + campo;
        private string Texto(string campo) => GestorIdioma06AV.Instancia.Obtener(Clave(campo));

        public string Titulo => Texto("titulo");
        public string Rol => Texto("rol");
        public string Donde => Texto("donde");
        public string Resultado => Texto("resultado");
        public string Problema => GestorIdioma06AV.Instancia.Existe(Clave("problema")) ? Texto("problema") : null;
        public string[] Pasos => Texto("pasos").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static class DatosGuia06AV
    {
        public static readonly IReadOnlyList<PasoGuia06AV> Pn1 = new[]
        {
            new PasoGuia06AV("CU01", "pcf_menu_clientes"),
            new PasoGuia06AV("CU03", "pcf_menu_ventas"),
            new PasoGuia06AV("CU02", "pcf_menu_ventas"),
            new PasoGuia06AV("CU04", "pcf_menu_produccion"),
            new PasoGuia06AV("CU05", "pcf_menu_produccion"),
            new PasoGuia06AV("CU06", "pcf_menu_produccion"),
            new PasoGuia06AV("CU07", "pcf_menu_entregas")
        };

        public static readonly IReadOnlyList<PasoGuia06AV> Pn2 = new[]
        {
            new PasoGuia06AV("CU08", "pcf_menu_compras"),
            new PasoGuia06AV("CU09", "pcf_menu_compras"),
            new PasoGuia06AV("CU10", "pcf_menu_compras"),
            new PasoGuia06AV("CU11", "pcf_menu_compras"),
            new PasoGuia06AV("CU12", "pcf_menu_compras")
        };

        public static IEnumerable<PasoGuia06AV> DeModulo(string claveModulo) =>
            Pn1.Concat(Pn2).Where(p => string.Equals(p.ClaveModulo, claveModulo, StringComparison.OrdinalIgnoreCase));
    }
}
