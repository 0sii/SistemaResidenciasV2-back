namespace WebApiVinculacionProyectosV2.Models
{
    public class RevisionEntregable
    {
        public int Id { get; set; }
        public int IdEntregableVersion { get; set; }
        public int NumeroRevision { get; set; }
        public int IdDocenteRevisor { get; set; }

        public string Dictamen { get; set; } = "CAMBIOS"; // CAMBIOS, APROBADO, RECHAZADO
        public string Observaciones { get; set; } = null!;
        public DateTime FechaRevision { get; set; }

        // ✅ NUEVO: archivo opcional del docente (respuesta)
        public string? NombreOriginal { get; set; }
        public string? NombreServidor { get; set; }
        public string? ContentType { get; set; }
        public long? TamanoBytes { get; set; }
        public string? RutaFisica { get; set; }
    }
}
