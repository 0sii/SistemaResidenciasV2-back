namespace WebApiVinculacionProyectosV2.Models
{
    public class EntregableVersion
    {
        public int Id { get; set; }

        public int IdEntregable { get; set; }
        public int NumeroVersion { get; set; }

        public int IdEstudianteSubio { get; set; }

        public DateTime FechaSubida { get; set; }

        public string NombreOriginal { get; set; } = null!;
        public string NombreServidor { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long TamanoBytes { get; set; }
        public string RutaFisica { get; set; } = null!;
    }
}
