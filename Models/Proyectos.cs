namespace WebApiVinculacionProyectosV2.Models
{
    public class Proyectos
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public int? idEspecializcion { get; set; }

        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Objetivo { get; set; }
        public DateTime FechaRegistro { get; set; }  
        public int? NoResidentes { get; set; }
        public TimeSpan? HorarioInicio { get; set; }
        public TimeSpan? HorarioFinal { get; set; }
        public int? IdPeriodoAcademico { get; set; }
        public int? idModalidad { get; set; }
        public int? idEstado { get; set; }
        public bool PropuestaAlumno { get; set; }

        public int? IdEstudianteCreador { get; set; }
    }
}
