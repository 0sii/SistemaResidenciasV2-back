using System;

namespace WebApiVinculacionProyectosV2.Models
{
    /// <summary>
    /// Registro de un oficio ya emitido. Guarda el número de oficio y la fecha la PRIMERA vez
    /// que se genera, para que las descargas posteriores reutilicen exactamente los mismos
    /// valores (no se consume otro consecutivo ni cambia la fecha del PDF).
    ///
    /// Llave lógica: Periodo + TipoClave + Docente + Proyecto + Estudiante
    ///   • ASESOR_INTERNO        → un oficio por alumno (IdProyecto e IdEstudiante).
    ///   • REVISOR_ANTEPROYECTO  → un oficio por proyecto (IdProyecto; IdEstudiante = 0).
    ///   • REVISOR_RESIDENCIA    → un oficio por docente con todos sus proyectos (ambos = 0).
    /// Se usa 0 y no NULL porque MySQL permite duplicados de NULL en índices únicos.
    /// </summary>
    public class OficioEmitido
    {
        public int Id { get; set; }

        public int IdPeriodoAcademico { get; set; }

        /// <summary>ASESOR_INTERNO | REVISOR_ANTEPROYECTO | REVISOR_RESIDENCIA</summary>
        public string TipoClave { get; set; } = null!;

        public int IdDocente { get; set; }

        /// <summary>0 = no aplica (p. ej. oficio de revisor de residencia con todos los proyectos).</summary>
        public int IdProyecto { get; set; }

        /// <summary>Solo para el oficio de asesor interno (uno por alumno). 0 = no aplica.</summary>
        public int IdEstudiante { get; set; }

        /// <summary>Ej. "JV-012/26"</summary>
        public string NumeroOficio { get; set; } = null!;

        /// <summary>Fecha con la que se imprimió el oficio la primera vez.</summary>
        public DateTime FechaEmision { get; set; }

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    }
}
