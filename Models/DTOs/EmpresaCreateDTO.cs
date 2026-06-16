namespace WebApiVinculacionProyectosV2.Dto
{
    public class EmpresaCreateDto
    {
        public string Nombre { get; set; } = "";
        public string RFC { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";

        public string? Giro { get; set; }
        public string? Mision { get; set; }
        public string? Domicilio { get; set; }
        public int? Colonia { get; set; }
        public int? Estado { get; set; }
        public int? Municipio { get; set; }
        public string? Ciudad { get; set; }
        public string? CP { get; set; }

        public string? Titular { get; set; }
        public string? PuestoTitular { get; set; }
    }
}
