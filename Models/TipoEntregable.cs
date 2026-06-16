namespace WebApiVinculacionProyectosV2.Models
{
    public class TipoEntregable
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public int? MaxRevisiones { get; set; } // NULL = ilimitado
        public bool Activo { get; set; } = true;
    }
}
