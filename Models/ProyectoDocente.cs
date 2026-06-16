using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace WebApiVinculacionProyectosV2.Models
{
    public class ProyectoDocente
    {
        public int id { get; set; }
        public int idProyecto {  get; set; }
        public int idDocente { get; set; }
        public int IdTipoRelacion { get; set; }
        public DateOnly FechaInscripcion {  get; set; }
    }
}
