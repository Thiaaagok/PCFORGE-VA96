using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Instalador
{
    public class InstaladorDAL06AV
    {
        private readonly OpcionesInstalacion06AV _opciones;

        public InstaladorDAL06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        }

        // Instancias de SQL Server instaladas EN ESTE EQUIPO, leídas del registro (rápido y sin
        // red). No se usa SqlDataSourceEnumerator: tarda 15 s o más, depende del servicio SQL
        // Browser y bloqueaba la ventana. Un servidor remoto se puede escribir a mano.
        public static List<string> DetectarInstancias()
        {
            var lista = new List<string>();

            void Agregar(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) &&
                    !lista.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
                    lista.Add(s.Trim());
            }

            foreach (RegistryView vista in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, vista))
                    {
                        using (RegistryKey k = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server"))
                        {
                            if (k?.GetValue("InstalledInstances") is string[] instancias)
                                foreach (string nombre in instancias)
                                    Agregar(string.Equals(nombre, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                                        ? "." : ".\\" + nombre);
                        }
                        using (RegistryKey k = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions"))
                        {
                            if (k != null && k.GetSubKeyNames().Length > 0)
                                Agregar("(localdb)\\MSSQLLocalDB");
                        }
                    }
                }
                catch {  }
            }

            return lista;
        }

        // Prueba rápida (3 s) de si la instancia responde. Un login rechazado también cuenta como
        // "responde": el servidor existe, solo faltan credenciales válidas.
        public static bool InstanciaResponde(string servidor)
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = servidor,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ConnectTimeout = 3,
                Pooling = false
            };
            try
            {
                using (var conn = new SqlConnection(b.ConnectionString)) conn.Open();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 18456 || ex.Number == 4060 || ex.Number == 18452)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ProbarConexion()
        {
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            {
                conn.Open();
            }
        }

        public bool ExisteBaseDatos()
        {
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @nombre", conn))
            {
                cmd.Parameters.AddWithValue("@nombre", _opciones.BaseDatos);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public bool ExisteTabla(string tabla)
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @t", conn))
            {
                cmd.Parameters.AddWithValue("@t", tabla);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public bool ExisteProcedimiento(string nombre)
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.procedures WHERE name = @n", conn))
            {
                cmd.Parameters.AddWithValue("@n", nombre);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void CrearBaseDatos()
        {
            if (ExisteBaseDatos()) return;

            // El nombre de la base no admite parámetros: se valida y se escapa.
            string db = ValidarNombreBase(_opciones.BaseDatos);

            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand($"CREATE DATABASE [{db}]", conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void EliminarBaseDatos()
        {
            if (!ExisteBaseDatos()) return;

            string db = ValidarNombreBase(_opciones.BaseDatos);
            string sql =
                $"ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}];";

            SqlConnection.ClearAllPools();
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            SqlConnection.ClearAllPools();
        }

        public void EjecutarLote(string sqlBatch)
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            {
                conn.Open();
                foreach (string sub in SepararPorGo(sqlBatch))
                {
                    if (string.IsNullOrWhiteSpace(sub)) continue;
                    using (var cmd = new SqlCommand(sub, conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public int ContarTablas()
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int ContarUsuarios()
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand(
                "IF OBJECT_ID('Usuarios') IS NULL SELECT 0 ELSE SELECT COUNT(*) FROM Usuarios", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int ContarFilas(string tabla)
        {
            if (!Regex.IsMatch(tabla ?? "", @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException($"Nombre de tabla inválido: '{tabla}'.");

            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand(
                $"IF OBJECT_ID('{tabla}') IS NULL SELECT -1 ELSE SELECT COUNT(*) FROM [{tabla}]", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool ExisteUsuarioAdmin()
        {
            using (var conn = new SqlConnection(_opciones.CadenaBaseDatos()))
            using (var cmd = new SqlCommand(
                "IF OBJECT_ID('Usuarios') IS NULL SELECT 0 ELSE SELECT COUNT(*) FROM Usuarios WHERE Login = 'admin'", conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // ── Backup / Restore ─────────────────────────────────────────

        public string CarpetaBackupServidor()
        {
            return PropiedadServidor(
                "COALESCE(SERVERPROPERTY('InstanceDefaultBackupPath'), SERVERPROPERTY('InstanceDefaultDataPath'))");
        }

        private string CarpetaDatosServidor() => PropiedadServidor("SERVERPROPERTY('InstanceDefaultDataPath')");

        private string CarpetaLogServidor() => PropiedadServidor("SERVERPROPERTY('InstanceDefaultLogPath')");

        private string PropiedadServidor(string expresion)
        {
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand($"SELECT CONVERT(NVARCHAR(4000), {expresion})", conn))
            {
                conn.Open();
                object r = cmd.ExecuteScalar();
                return r == null || r == DBNull.Value ? null : (string)r;
            }
        }

        // Backup COPY_ONLY: no altera la cadena de backups que lleve el administrador.
        public string Respaldar(string sufijo)
        {
            string db = ValidarNombreBase(_opciones.BaseDatos);
            string carpeta = CarpetaBackupServidor();
            if (string.IsNullOrWhiteSpace(carpeta))
                throw new InvalidOperationException("No se pudo determinar la carpeta de backups del servidor.");

            string archivo = $"{db}_{sufijo}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string ruta = carpeta.TrimEnd('\\') + "\\" + archivo;

            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            using (var cmd = new SqlCommand(
                $"BACKUP DATABASE [{db}] TO DISK = @ruta WITH COPY_ONLY, INIT, FORMAT, NAME = @nombre", conn))
            {
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.Parameters.AddWithValue("@nombre", $"{db} - respaldo del instalador ({sufijo})");
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return ruta;
        }

        public void Restaurar(string rutaBak)
        {
            if (string.IsNullOrWhiteSpace(rutaBak))
                throw new ArgumentException("No se indicó el archivo de backup a restaurar.");

            string db = ValidarNombreBase(_opciones.BaseDatos);
            bool existia = ExisteBaseDatos();

            SqlConnection.ClearAllPools();
            using (var conn = new SqlConnection(_opciones.CadenaMaster()))
            {
                conn.Open();

                List<ArchivoBackup> archivos = LeerArchivosBackup(conn, rutaBak);
                List<string> destinos = CalcularDestinos(conn, db, archivos, existia);

                var sql = new StringBuilder($"RESTORE DATABASE [{db}] FROM DISK = @ruta WITH REPLACE, RECOVERY");
                for (int i = 0; i < archivos.Count; i++)
                    sql.Append($", MOVE @l{i} TO @p{i}");

                if (existia)
                    EjecutarMaster(conn, $"ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                try
                {
                    using (var cmd = new SqlCommand(sql.ToString(), conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("@ruta", rutaBak);
                        for (int i = 0; i < archivos.Count; i++)
                        {
                            cmd.Parameters.AddWithValue("@l" + i, archivos[i].Logico);
                            cmd.Parameters.AddWithValue("@p" + i, destinos[i]);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex) when (ex.Number == 3201)
                {
                    throw new InvalidOperationException(
                        "SQL Server no pudo abrir el archivo de backup. El archivo lo lee el servicio de " +
                        "SQL Server, no el instalador: copialo a la carpeta de backups del servidor (" +
                        (CarpetaBackupServidor() ?? "?") + ") y volvé a intentar.", ex);
                }
                finally
                {
                    try { EjecutarMaster(conn, $"IF DB_ID('{db}') IS NOT NULL ALTER DATABASE [{db}] SET MULTI_USER"); }
                    catch {  }
                }
            }
            SqlConnection.ClearAllPools();
        }

        private sealed class ArchivoBackup
        {
            public string Logico;
            public string Fisico;
            public string Tipo;
        }

        private static List<ArchivoBackup> LeerArchivosBackup(SqlConnection conn, string rutaBak)
        {
            var lista = new List<ArchivoBackup>();
            try
            {
                using (var cmd = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @ruta", conn))
                {
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddWithValue("@ruta", rutaBak);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            lista.Add(new ArchivoBackup
                            {
                                Logico = r["LogicalName"].ToString(),
                                Fisico = r["PhysicalName"].ToString(),
                                Tipo = r["Type"].ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 3201)
            {
                throw new InvalidOperationException(
                    "SQL Server no pudo abrir el archivo de backup '" + rutaBak + "'. El archivo lo lee " +
                    "el servicio de SQL Server: copialo a una carpeta accesible para el servicio " +
                    "(por ejemplo la carpeta de backups del servidor) y volvé a intentar.", ex);
            }

            if (lista.Count == 0)
                throw new InvalidOperationException("El archivo de backup no contiene archivos de base de datos.");
            return lista;
        }

        // Si la base ya existe se reutilizan sus archivos físicos (mismo orden por tipo);
        // si no, se crean en las carpetas por defecto de la instancia.
        private List<string> CalcularDestinos(SqlConnection conn, string db, List<ArchivoBackup> archivos, bool existia)
        {
            var actualesDatos = new List<string>();
            var actualesLog = new List<string>();
            if (existia)
            {
                using (var cmd = new SqlCommand(
                    "SELECT type_desc, physical_name FROM sys.master_files WHERE database_id = DB_ID(@db) ORDER BY file_id", conn))
                {
                    cmd.Parameters.AddWithValue("@db", db);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if (r.GetString(0) == "LOG") actualesLog.Add(r.GetString(1));
                            else if (r.GetString(0) == "ROWS") actualesDatos.Add(r.GetString(1));
                        }
                    }
                }
            }

            int cantDatos = archivos.Count(a => a.Tipo == "D");
            int cantLog = archivos.Count(a => a.Tipo == "L");
            bool reutilizar = existia && cantDatos == actualesDatos.Count && cantLog == actualesLog.Count
                              && archivos.All(a => a.Tipo == "D" || a.Tipo == "L");

            string carpetaDatos = (CarpetaDatosServidor() ?? "").TrimEnd('\\');
            string carpetaLog = (CarpetaLogServidor() ?? carpetaDatos).TrimEnd('\\');

            var destinos = new List<string>();
            int iDatos = 0, iLog = 0;
            foreach (ArchivoBackup a in archivos)
            {
                if (reutilizar)
                {
                    destinos.Add(a.Tipo == "L" ? actualesLog[iLog++] : actualesDatos[iDatos++]);
                    continue;
                }

                string ext = Path.GetExtension(a.Fisico);
                if (a.Tipo == "L")
                {
                    destinos.Add($"{carpetaLog}\\{db}_log{(iLog == 0 ? "" : "_" + iLog)}{(string.IsNullOrEmpty(ext) ? ".ldf" : ext)}");
                    iLog++;
                }
                else if (a.Tipo == "D")
                {
                    destinos.Add($"{carpetaDatos}\\{db}{(iDatos == 0 ? "" : "_" + iDatos)}{(string.IsNullOrEmpty(ext) ? ".mdf" : ext)}");
                    iDatos++;
                }
                else
                {
                    destinos.Add($"{carpetaDatos}\\{db}_{a.Logico}");
                }
            }
            return destinos;
        }

        private static void EjecutarMaster(SqlConnection conn, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
        }

        private static string[] SepararPorGo(string sql)
        {
            return Regex.Split(sql, @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private static string ValidarNombreBase(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre) || !Regex.IsMatch(nombre, @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException($"Nombre de base de datos inválido: '{nombre}'.");
            return nombre;
        }
    }
}
