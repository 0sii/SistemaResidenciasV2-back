using System.Runtime.CompilerServices;

namespace WebApiVinculacionProyectosV2.Models
{
    public class DependenciaMedica
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}
