namespace WebApiVinculacionProyectosV2.Models
{
    public class PeriodoAcademico
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public bool Activo { get; set; }

        // ✅ Solo guardamos el nombre del jefe por período
        public string? JefeDepartamentoNombre { get; set; }

        // ✅ Interno (no UI)
        public string PrefijoOficio { get; set; } = "JV";

        // ✅ UN SOLO consecutivo compartido para todos los oficios del sistema
        public int ConsecutivoOficio { get; set; } = 1;
    }
}