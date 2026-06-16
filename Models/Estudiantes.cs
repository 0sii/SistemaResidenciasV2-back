namespace WebApiVinculacionProyectosV2.Models
{
    // Estudiante.cs
    public class Estudiantes
    {
        public int id { get; set; }
        public int idUsuario { get; set; }  // FK manual (sin navegación)
        public int? idProyecto { get; set; }
        public string Nombre { get; set; } = null!;
        public string ApellidoPaterno { get; set; } = null!;
        public string ApellidoMaterno { get; set; } = null!;
        public int? idcarrera { get; set; }
        public string? domicilio { get; set; }
        public string? ciudad { get; set; }
        public string? cp { get; set; }
        public string? noControl { get; set; }
        public string? correoPersonal { get; set; }
        public string? noSeguroSocial { get; set; }
        public int? idDependenciaMedica { get; set; }
        public string? telefonoCelular { get; set; }
        public int? idContactoEmergencia { get; set; }
    }

}