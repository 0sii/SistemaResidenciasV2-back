namespace WebApiVinculacionProyectosV2.Models
{
    // Docente.cs
    public class Docentes
    {
        public int Id { get; set; }
        public int idUsuario { get; set; }  // FK manual
        public string Nombre { get; set; } = null!;
        public string ApellidoPaterno { get; set; } = null!;
        public string ApellidoMaterno { get; set; } = null!;
        public string? RFC { get; set; }
        public string? Telefono { get; set; }
        public string? NivelAcademico { get; set; }
        public bool EsJefeDepartamento { get; set; } = false;
    }

}
