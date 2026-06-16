using System.ComponentModel.DataAnnotations;

namespace WebApiVinculacionProyectosV2.Models
{
    public class SepomexColonia
    {
        [Key]
        [StringLength(10)]
        public string ColoniaId { get; set; } = null!; // id_asenta_cpcons

        [Required, StringLength(2)]
        public string EstadoId { get; set; } = null!;

        [Required, StringLength(3)]
        public string MunicipioId { get; set; } = null!;

        [Required, StringLength(5)]
        public string Cp { get; set; } = null!; // d_codigo

        [Required, StringLength(200)]
        public string Nombre { get; set; } = null!; // d_asenta

        [StringLength(5)]
        public string? Cr { get; set; } // centro de reparto/oficina

        public DateTime FechaAct { get; set; } = DateTime.UtcNow;
    }
}