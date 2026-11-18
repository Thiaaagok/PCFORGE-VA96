using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Instalador
{
    /// <summary>
    /// Prueba del Instalador. Trabaja sobre una base temporal (nombre + "_TestInstalador")
    /// y nunca toca la base real. Casos:
    ///   1. Instalación nueva: tablas requeridas, usuario admin y procedimientos clave.
    ///   2. Reparar sobre la base existente: los scripts son idempotentes (no duplican datos).
    ///   3. Reinstalar desde cero: hace backup previo y deja la base limpia.
    ///   4. Restaurar sobre reinstalación: el backup vuelve a dejar los datos previos.
    /// Al terminar borra la base temporal y, si el servidor es local, los .bak generados.
    /// </summary>
    public class InstaladorTest06AV
    {
        private const string TablaMarca = "PruebaInstalador06AV";


        private static readonly string[] ProcedimientosClave =
        {
            "sp_ObtenerPatentesUsuario", "sp_Componentes_ReservarStock"
        };

        private readonly OpcionesInstalacion06AV _base;
        private int _pasan;
        private int _fallan;

        public InstaladorTest06AV(OpcionesInstalacion06AV opcionesBase)
        {
            _base = opcionesBase ?? throw new ArgumentNullException(nameof(opcionesBase));
        }

        public bool Ejecutar(Action<string> log)
        {
            OpcionesInstalacion06AV opciones = _base.Clonar(_base.BaseDatos + "_TestInstalador");
            var dal = new InstaladorDAL06AV(opciones);
            var backups = new List<string>();

            log?.Invoke("=== PRUEBA DEL INSTALADOR ===");
            log?.Invoke($"Base de prueba: {opciones.BaseDatos}");

            try
            {
                List<ScriptSql06AV> scripts = InstaladorMPP06AV.ListarScripts(opciones.CarpetaScripts);
                Comprobar(log, scripts.Count > 0, $"Scripts a ejecutar: {scripts.Count}");
                Comprobar(log, scripts.All(s => !s.Nombre.StartsWith("99_")),
                    "Los scripts agregados/destructivos 99_* quedan excluidos");

                dal.EliminarBaseDatos();

                log?.Invoke("");
                log?.Invoke("--- Caso 1: instalación nueva ---");
                opciones.Modo = ModoInstalacion06AV.Instalar;
                ResultadoInstalacion06AV r1 = new InstaladorBLL06AV(opciones).Ejecutar(log);
                Comprobar(log, r1.TablasFaltantes.Count == 0,
                    "Tablas requeridas presentes" + Faltantes(r1));
                Comprobar(log, dal.ExisteUsuarioAdmin(), "Usuario 'admin' sembrado");
                foreach (string sp in ProcedimientosClave)
                    Comprobar(log, dal.ExisteProcedimiento(sp), $"Procedimiento {sp} creado");

                int usuarios = dal.ContarFilas("Usuarios");
                int patentes = dal.ContarFilas("Patentes");
                int componentes = dal.ContarFilas("Componentes");

                log?.Invoke("");
                log?.Invoke("--- Caso 2: reparar (re-ejecución idempotente) ---");
                opciones.Modo = ModoInstalacion06AV.Reparar;
                ResultadoInstalacion06AV r2 = new InstaladorBLL06AV(opciones).Ejecutar(log);
                Registrar(backups, r2.RutaBackupPrevio);
                Comprobar(log, r2.RutaBackupPrevio != null, "Backup previo generado");
                Comprobar(log, dal.ContarFilas("Usuarios") == usuarios
                               && dal.ContarFilas("Patentes") == patentes
                               && dal.ContarFilas("Componentes") == componentes,
                    "Los datos iniciales no se duplican al reparar");

                dal.EjecutarLote(
                    $"CREATE TABLE {TablaMarca} (Id INT NOT NULL PRIMARY KEY); " +
                    $"INSERT INTO {TablaMarca} (Id) VALUES (1);");
                string backupConMarca = dal.Respaldar("test");
                Registrar(backups, backupConMarca);

                log?.Invoke("");
                log?.Invoke("--- Caso 3: reinstalar desde cero ---");
                opciones.Modo = ModoInstalacion06AV.ReinstalarLimpio;
                ResultadoInstalacion06AV r3 = new InstaladorBLL06AV(opciones).Ejecutar(log);
                Registrar(backups, r3.RutaBackupPrevio);
                Comprobar(log, r3.RutaBackupPrevio != null, "Backup previo generado");
                Comprobar(log, !dal.ExisteTabla(TablaMarca), "La base quedó limpia (sin datos previos)");
                Comprobar(log, r3.TablasFaltantes.Count == 0, "Tablas requeridas presentes" + Faltantes(r3));

                log?.Invoke("");
                log?.Invoke("--- Caso 4: restaurar sobre reinstalación ---");
                opciones.Modo = ModoInstalacion06AV.Restaurar;
                opciones.ArchivoRestore = backupConMarca;
                ResultadoInstalacion06AV r4 = new InstaladorBLL06AV(opciones).Ejecutar(log);
                Registrar(backups, r4.RutaBackupPrevio);
                Comprobar(log, dal.ContarFilas(TablaMarca) == 1, "Los datos del backup volvieron");
                Comprobar(log, r4.TablasFaltantes.Count == 0, "Estructura actualizada tras el restore" + Faltantes(r4));
            }
            catch (Exception ex)
            {
                _fallan++;
                log?.Invoke("[FALLA] Excepción: " + ex.Message);
            }
            finally
            {
                try
                {
                    dal.EliminarBaseDatos();
                    log?.Invoke($"Base de prueba '{opciones.BaseDatos}' eliminada.");
                }
                catch (Exception ex)
                {
                    log?.Invoke("Advertencia: no se pudo eliminar la base de prueba: " + ex.Message);
                }

                foreach (string b in backups)
                {
                    try { if (File.Exists(b)) File.Delete(b); }
                    catch {  }
                }
            }

            bool exito = _fallan == 0 && _pasan > 0;
            log?.Invoke("");
            log?.Invoke($"Comprobaciones: {_pasan} pasan, {_fallan} fallan.");
            log?.Invoke(exito ? "RESULTADO: PRUEBA SUPERADA (OK)" : "RESULTADO: PRUEBA FALLIDA (X)");
            return exito;
        }

        private void Comprobar(Action<string> log, bool ok, string descripcion)
        {
            if (ok) _pasan++; else _fallan++;
            log?.Invoke($"[{(ok ? "PASA" : "FALLA")}] {descripcion}");
        }

        private static string Faltantes(ResultadoInstalacion06AV r) =>
            r.TablasFaltantes.Count == 0 ? "" : " (faltan: " + string.Join(", ", r.TablasFaltantes) + ")";

        private static void Registrar(List<string> lista, string ruta)
        {
            if (!string.IsNullOrEmpty(ruta)) lista.Add(ruta);
        }
    }
}
