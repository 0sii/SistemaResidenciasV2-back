namespace WebApiVinculacionProyectosV2.Dto
{
    public class CrearInvitacionDto
    {
        public int IdEstudianteInvitado { get; set; }
    }

    public class ResponderInvitacionDto
    {
        // "ACEPTAR" o "RECHAZAR"
        public string Accion { get; set; } = "";
    }

    public class InvitacionMiaDto
    {
        public int Id { get; set; }
        public int IdProyecto { get; set; }
        public string? TituloProyecto { get; set; }

        public int IdEstudianteCreador { get; set; }
        public string? NombreCreador { get; set; } // opcional para mostrar bonito en UI

        public string Estado { get; set; } = "PENDIENTE";
        public System.DateTime FechaCreacion { get; set; }
    }

    public class InvitacionEnviadaDto
    {
        public int Id { get; set; }
        public int IdProyecto { get; set; }
        public int IdEstudianteInvitado { get; set; }
        public int IdEstudianteCreador { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public System.DateTime FechaCreacion { get; set; }
        public System.DateTime? FechaRespuesta { get; set; }

        public string? NoControlInvitado { get; set; }
        public string? NombreInvitado { get; set; }
    }
    public class EsLiderDto
    {
        public int IdProyecto { get; set; }
        public bool EsLider { get; set; }
        public int? IdEstudianteCreador { get; set; }
    }

}
