namespace WebApiVinculacionProyectosV2.Models
{
    public class Empresas
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string RFC { get; set; } = null;
        public string? Giro { get; set; }
        public string? Mision { get; set; } = null;
        public string? Domicilio { get; set; } = null;
        public int? Colonia { get; set; } = null;
        public int? Estado { get; set; } = null;
        public int? Municipio { get; set; } = null;
        public string? Ciudad { get; set; } = null;
        public string? CP { get; set; } = null;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Titular { get; set; } = null;
        public string? PuestoTitular { get; set; } = null;

        public ICollection<Proyectos> Proyectos { get; set; } = new List<Proyectos>();  
    }
}
