using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Instalador
{
    internal static class Program
    {
        private const string NombreMutex = @"Local\PCFORGE_ValdezThiago_96VA_Instalador";

        [STAThread]
        private static int Main(string[] args)
        {
            bool consola = RequiereConsola(args);

            // Una sola instancia: si ya hay un instalador abierto se lo trae al frente y se sale.
            using (var mutex = new Mutex(true, NombreMutex, out bool soyElPrimero))
            {
                if (!soyElPrimero)
                {
                    if (!consola && !TraerInstanciaAlFrente())
                        MessageBox.Show("El instalador ya está abierto en otra ventana.", "PCFORGE",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 3;
                }

                if (consola)
                    return EjecutarConsola(args);

                if (TieneFlag(args, "--desinstalar"))
                    return Desinstalar();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => MostrarErrorInesperado(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => MostrarErrorInesperado(e.ExceptionObject as Exception);

                try
                {
                    return EjecutarAsistente(args);
                }
                catch (Exception ex)
                {
                    MostrarErrorInesperado(ex);
                    return 1;
                }
            }
        }

        private static int Desinstalar()
        {
            if (MessageBox.Show("Se va a desinstalar PCFORGE de este equipo.\n\n" +
                                "La base de datos NO se borra (se puede volver a usar al reinstalar).\n\n¿Continuar?",
                    "Desinstalar PCFORGE", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return 2;
            try
            {
                InstaladorSistema06AV.Desinstalar();
                MessageBox.Show("PCFORGE se desinstaló correctamente.", "Desinstalar PCFORGE",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo desinstalar:\n\n" + ex.Message, "Desinstalar PCFORGE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void MostrarErrorInesperado(Exception ex)
        {
            MessageBox.Show("Ocurrió un error inesperado en el instalador:\n\n" + (ex?.Message ?? "(sin detalle)") +
                            "\n\nNo se hizo ningún cambio que no figure en el registro de la instalación.",
                "PCFORGE", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool TraerInstanciaAlFrente()
        {
            try
            {
                Process actual = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(actual.ProcessName))
                {
                    if (p.Id == actual.Id || p.MainWindowHandle == IntPtr.Zero) continue;
                    ShowWindow(p.MainWindowHandle, SW_RESTORE);
                    return SetForegroundWindow(p.MainWindowHandle);
                }
            }
            catch {  }
            return false;
        }

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static bool RequiereConsola(string[] args)
        {
            foreach (string a in args)
            {
                switch (a.ToLowerInvariant())
                {
                    case "--test":
                    case "--silent":
                    case "--help":
                    case "-h":
                    case "--servidor":
                    case "--bd":
                    case "--usuario":
                    case "--password":
                    case "--scripts":
                    case "--modo":
                    case "--backup":
                    case "--carpeta":
                    case "--solo-base":
                    case "--consola":
                        return true;
                }
            }
            return false;
        }

        private static int EjecutarAsistente(string[] args)
        {
            OpcionesInstalacion06AV opciones;
            bool baseExiste;
            int tablas, usuarios;
            string carpetaBackups;
            using (var conexion = new FrmConexion06AV())
            {
                if (conexion.ShowDialog() != DialogResult.OK)
                    return 2;
                opciones = conexion.Opciones;
                baseExiste = conexion.BaseExiste;
                tablas = conexion.TablasExistentes;
                usuarios = conexion.UsuariosExistentes;
                carpetaBackups = conexion.CarpetaBackups;
            }

            if (baseExiste)
            {
                using (var modo = new FrmModo06AV(opciones, tablas, usuarios, carpetaBackups))
                {
                    if (modo.ShowDialog() != DialogResult.OK)
                        return 2;
                }
            }
            else
            {
                opciones.Modo = ModoInstalacion06AV.Instalar;
            }

            using (var progreso = new FrmProgreso06AV(opciones))
            {
                progreso.ShowDialog();
                return progreso.Exito ? 0 : 1;
            }
        }

        private static int EjecutarConsola(string[] args)
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                AllocConsole();

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "PCFORGE - Instalación del sistema";

            bool silent = TieneFlag(args, "--silent");
            try
            {
                if (TieneFlag(args, "--help") || TieneFlag(args, "-h"))
                {
                    MostrarAyuda();
                    return 0;
                }

                var opciones = ConstruirOpciones(args);
                bool esTest = TieneFlag(args, "--test");

                Console.WriteLine("======================================================");
                Console.WriteLine("  INSTALADOR - Sistema PCFORGE-ValdezThiago-96VA");
                Console.WriteLine("======================================================");
                Console.WriteLine($"  Servidor : {opciones.Servidor}");
                Console.WriteLine($"  Base     : {opciones.BaseDatos}{(esTest ? " (modo prueba)" : "")}");
                Console.WriteLine($"  Auth     : {(opciones.SeguridadIntegrada ? "Windows" : "SQL (" + opciones.Usuario + ")")}");
                Console.WriteLine($"  Scripts  : {(string.IsNullOrWhiteSpace(opciones.CarpetaScripts) ? "(embebidos en el instalador)" : opciones.CarpetaScripts)}");
                if (!esTest)
                {
                    Console.WriteLine($"  Modo     : {DescribirModo(opciones)}");
                    Console.WriteLine($"  Sistema  : {(opciones.InstalarSistema ? opciones.CarpetaInstalacion : "(no se copian archivos: --solo-base)")}");
                }
                Console.WriteLine("======================================================");
                Console.WriteLine();

                if (!silent && !Confirmar("¿Continuar con la operación? (S/N): "))
                {
                    Console.WriteLine("Operación cancelada por el usuario.");
                    return 2;
                }

                if (esTest)
                {
                    bool ok = new InstaladorTest06AV(opciones).Ejecutar(Log);
                    return ok ? 0 : 1;
                }

                string exe = InstaladorSistema06AV.RutaExe(opciones.CarpetaInstalacion);
                if (opciones.InstalarSistema)
                    exe = InstaladorSistema06AV.Instalar(opciones.CarpetaInstalacion, Log);

                ResultadoInstalacion06AV resultado = new InstaladorBLL06AV(opciones).Ejecutar(Log);
                if (resultado.RutaBackupPrevio != null)
                    Console.WriteLine($"Backup previo: {resultado.RutaBackupPrevio}");

                if (File.Exists(exe))
                {
                    Console.WriteLine(ConfiguradorApp06AV.EscribirCadenaConexion(exe, opciones.CadenaBaseDatos())
                        ? "Conexión del sistema configurada."
                        : "Aviso: no se pudo escribir la configuración del sistema.");
                    if (opciones.InstalarSistema)
                    {
                        InstaladorSistema06AV.CrearAccesosMenuInicio(opciones.CarpetaInstalacion, Log);
                        InstaladorSistema06AV.RegistrarDesinstalacion(opciones.CarpetaInstalacion, Log);
                        if (InstaladorSistema06AV.CrearAccesoEscritorio(opciones.CarpetaInstalacion, out _) != null)
                            Console.WriteLine("Acceso directo creado en el Escritorio.");
                    }
                }
                else
                {
                    Console.WriteLine("Aviso: el sistema no está instalado en " + opciones.CarpetaInstalacion + ".");
                }

                Console.WriteLine();
                Console.WriteLine("Operación completada.");
                if (opciones.Modo == ModoInstalacion06AV.Instalar || opciones.Modo == ModoInstalacion06AV.ReinstalarLimpio)
                {
                    Console.WriteLine("Credenciales iniciales:");
                    Console.WriteLine("   Login: admin   Contraseña: Admin1234");
                    Console.WriteLine("   (el sistema pedirá cambiarla en el primer ingreso)");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("ERROR: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
            finally
            {
                if (!silent && EsInteractivo())
                {
                    Console.WriteLine();
                    Console.WriteLine("Presioná una tecla para salir...");
                    Console.ReadKey();
                }
            }
        }

        private static OpcionesInstalacion06AV ConstruirOpciones(string[] args)
        {
            var o = new OpcionesInstalacion06AV
            {
                Servidor = ValorArg(args, "--servidor") ?? ".",
                BaseDatos = ValorArg(args, "--bd") ?? "IngSoftValdezAlegre",
                // Sin --scripts se usan los .sql embebidos en el .exe.
                CarpetaScripts = ValorArg(args, "--scripts"),
                InstalarSistema = !TieneFlag(args, "--test") && !TieneFlag(args, "--solo-base")
            };
            string carpeta = ValorArg(args, "--carpeta");
            if (!string.IsNullOrWhiteSpace(carpeta)) o.CarpetaInstalacion = carpeta;

            string usuario = ValorArg(args, "--usuario");
            if (!string.IsNullOrEmpty(usuario))
            {
                o.SeguridadIntegrada = false;
                o.Usuario = usuario;
                o.Contrasenia = ValorArg(args, "--password") ?? "";
            }

            if (TieneFlag(args, "--test"))
                return o;

            string modo = (ValorArg(args, "--modo") ?? "").Trim().ToLowerInvariant();
            switch (modo)
            {
                case "":
                    // Sin --modo: instalación nueva o, si la base ya existe, reparar.
                    o.Modo = new InstaladorDAL06AV(o).ExisteBaseDatos()
                        ? ModoInstalacion06AV.Reparar
                        : ModoInstalacion06AV.Instalar;
                    break;
                case "instalar": o.Modo = ModoInstalacion06AV.Instalar; break;
                case "reparar": o.Modo = ModoInstalacion06AV.Reparar; break;
                case "reinstalar": o.Modo = ModoInstalacion06AV.ReinstalarLimpio; break;
                case "restaurar":
                    o.Modo = ModoInstalacion06AV.Restaurar;
                    o.ArchivoRestore = ValorArg(args, "--backup");
                    if (string.IsNullOrWhiteSpace(o.ArchivoRestore))
                        throw new ArgumentException("--modo restaurar requiere --backup <ruta.bak>.");
                    break;
                default:
                    throw new ArgumentException($"Modo desconocido: '{modo}'. Use instalar, reparar, reinstalar o restaurar.");
            }

            return o;
        }

        private static void Log(string mensaje) => Console.WriteLine(mensaje);

        private static string DescribirModo(OpcionesInstalacion06AV o)
        {
            string d = new InstaladorBLL06AV(o).Describir(o.Modo);
            return o.Modo == ModoInstalacion06AV.Restaurar ? d + " (" + o.ArchivoRestore + ")" : d;
        }

        private static bool Confirmar(string prompt)
        {
            Console.Write(prompt);
            string r = Console.ReadLine();
            return r != null && (r.Trim().Equals("S", StringComparison.OrdinalIgnoreCase)
                              || r.Trim().Equals("SI", StringComparison.OrdinalIgnoreCase)
                              || r.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase));
        }

        private static bool EsInteractivo()
        {
            try { return !Console.IsInputRedirected; }
            catch { return false; }
        }

        private static void MostrarAyuda()
        {
            Console.WriteLine("PCFORGE.exe - instalación del sistema PCFORGE-ValdezThiago-96VA");
            Console.WriteLine();
            Console.WriteLine("  (sin parámetros)    Abre el asistente gráfico de instalación.");
            Console.WriteLine("  --servidor <inst>   Instancia de SQL Server (por defecto '.').");
            Console.WriteLine("  --bd <nombre>       Nombre de la base (por defecto IngSoftValdezAlegre).");
            Console.WriteLine("  --scripts <ruta>    Carpeta con los .sql (por defecto se usan los embebidos en el exe).");
            Console.WriteLine("  --usuario <user>    Usa SQL Auth con ese usuario (si no, Windows Auth).");
            Console.WriteLine("  --password <pass>   Contraseña para SQL Auth.");
            Console.WriteLine("  --modo <modo>       instalar | reparar | reinstalar | restaurar.");
            Console.WriteLine("                      Sin --modo: instala, o repara si la base ya existe.");
            Console.WriteLine("                      reparar/reinstalar/restaurar hacen backup previo automático.");
            Console.WriteLine("  --backup <ruta.bak> Backup a restaurar (con --modo restaurar). Lo lee el servicio de SQL Server.");
            Console.WriteLine("  --carpeta <ruta>    Carpeta donde se instala el sistema (por defecto Archivos de programa\\PCFORGE).");
            Console.WriteLine("  --solo-base         Solo prepara la base; no copia los archivos del sistema.");
            Console.WriteLine("  --desinstalar       Quita el sistema del equipo (la base de datos se conserva).");
            Console.WriteLine("  --test              Corre la prueba del instalador sobre una base temporal.");
            Console.WriteLine("  --silent            No pide confirmación.");
            Console.WriteLine("  --help              Muestra esta ayuda.");
        }

        private static string ValorArg(string[] args, string clave)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(clave, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static bool TieneFlag(string[] args, string flag)
        {
            foreach (string a in args)
                if (a.Equals(flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
    }
}
