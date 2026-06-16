using System.ComponentModel.DataAnnotations;

namespace WebApiVinculacionProyectosV2.Models
{
    public class SepomexEstado
    {
        [Key]
        [StringLength(2)]
        public string EstadoId { get; set; } = null!; // "09"

        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;   // "CIUDAD DE MEXICO" (normalizado)

        [StringLength(10)]
        public string? Abreviatura { get; set; }      // "CMX"

        [Required, StringLength(5)]
        public string Rango1 { get; set; } = "00000";

        [Required, StringLength(5)]
        public string Rango2 { get; set; } = "99999";
    }
}