using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Instalador
{
    // Un script SQL a ejecutar: nombre del archivo + lectura de su contenido.
    public sealed class ScriptSql06AV
    {
        public string Nombre { get; }
        private readonly Func<string> _leer;

        public ScriptSql06AV(string nombre, Func<string> leer)
        {
            Nombre = nombre;
            _leer = leer;
        }

        public string LeerContenido() => _leer();
    }

    public class InstaladorMPP06AV
    {
        // Prefijo del nombre lógico de los recursos embebidos en el .exe (ver Instalador.csproj).
        private const string PrefijoRecurso = "Scripts.";

        // Solo scripts numerados "NN_*.sql". Los "99_*" son agregados armados a mano
        // (duplican al resto) y 99_recrear_todo_desde_cero.sql hace DROP DATABASE:
        // nunca se ejecutan desde el instalador.
        private static readonly Regex PatronScript = new Regex(@"^\d{2}_.+\.sql$", RegexOptions.IgnoreCase);

        private readonly OpcionesInstalacion06AV _opciones;
        private readonly InstaladorDAL06AV _dal;

        public InstaladorMPP06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            _dal = new InstaladorDAL06AV(opciones);
        }

        public void PrepararBaseDatos(Action<string> log)
        {
            log?.Invoke($"Verificando conexión al servidor '{_opciones.Servidor}'...");
            _dal.ProbarConexion();
            log?.Invoke("Conexión establecida.");

            if (_dal.ExisteBaseDatos())
            {
                log?.Invoke($"La base '{_opciones.BaseDatos}' ya existe: se actualizará sobre la existente.");
            }
            else
            {
                log?.Invoke($"Creando base de datos '{_opciones.BaseDatos}'...");
                _dal.CrearBaseDatos();
                log?.Invoke("Base de datos creada.");
            }
        }

        // carpeta vacía/nula => scripts embebidos en el .exe (caso normal: el instalador viaja solo).
        // carpeta informada (--scripts <ruta>) => se leen los .sql de esa carpeta.
        public static List<ScriptSql06AV> ListarScripts(string carpeta)
        {
            IEnumerable<ScriptSql06AV> origen;
            if (string.IsNullOrWhiteSpace(carpeta))
            {
                origen = ListarEmbebidos();
            }
            else
            {
                if (!Directory.Exists(carpeta))
                    throw new DirectoryNotFoundException(
                        $"No se encontró la carpeta de scripts: '{carpeta}'.");

                origen = Directory.GetFiles(carpeta, "*.sql")
                    .Select(f => new ScriptSql06AV(Path.GetFileName(f), () => File.ReadAllText(f)));
            }

            return origen
                .Where(s => PatronScript.IsMatch(s.Nombre))
                .Where(s => !s.Nombre.StartsWith("99_", StringComparison.Ordinal))
                .OrderBy(s => s.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<ScriptSql06AV> ListarEmbebidos()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string recurso in asm.GetManifestResourceNames())
            {
                if (!recurso.StartsWith(PrefijoRecurso, StringComparison.OrdinalIgnoreCase) ||
                    !recurso.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    continue;

                string nombreRecurso = recurso;
                yield return new ScriptSql06AV(
                    nombreRecurso.Substring(PrefijoRecurso.Length),
                    () => LeerRecurso(asm, nombreRecurso));
            }
        }

        // Misma lectura que File.ReadAllText: UTF-8, respetando el BOM si lo hay.
        private static string LeerRecurso(Assembly asm, string nombreRecurso)
        {
            using (Stream s = asm.GetManifestResourceStream(nombreRecurso))
            using (var r = new StreamReader(s, new UTF8Encoding(false), true))
                return r.ReadToEnd();
        }

        public int EjecutarScripts(Action<string> log)
        {
            List<ScriptSql06AV> scripts = ListarScripts(_opciones.CarpetaScripts);

            if (scripts.Count == 0)
                throw new FileNotFoundException(string.IsNullOrWhiteSpace(_opciones.CarpetaScripts)
                    ? "El instalador no tiene scripts .sql embebidos."
                    : $"No hay scripts .sql en la carpeta: '{_opciones.CarpetaScripts}'.");

            foreach (ScriptSql06AV script in scripts)
            {
                log?.Invoke($"Ejecutando {script.Nombre}...");
                string sql = script.LeerContenido();
                try
                {
                    _dal.EjecutarLote(sql);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Falló el script {script.Nombre}: {ex.Message}", ex);
                }
            }

            return scripts.Count;
        }
    }
}