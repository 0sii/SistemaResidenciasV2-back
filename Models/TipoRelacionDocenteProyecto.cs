namespace WebApiVinculacionProyectosV2.Models
{
    public class TipoRelacionDocenteProyecto
    {
        public int Id { get; set; }
        public string Clave { get; set; } = null!;        // "REVISOR_ANTEPROYECTO"
        public string Descripcion { get; set; } = null!;  // "Revisor de anteproyecto"
        public bool Activo { get; set; } = true;
    }
}
