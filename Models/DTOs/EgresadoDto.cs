namespace WebApiVinculacionProyectosV2.Models.DTOs
{
    public class EgresadoDto
    {
        public int    IdEstudiante   { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? NoControl      { get; set; }
        public string? CorreoPersonal { get; set; }
        public string? Telefono       { get; set; }

        public int    IdProyecto     { get; set; }
        public string? TituloProyecto { get; set; }
        public string? DescripcionProyecto { get; set; }

        public string? Asesor    { get; set; }
        public string? Revisor   { get; set; }

        public string? Periodo   { get; set; }
        public string? Carrera   { get; set; }
        public string? Modalidad { get; set; }

        // Empresa
        public string? Empresa        { get; set; }
        public string? EmpresaCorreo  { get; set; }
        public string? EmpresaTelefono { get; set; }
        public string? EmpresaDireccion { get; set; }
    }
}
