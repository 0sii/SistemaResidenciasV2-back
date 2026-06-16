using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebApiVinculacionProyectosV2.Models.Requests
{
    public class SepomexImportForm
    {
        [Required]
        public IFormFile File { get; set; } = default!;
    }
}