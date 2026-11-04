using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class NotificacionesDAL06AV
    {
        private static bool _estructuraVerificada;

        // Misma definición que 41_notificaciones.sql: la tabla se crea sola si la base no tiene el script.
        private const string SqlEstructura = @"
            IF OBJECT_ID('Notificaciones') IS NULL
            CREATE TABLE Notificaciones (
                Id         INT IDENTITY(1,1) PRIMARY KEY,
                Fecha      DATETIME      NOT NULL DEFAULT GETDATE(),
                Tipo       INT           NOT NULL,
                Datos      NVARCHAR(400) NULL,
                Patente    NVARCHAR(100) NULL,
                Destino    NVARCHAR(80)  NULL,
                ClaveUnica NVARCHAR(150) NULL,
                DniOrigen  NVARCHAR(20)  NULL
            );
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Notificaciones_ClaveUnica')
                CREATE UNIQUE INDEX UX_Notificaciones_ClaveUnica ON Notificaciones(ClaveUnica) WHERE ClaveUnica IS NOT NULL;
            IF OBJECT_ID('NotificacionesLeidas') IS NULL
            CREATE TABLE NotificacionesLeidas (
                IdNotificacion INT          NOT NULL REFERENCES Notificaciones(Id) ON DELETE CASCADE,
                Dni            NVARCHAR(20) NOT NULL,
                FechaLectura   DATETIME     NOT NULL DEFAULT GETDATE(),
                PRIMARY KEY (IdNotificacion, Dni)
            );";

        private static void AsegurarEstructura(SqlConnection conn)
        {
            if (_estructuraVerificada) return;
            using (var cmd = new SqlCommand(SqlEstructura, conn)) cmd.ExecuteNonQuery();
            _estructuraVerificada = true;
        }

        private static T Ejecutar<T>(Func<SqlConnection, T> accion)
        {
            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            {
                conn.Open();
                AsegurarEstructura(conn);
                return accion(conn);
            }
        }

        public bool Insertar(Dictionary<string, object> parametros)
        {
            const string sql = @"
                IF @ClaveUnica IS NULL OR NOT EXISTS (SELECT 1 FROM Notificaciones WHERE ClaveUnica = @ClaveUnica)
                BEGIN
                    INSERT INTO Notificaciones (Fecha, Tipo, Datos, Patente, Destino, ClaveUnica, DniOrigen)
                    VALUES (GETDATE(), @Tipo, @Datos, @Patente, @Destino, @ClaveUnica, @DniOrigen);
                    SELECT 1;
                END
                ELSE SELECT 0;";

            return Ejecutar(conn =>
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            });
        }

        public DataTable ObtenerRecientes(string dni, int dias)
        {
            const string sql = @"
                SELECT n.Id, n.Fecha, n.Tipo, n.Datos, n.Patente, n.Destino, n.ClaveUnica, n.DniOrigen,
                       CAST(CASE WHEN l.IdNotificacion IS NULL THEN 0 ELSE 1 END AS BIT) AS Leida
                FROM Notificaciones n
                LEFT JOIN NotificacionesLeidas l ON l.IdNotificacion = n.Id AND l.Dni = @Dni
                WHERE n.Fecha >= DATEADD(DAY, -@Dias, GETDATE())
                  AND (n.DniOrigen IS NULL OR n.DniOrigen <> @Dni)
                ORDER BY n.Fecha DESC, n.Id DESC;";

            return Ejecutar(conn =>
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Dni", dni);
                    cmd.Parameters.AddWithValue("@Dias", dias);
                    var tabla = new DataTable();
                    new SqlDataAdapter(cmd).Fill(tabla);
                    return tabla;
                }
            });
        }

        public void MarcarLeidas(IEnumerable<int> ids, string dni)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM NotificacionesLeidas WHERE IdNotificacion = @Id AND Dni = @Dni)
                    INSERT INTO NotificacionesLeidas (IdNotificacion, Dni, FechaLectura) VALUES (@Id, @Dni, GETDATE());";

            Ejecutar(conn =>
            {
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (int id in ids)
                            using (var cmd = new SqlCommand(sql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@Dni", dni);
                                cmd.ExecuteNonQuery();
                            }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
                return 0;
            });
        }
    }
}
