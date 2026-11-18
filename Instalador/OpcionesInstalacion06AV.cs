using System.Data.SqlClient;

namespace Instalador
{
    public enum ModoInstalacion06AV
    {
        Instalar,
        Reparar,
        ReinstalarLimpio,
        Restaurar
    }

    public class OpcionesInstalacion06AV
    {
        public string Servidor { get; set; } = ".";

        public string BaseDatos { get; set; } = "IngSoftValdezAlegre";

        public bool SeguridadIntegrada { get; set; } = true;

        public string Usuario { get; set; }
        public string Contrasenia { get; set; }

        public string CarpetaScripts { get; set; }

        public ModoInstalacion06AV Modo { get; set; } = ModoInstalacion06AV.Instalar;

        // Ruta del .bak a restaurar (modo Restaurar). La lee el servicio de SQL Server,
        // no este proceso: tiene que ser accesible para la cuenta del servicio.
        public string ArchivoRestore { get; set; }

        // Copiar los archivos del sistema (Sistema.zip embebido) antes de preparar la base.
        public bool InstalarSistema { get; set; } = true;

        public string CarpetaInstalacion { get; set; } = InstaladorSistema06AV.CarpetaInstalada();

        public bool CrearAccesoDirecto { get; set; } = true;

        public bool AbrirAlFinalizar { get; set; } = true;

        public string CadenaMaster() => Construir("master");

        public string CadenaBaseDatos() => Construir(BaseDatos);

        public OpcionesInstalacion06AV Clonar(string baseDatos = null)
        {
            var c = (OpcionesInstalacion06AV)MemberwiseClone();
            if (baseDatos != null) c.BaseDatos = baseDatos;
            return c;
        }

        private string Construir(string catalogo)
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = Servidor,
                InitialCatalog = catalogo,
                ConnectTimeout = 10
            };

            if (SeguridadIntegrada)
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.UserID = Usuario;
                b.Password = Contrasenia;
            }

            return b.ConnectionString;
        }
    }
}
