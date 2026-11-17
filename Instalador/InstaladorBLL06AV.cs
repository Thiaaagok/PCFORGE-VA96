using System;
using System.Collections.Generic;
using System.Linq;

namespace Instalador
{
    public class ResultadoInstalacion06AV
    {
        public int CantidadTablas { get; set; }
        public int CantidadUsuarios { get; set; }
        public List<string> TablasFaltantes { get; set; } = new List<string>();
        public string RutaBackupPrevio { get; set; }
        public bool Correcto => CantidadTablas > 0 && CantidadUsuarios > 0 && TablasFaltantes.Count == 0;
    }

    public class InstaladorBLL06AV
    {
        // Tablas que tiene que dejar cualquier instalación completa (una por módulo).
        public static readonly string[] TablasRequeridas =
        {
            "Usuarios", "Roles", "Patentes", "Familias", "Bitacora",
            "Clientes", "Componentes", "Componentes_C", "Proveedores", "LineasEnsamblaje",
            "Ventas", "Computadoras", "OrdenesProduccion", "Pagos",
            "OrdenesCompra", "PedidosCotizacion", "FacturaCompra", "PagosProveedor",
            "ModelosEstandar", "ComponenteEspecificaciones", "ReglasCompatibilidad",
            "Notificaciones"
        };

        private readonly OpcionesInstalacion06AV _opciones;
        private readonly InstaladorMPP06AV _mpp;
        private readonly InstaladorDAL06AV _dal;

        public InstaladorBLL06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            _mpp = new InstaladorMPP06AV(opciones);
            _dal = new InstaladorDAL06AV(opciones);
        }

        public bool ExisteBaseDatos() => _dal.ExisteBaseDatos();

        public string Describir(ModoInstalacion06AV modo)
        {
            switch (modo)
            {
                case ModoInstalacion06AV.Reparar: return "Reparar / actualizar";
                case ModoInstalacion06AV.ReinstalarLimpio: return "Reinstalar desde cero";
                case ModoInstalacion06AV.Restaurar: return "Restaurar desde backup";
                default: return "Instalación nueva";
            }
        }

        public ResultadoInstalacion06AV Ejecutar(Action<string> log)
        {
            switch (_opciones.Modo)
            {
                case ModoInstalacion06AV.Reparar: return Reparar(log);
                case ModoInstalacion06AV.ReinstalarLimpio: return ReinstalarLimpio(log);
                case ModoInstalacion06AV.Restaurar: return Restaurar(log);
                default: return Instalar(log);
            }
        }

        public ResultadoInstalacion06AV Instalar(Action<string> log)
        {
            log?.Invoke("=== INSTALACIÓN DEL SISTEMA PCFORGE-ValdezThiago-96VA ===");
            _mpp.PrepararBaseDatos(log);
            return EjecutarScriptsYVerificar(log, null);
        }

        // Re-ejecuta los scripts (todos idempotentes) sobre la base existente: crea lo que
        // falte y actualiza procedimientos sin tocar los datos de negocio.
        public ResultadoInstalacion06AV Reparar(Action<string> log)
        {
            log?.Invoke("=== REINSTALACIÓN: REPARAR / ACTUALIZAR ===");
            string backup = RespaldoPrevio(log, "prereparacion");
            _mpp.PrepararBaseDatos(log);
            return EjecutarScriptsYVerificar(log, backup);
        }

        public ResultadoInstalacion06AV ReinstalarLimpio(Action<string> log)
        {
            log?.Invoke("=== REINSTALACIÓN DESDE CERO ===");
            string backup = RespaldoPrevio(log, "prereinstalacion");

            log?.Invoke($"Eliminando la base '{_opciones.BaseDatos}'...");
            _dal.EliminarBaseDatos();
            log?.Invoke("Base eliminada.");

            _mpp.PrepararBaseDatos(log);
            return EjecutarScriptsYVerificar(log, backup);
        }

        // Restore sobre reinstalación: se restaura el .bak y después se corren los scripts,
        // así un backup de una versión anterior queda con la estructura de la versión actual.
        public ResultadoInstalacion06AV Restaurar(Action<string> log)
        {
            log?.Invoke("=== REINSTALACIÓN: RESTAURAR DESDE BACKUP ===");
            if (string.IsNullOrWhiteSpace(_opciones.ArchivoRestore))
                throw new InvalidOperationException("No se indicó el archivo .bak a restaurar.");

            log?.Invoke($"Verificando conexión al servidor '{_opciones.Servidor}'...");
            _dal.ProbarConexion();

            string backup = RespaldoPrevio(log, "prerestore");

            log?.Invoke($"Restaurando '{_opciones.ArchivoRestore}'...");
            _dal.Restaurar(_opciones.ArchivoRestore);
            log?.Invoke("Backup restaurado.");

            log?.Invoke("Actualizando la estructura a la versión actual...");
            return EjecutarScriptsYVerificar(log, backup);
        }

        private string RespaldoPrevio(Action<string> log, string sufijo)
        {
            if (!_dal.ExisteBaseDatos())
            {
                log?.Invoke("No hay base previa: no hace falta respaldo.");
                return null;
            }

            log?.Invoke("Respaldando la base actual antes de modificarla...");
            string ruta = _dal.Respaldar(sufijo);
            log?.Invoke($"Respaldo creado en el servidor: {ruta}");
            return ruta;
        }

        private ResultadoInstalacion06AV EjecutarScriptsYVerificar(Action<string> log, string backup)
        {
            log?.Invoke("Preparando estructura y datos iniciales...");
            int scripts = _mpp.EjecutarScripts(log);
            log?.Invoke($"Se ejecutaron {scripts} script(s) correctamente.");

            ResultadoInstalacion06AV resultado = Verificar();
            resultado.RutaBackupPrevio = backup;
            log?.Invoke(
                $"Verificación: {resultado.CantidadTablas} tabla(s), " +
                $"{resultado.CantidadUsuarios} usuario(s).");

            if (resultado.TablasFaltantes.Count > 0)
                log?.Invoke("Faltan tablas: " + string.Join(", ", resultado.TablasFaltantes));

            if (!resultado.Correcto)
            {
                string msg = "La verificación falló: la base no quedó completa.";
                if (backup != null) msg += " La base anterior quedó respaldada en: " + backup;
                throw new InvalidOperationException(msg);
            }

            log?.Invoke("La línea base del dígito verificador se recalcula en el próximo ingreso al sistema.");
            log?.Invoke("Operación finalizada con éxito.");
            return resultado;
        }

        public ResultadoInstalacion06AV Verificar()
        {
            return new ResultadoInstalacion06AV
            {
                CantidadTablas = _dal.ContarTablas(),
                CantidadUsuarios = _dal.ContarUsuarios(),
                TablasFaltantes = TablasRequeridas.Where(t => !_dal.ExisteTabla(t)).ToList()
            };
        }
    }
}
