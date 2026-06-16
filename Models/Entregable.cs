namespace WebApiVinculacionProyectosV2.Models
{
    public class Entregable
    {
        public int Id { get; set; }

        public int IdProyecto { get; set; }
        public int IdTipoEntregable { get; set; }

        // Quién creó/subió la primera versión (autor “responsable”)
        public int IdEstudianteAutor { get; set; }

        // Para acceso rápido sin calcular siempre
        public int VersionActual { get; set; } = 0;
        public int IdEstadoEntregable { get; set; }   // NUEVO FK
        public DateTime FechaCreacion { get; set; }
    }
}
