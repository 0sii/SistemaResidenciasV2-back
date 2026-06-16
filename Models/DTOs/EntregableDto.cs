namespace WebApiVinculacionProyectosV2.Models.DTOs
{
    public class CreateEntregableDto
    {
        public int IdProyecto { get; set; }
        public int IdTipoEntregable { get; set; }
        public int IdEstudianteAutor { get; set; }
    }

    public class UploadEntregableVersionDto
    {
        public int IdEstudianteSubio { get; set; }
        public IFormFile Archivo { get; set; } = null!;
    }

    
    public class CancelarProyectoDto
    {
        public string? Motivo { get; set; } // opcional
    }
    public class CreateRevisionEntregableDto
    {
        public string Dictamen { get; set; } = "CAMBIOS"; // CAMBIOS, APROBADO, RECHAZADO
        public string Observaciones { get; set; } = "";
    }
    public class CreateRevisionDto
    {
        public string Dictamen { get; set; } = "CAMBIOS";
        public string Observaciones { get; set; } = "";
    }
}
