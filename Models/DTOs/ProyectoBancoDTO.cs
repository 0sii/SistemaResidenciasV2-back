namespace WebApiVinculacionProyectosV2.Dto
{
    public class ProyectoBancoDto
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public int? idEspecializcion { get; set; }

        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Objetivo { get; set; }

        public System.DateTime? FechaRegistro { get; set; }
        public int? idPeriodoAcademico { get; set; }
        public int? NoResidentes { get; set; }
        public System.TimeSpan? HorarioInicio { get; set; }
        public System.TimeSpan? HorarioFinal { get; set; }

        public int? idModalidad { get; set; }
        public int? idEstado { get; set; }

        public bool PropuestaAlumno { get; set; }

        // ✅ Conteo real de alumnos asignados (Estudiantes.idProyecto = Proyectos.Id)
        public int Registrados { get; set; }
    }
    public class AsignarDocenteRelacionDto
    {
        public int IdDocente { get; set; }
        public string TipoClave { get; set; } // default
    }
    public class PeriodoAcademicoDto
    {
        public string Nombre { get; set; } = null!;
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public bool Activo { get; set; }
    }
    public class ProyectoCreateDto
    {
        public string Titulo { get; set; } = null!;
        public int IdPeriodoAcademico { get; set; }
    }

}
