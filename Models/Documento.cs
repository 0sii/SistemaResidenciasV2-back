using System;

namespace WebApiVinculacionProyectosV2.Models
{
    public enum EstadoRevisionDocumento
    {
        EnRevision = 0,
        Aceptado = 1,
        Rechazado = 2
    }

    public class Documento
    {
        public int Id { get; set; }
        public int IdEstudiante { get; set; }
        public int TipoDocumento { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
        public string NombreOriginal { get; set; } = null!;
        public string NombreServidor { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long TamanoBytes { get; set; }
        public string RutaFisica { get; set; } = null!;
        public string? UrlExterna { get; set; }

        // NUEVO: revisión administrativa
        public EstadoRevisionDocumento EstadoRevision { get; set; } = EstadoRevisionDocumento.EnRevision;
        public string? ComentarioRevision { get; set; }
        public DateTime? FechaRevision { get; set; }
        public int? RevisadoPorUsuarioId { get; set; }
    }
}