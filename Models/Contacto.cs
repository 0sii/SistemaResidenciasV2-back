namespace WebApiVinculacionProyectosV2.Models
{
    public class Contacto
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public string? nombre {  get; set; }
        public string? Telefono { get; set; }
        public string? correo { get; set; }
    }
}
