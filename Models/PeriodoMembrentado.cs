namespace WebApiVinculacionProyectosV2.Models
{
    public class PeriodoMembrentado
    {
        public int Id { get; set; }

        public int PeriodoAcademicoId { get; set; }

        public string FileName { get; set; } = "membrentado.pdf";
        public string ContentType { get; set; } = "application/pdf";

        // ✅ Guardamos el PDF en BD (varbinary(max))
        public byte[] PdfBytes { get; set; } = Array.Empty<byte>();

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
