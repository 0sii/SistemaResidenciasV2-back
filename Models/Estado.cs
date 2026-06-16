namespace WebApiVinculacionProyectosV2.Models
{
    public class Estado
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }

        public ICollection<Proyectos> Proyectos { get; set; } = new List<Proyectos>();
    }
}
