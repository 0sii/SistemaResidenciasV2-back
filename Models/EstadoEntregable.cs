namespace WebApiVinculacionProyectosV2.Models
{
    public class EstadoEntregable
    {
        public int Id { get; set; }
        public string Clave { get; set; } = null!;        // PENDIENTE, EN_REVISION, ...
        public string Descripcion { get; set; } = null!;  // Texto bonito
        public bool Activo { get; set; } = true;

    }
}
