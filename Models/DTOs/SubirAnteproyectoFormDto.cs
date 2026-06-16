namespace WebApiVinculacionProyectosV2.Models.DTOs
{
    public class SubirAnteproyectoForm
    {
        public IFormFile Archivo { get; set; } = default!;
        public bool AplicarAEquipo { get; set; } = true;
    }


}
