using BE;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class NotificacionesBLL06AV
    {
        public const string DestinoVentas = "pcf_menu_ventas";
        public const string DestinoEntregas = "pcf_menu_entregas";
        public const string DestinoProduccion = "pcf_menu_produccion";
        public const string DestinoCompras = "pcf_menu_compras";
        public const string DestinoCotizaciones = "pcf_menu_cotizaciones";

        private const int DiasVisibles = 14;
        private const int Maximo = 40;

        private readonly NotificacionesMPP06AV _mpp = new NotificacionesMPP06AV();

        public List<Notificacion06AV> ObtenerParaUsuario()
        {
            var sesion = UsuarioSesion06AV.Instancia();
            string dni = sesion?.UsuarioActual?.Dni;
            if (dni == null) return new List<Notificacion06AV>();

            return _mpp.ObtenerRecientes(dni, DiasVisibles)
                       .Where(n => PuedeVer(sesion, n.Patente))
                       .Take(Maximo)
                       .ToList();
        }

        public void MarcarLeidas(IEnumerable<int> ids)
        {
            string dni = UsuarioSesion06AV.Instancia()?.UsuarioActual?.Dni;
            var lista = ids?.Distinct().ToList();
            if (dni == null || lista == null || lista.Count == 0) return;
            _mpp.MarcarLeidas(lista, dni);
        }

        private static bool PuedeVer(UsuarioSesion06AV sesion, string patente)
        {
            if (string.IsNullOrEmpty(patente)) return true;
            return Enum.TryParse(patente, out PatenteEnum06AV p) && sesion.TienePermiso(p);
        }

        // Igual que la auditoría: se llama después de grabar y nunca hace fallar la operación de negocio.
        internal static void Publicar(TipoNotificacion06AV tipo, PatenteEnum06AV destinatarios, string destino, params object[] datos)
        {
            string dni = null;
            try { dni = UsuarioSesion06AV.Instancia().UsuarioActual?.Dni; } catch { }
            Grabar(new Notificacion06AV
            {
                Tipo = tipo,
                Patente = destinatarios.ToString(),
                Destino = destino,
                DniOrigen = dni,
                Datos = Textos(datos)
            });
        }

        // Para situaciones detectadas (atrasos, stock bajo): la clave evita repetir el mismo aviso.
        internal static bool PublicarUnica(string clave, TipoNotificacion06AV tipo, PatenteEnum06AV destinatarios,
                                           string destino, params object[] datos)
        {
            return Grabar(new Notificacion06AV
            {
                Tipo = tipo,
                Patente = destinatarios.ToString(),
                Destino = destino,
                ClaveUnica = clave,
                Datos = Textos(datos)
            });
        }

        private static string[] Textos(object[] datos) =>
            (datos ?? new object[0]).Select(d => Convert.ToString(d, System.Globalization.CultureInfo.CurrentCulture)).ToArray();

        private static bool Grabar(Notificacion06AV n)
        {
            try { return new NotificacionesMPP06AV().Agregar(n); }
            catch { return false; }
        }
    }
}
