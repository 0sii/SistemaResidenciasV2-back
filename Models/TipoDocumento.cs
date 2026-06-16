namespace WebApiVinculacionProyectosV2.Models
{
    public class TipoDocumento
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null;
        public bool Activo { get; set; }
    }
}
