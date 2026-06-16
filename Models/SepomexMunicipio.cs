using System.ComponentModel.DataAnnotations;

namespace WebApiVinculacionProyectosV2.Models
{
    public class SepomexMunicipio
    {
        [StringLength(2)]
        public string EstadoId { get; set; } = null!;

        [StringLength(3)]
        public string MunicipioId { get; set; } = null!; // "014"

        [Required, StringLength(160)]
        public string Nombre { get; set; } = null!; // normalizado

        [Required, StringLength(5)]
        public string Rango1 { get; set; } = "00000";

        [Required, StringLength(5)]
        public string Rango2 { get; set; } = "99999";
    }
}