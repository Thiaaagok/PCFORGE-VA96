using System;

namespace BE
{
    public class Notificacion06AV
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public TipoNotificacion06AV Tipo { get; set; }
        public string[] Datos { get; set; } = new string[0];
        public string Patente { get; set; }
        public string Destino { get; set; }
        public string ClaveUnica { get; set; }
        public string DniOrigen { get; set; }
        public bool Leida { get; set; }
        public bool Urgente =>
            Tipo == TipoNotificacion06AV.ControlCalidadRechazado ||
            Tipo == TipoNotificacion06AV.OrdenCompraAtrasada ||
            Tipo == TipoNotificacion06AV.ProduccionAtrasada ||
            Tipo == TipoNotificacion06AV.EntregaVencida;
    }
}
