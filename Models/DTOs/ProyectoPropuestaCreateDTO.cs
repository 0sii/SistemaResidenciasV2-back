namespace WebApiVinculacionProyectosV2.Dto
{
    public class ProyectoPropuestaCreateDto
    {
        // ✅ Empresa obligatoria: o mandas IdEmpresa o EmpresaNueva
        public int? IdEmpresa { get; set; }
        public EmpresaCreateDto? EmpresaNueva { get; set; }

        // Proyecto (con tus nombres)
        public int idEspecializcion { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Objetivo { get; set; }
        public int idPeridoAcademico { get; set; }
        public int NoResidentes { get; set; }
        public System.TimeSpan? HorarioInicio { get; set; }
        public System.TimeSpan? HorarioFinal { get; set; }

        public int? idModalidad { get; set; }
        public int? idEstado { get; set; }
    }
}
