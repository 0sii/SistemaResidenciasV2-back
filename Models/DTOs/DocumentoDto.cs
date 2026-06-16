namespace WebApiVinculacionProyectosV2.Models.DTOs
{
    public class DocumentoDto
    {
        public int Id { get; set; }
        public int TipoDocumento { get; set; }
        public DateTime FechaSubida { get; set; }
        public string NombreOriginal { get; set; } = null!;
        public long TamanoBytes { get; set; }
    }
}
