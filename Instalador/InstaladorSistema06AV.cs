using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Instalador
{
    /// <summary>
    /// Instala los archivos del sistema. El .exe del sistema, sus DLL, el .config y la carpeta
    /// Resources viajan dentro de PCFORGE.exe (el instalador) como recurso "Sistema.zip" (lo arma el target
    /// EmpaquetarSistema06AV de Instalador.csproj al compilar). Se extraen en
    /// Archivos de programa\PCFORGE, se crean los accesos del menú Inicio y se registra la
    /// aplicación en "Aplicaciones instaladas" para poder desinstalarla.
    /// </summary>
    public static class InstaladorSistema06AV
    {
        public const string NombreProducto = "PCFORGE";
        public const string Version = "2.0";
        private const string RecursoPaquete = "Sistema.zip";
        private const string ClaveDesinstalar = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PCFORGE-ValdezThiago-96VA";
        private const string NombreAcceso = "PCFORGE-ValdezThiago-96VA";
        // Nombre de la copia del instalador que queda en la carpeta del sistema (reinstalar / desinstalar).
        // No se llama PCFORGE.exe para no confundirla con el sistema, que está al lado.
        public const string NombreCopiaInstalador = "PCFORGE-Instalador.exe";
        private const string NombreCopiaAnterior = "Instalador.exe";   // nombre de versiones anteriores

        private const string NombreAccesoReinstalar = "Configurar o reinstalar la base de datos";

        public static string CarpetaPorDefecto =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), NombreProducto);

        // Carpeta donde quedó instalado (según el registro) o la de por defecto.
        public static string CarpetaInstalada()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(ClaveDesinstalar))
                {
                    string ruta = k?.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(ruta)) return ruta;
                }
            }
            catch {  }
            return CarpetaPorDefecto;
        }

        public static bool TienePaquete() =>
            Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(RecursoPaquete);

        public static string RutaExe(string carpeta) => Path.Combine(carpeta, ConfiguradorApp06AV.NombreExeApp);

        /// <summary>Copia los archivos del sistema a la carpeta. Devuelve la ruta del .exe instalado.</summary>
        public static string Instalar(string carpeta, Action<string> log)
        {
            if (!TienePaquete())
                throw new InvalidOperationException(
                    "Este PCFORGE.exe no contiene el sistema. Compilá la solución completa en Release para generarlo.");

            string proceso = Path.GetFileNameWithoutExtension(ConfiguradorApp06AV.NombreExeApp);
            if (Process.GetProcessesByName(proceso).Length > 0)
                throw new InvalidOperationException(
                    "El sistema está abierto. Cerralo y volvé a ejecutar el instalador.");

            log?.Invoke($"Instalando el sistema en {carpeta}...");
            string raiz = Path.GetFullPath(carpeta).TrimEnd('\\') + "\\";
            Directory.CreateDirectory(raiz);

            int archivos = 0;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(RecursoPaquete))
            using (var zip = new ZipArchive(s, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    string destino = Path.GetFullPath(Path.Combine(raiz, e.FullName.Replace('/', '\\')));
                    if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) continue;
                    if (e.FullName.EndsWith("/") || string.IsNullOrEmpty(e.Name))
                    {
                        Directory.CreateDirectory(destino);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destino));
                    e.ExtractToFile(destino, true);
                    archivos++;
                }
            }

            // El propio instalador queda en la carpeta: es el reinstalador y el desinstalador.
            string yo = Assembly.GetExecutingAssembly().Location;
            string copia = Path.Combine(raiz, NombreCopiaInstalador);
            if (!string.Equals(Path.GetFullPath(yo), Path.GetFullPath(copia), StringComparison.OrdinalIgnoreCase))
                File.Copy(yo, copia, true);
            // La copia con el nombre viejo (Instalador.exe) se reemplaza por la nueva.
            string anterior = Path.Combine(raiz, NombreCopiaAnterior);
            try
            {
                if (File.Exists(anterior) &&
                    !string.Equals(Path.GetFullPath(yo), Path.GetFullPath(anterior), StringComparison.OrdinalIgnoreCase))
                    File.Delete(anterior);
            }
            catch { }

            string exe = RutaExe(raiz);
            if (!File.Exists(exe))
                throw new FileNotFoundException("No se pudo instalar " + ConfiguradorApp06AV.NombreExeApp + ".", exe);

            log?.Invoke($"Sistema instalado ({archivos} archivo(s)).");
            return exe;
        }

        public static void CrearAccesosMenuInicio(string carpeta, Action<string> log)
        {
            string menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), NombreProducto);
            try
            {
                Directory.CreateDirectory(menu);
                bool ok = ConfiguradorApp06AV.CrearAcceso(Path.Combine(menu, NombreAcceso + ".lnk"), RutaExe(carpeta), null,
                              "Sistema PCFORGE-ValdezThiago-96VA", out string e1) != null
                          & ConfiguradorApp06AV.CrearAcceso(Path.Combine(menu, NombreAccesoReinstalar + ".lnk"),
                              Path.Combine(carpeta, NombreCopiaInstalador), null, "Reparar, reinstalar o restaurar la base", out string e2) != null;
                log?.Invoke(ok ? "Accesos creados en el menú Inicio." : "Aviso: no se pudieron crear los accesos del menú Inicio: " + (e1 ?? e2));
            }
            catch (Exception ex)
            {
                log?.Invoke("Aviso: no se pudieron crear los accesos del menú Inicio: " + ex.Message);
            }
        }

        // Escritorio público: el instalador corre como administrador, así el acceso lo ve el usuario
        // aunque la cuenta elevada sea otra.
        public static string CrearAccesoEscritorio(string carpeta, out string error) =>
            ConfiguradorApp06AV.CrearAcceso(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), NombreAcceso + ".lnk"),
                RutaExe(carpeta), null, "Sistema PCFORGE-ValdezThiago-96VA", out error);

        public static void RegistrarDesinstalacion(string carpeta, Action<string> log)
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.CreateSubKey(ClaveDesinstalar))
                {
                    string instalador = Path.Combine(carpeta, NombreCopiaInstalador);
                    k.SetValue("DisplayName", "PCFORGE - Sistema de ensamblaje de PC");
                    k.SetValue("DisplayVersion", Version);
                    k.SetValue("Publisher", "Valdez Thiago - UAI");
                    k.SetValue("InstallLocation", carpeta);
                    k.SetValue("DisplayIcon", RutaExe(carpeta));
                    k.SetValue("UninstallString", "\"" + instalador + "\" --desinstalar");
                    k.SetValue("ModifyPath", "\"" + instalador + "\"");
                    k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    k.SetValue("EstimatedSize", (int)(TamanioCarpeta(carpeta) / 1024), RegistryValueKind.DWord);
                }
                log?.Invoke("Registrado en «Aplicaciones instaladas» de Windows.");
            }
            catch (Exception ex)
            {
                log?.Invoke("Aviso: no se pudo registrar en «Aplicaciones instaladas»: " + ex.Message);
            }
        }

        /// <summary>Quita archivos, accesos y registro. La base de datos NO se toca.</summary>
        public static void Desinstalar()
        {
            string carpeta = CarpetaInstalada();
            string proceso = Path.GetFileNameWithoutExtension(ConfiguradorApp06AV.NombreExeApp);
            if (Process.GetProcessesByName(proceso).Length > 0)
                throw new InvalidOperationException("El sistema está abierto. Cerralo y volvé a intentar.");

            BorrarSilencioso(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), NombreAcceso + ".lnk"));
            BorrarSilencioso(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), NombreAcceso + ".lnk"));
            try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), NombreProducto), true); } catch {  }
            try { Registry.LocalMachine.DeleteSubKeyTree(ClaveDesinstalar, false); } catch {  }

            if (!Directory.Exists(carpeta)) return;
            string yo = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            foreach (string f in Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories))
                if (!string.Equals(Path.GetFullPath(f), yo, StringComparison.OrdinalIgnoreCase))
                    BorrarSilencioso(f);
            foreach (string d in Directory.GetDirectories(carpeta))
                try { Directory.Delete(d, true); } catch {  }

            // Si el desinstalador corre desde la carpeta instalada, se borra a sí mismo al salir.
            bool dentro = yo.StartsWith(Path.GetFullPath(carpeta).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
            if (!dentro)
            {
                try { Directory.Delete(carpeta, true); } catch {  }
                return;
            }
            Process.Start(new ProcessStartInfo("cmd.exe",
                $"/c ping 127.0.0.1 -n 3 > nul & del /f /q \"{yo}\" & rmdir /q \"{carpeta}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        // Abre el sistema como el usuario del escritorio (no elevado) aunque el instalador corra
        // como administrador.
        public static void AbrirSistema(string exe)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + exe + "\"") { UseShellExecute = true });
        }

        private static long TamanioCarpeta(string carpeta)
        {
            try { return Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); }
            catch { return 0; }
        }

        private static void BorrarSilencioso(string ruta)
        {
            try { if (File.Exists(ruta)) File.Delete(ruta); } catch {  }
        }
    }
}
