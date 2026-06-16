namespace WebApiVinculacionProyectosV2.Models
{
    public class Especializacion
    {
        public int id { get; set; }
        public string? descripcion { get; set; }
        public bool Activo { get; set; }

        public ICollection<Proyectos> Proyectos { get; set; } = new List<Proyectos>();
    }
}
