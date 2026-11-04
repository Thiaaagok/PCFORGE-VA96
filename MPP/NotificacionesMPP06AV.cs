using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MPP
{
    public class NotificacionesMPP06AV
    {
        private const char SeparadorDatos = '|';
        private readonly NotificacionesDAL06AV _dal = new NotificacionesDAL06AV();

        public bool Agregar(Notificacion06AV n)
        {
            string datos = n.Datos == null || n.Datos.Length == 0
                ? null
                : string.Join(SeparadorDatos.ToString(), n.Datos.Select(d => (d ?? string.Empty).Replace(SeparadorDatos, '/')));

            return _dal.Insertar(new Dictionary<string, object>
            {
                { "@Tipo", (int)n.Tipo },
                { "@Datos", datos },
                { "@Patente", n.Patente },
                { "@Destino", n.Destino },
                { "@ClaveUnica", n.ClaveUnica },
                { "@DniOrigen", n.DniOrigen }
            });
        }

        public List<Notificacion06AV> ObtenerRecientes(string dni, int dias)
        {
            var lista = new List<Notificacion06AV>();
            foreach (DataRow row in _dal.ObtenerRecientes(dni, dias).Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public void MarcarLeidas(IEnumerable<int> ids, string dni) => _dal.MarcarLeidas(ids, dni);

        private static Notificacion06AV Mapear(DataRow row)
        {
            string datos = row["Datos"] as string;
            return new Notificacion06AV
            {
                Id = Convert.ToInt32(row["Id"]),
                Fecha = Convert.ToDateTime(row["Fecha"]),
                Tipo = (TipoNotificacion06AV)Convert.ToInt32(row["Tipo"]),
                Datos = string.IsNullOrEmpty(datos) ? new string[0] : datos.Split(SeparadorDatos),
                Patente = row["Patente"] as string,
                Destino = row["Destino"] as string,
                ClaveUnica = row["ClaveUnica"] as string,
                DniOrigen = row["DniOrigen"] as string,
                Leida = Convert.ToBoolean(row["Leida"])
            };
        }
    }
}
