using System;

namespace WebApiVinculacionProyectosV2.Models
{
    public class InvitacionProyecto
    {
        public int Id { get; set; }

        public int IdProyecto { get; set; }
        public int IdEstudianteInvitado { get; set; }
        public int IdEstudianteCreador { get; set; }

        // PENDIENTE | ACEPTADA | RECHAZADA | CANCELADA
        public string Estado { get; set; } = "PENDIENTE";

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRespuesta { get; set; }
    }
}
