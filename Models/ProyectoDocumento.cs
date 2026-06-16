namespace WebApiVinculacionProyectosV2.Models
{
    public class ProyectoDocumento
    {
        public int Id { get; set; }

        public int IdProyecto { get; set; }
        public string NombreOriginal { get; set; } = null!;
        public string NombreServidor { get; set; } = null!;
        public long TamanoBytes { get; set; }


        public int? UploadedByUserId { get; set; } // opcional

        public string? RutaFisica { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
        public string ContentType { get; set; } = null!;

    }
}
