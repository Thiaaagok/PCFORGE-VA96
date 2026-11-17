using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Instalador
{
    public class InstaladorMPP06AV
    {
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

        public static List<string> ListarScripts(string carpeta)
        {
            if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta))
                throw new DirectoryNotFoundException(
                    $"No se encontró la carpeta de scripts: '{carpeta}'.");

            return Directory
                .GetFiles(carpeta, "*.sql")
                .Where(f => PatronScript.IsMatch(Path.GetFileName(f)))
                .Where(f => !Path.GetFileName(f).StartsWith("99_", StringComparison.Ordinal))
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public int EjecutarScripts(Action<string> log)
        {
            List<string> archivos = ListarScripts(_opciones.CarpetaScripts);

            if (archivos.Count == 0)
                throw new FileNotFoundException(
                    $"No hay scripts .sql en la carpeta: '{_opciones.CarpetaScripts}'.");

            foreach (string archivo in archivos)
            {
                log?.Invoke($"Ejecutando {Path.GetFileName(archivo)}...");
                string sql = File.ReadAllText(archivo);
                try
                {
                    _dal.EjecutarLote(sql);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Falló el script {Path.GetFileName(archivo)}: {ex.Message}", ex);
                }
            }

            return archivos.Count;
        }
    }
}
