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

        // Fecha en que el asesor interno aceptó formalmente el anteproyecto.
        // Se conserva para que la constancia no dependa de la fecha de descarga.
        public DateTime? FechaAceptacionAnteproyecto { get; set; }
        public int? NoResidentes { get; set; }
        public TimeSpan? HorarioInicio { get; set; }
        public TimeSpan? HorarioFinal { get; set; }
        public int? IdPeriodoAcademico { get; set; }
        public int? idModalidad { get; set; }
        public int? idEstado { get; set; }
        public bool PropuestaAlumno { get; set; }

        public int? IdEstudianteCreador { get; set; }

        // Nº de oficio de "Asignación de revisor de reporte preliminar" que se emitió para este
        // proyecto. La "Aceptación del Reporte Preliminar" lo cita en "En respuesta al oficio ___".
        public string? OficioRevisorReportePreliminar { get; set; }
    }
}
