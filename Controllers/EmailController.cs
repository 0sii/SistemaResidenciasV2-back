using Microsoft.AspNetCore.Mvc;
using WebApiVinculacionProyectosV2.Servicios;

namespace WebApiVinculacionProyectosV2.Controllers
{
    public class EmailRequest
    {
        public string Email { get; set; }
        public string Tema { get; set; }
        public string Cuerpo { get; set; }
    }

    [Route("api/emails")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IServicioEmail _servicioEmail;

        public EmailController(IServicioEmail servicioEmail)
        {
            _servicioEmail = servicioEmail;
        }

        [HttpPost]
        public async Task<IActionResult> Enviar([FromBody] EmailRequest request, CancellationToken ct)
        {
            if (request == null)
                return BadRequest("Body requerido.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email es requerido.");

            if (string.IsNullOrWhiteSpace(request.Tema))
                return BadRequest("Tema es requerido.");

            if (string.IsNullOrWhiteSpace(request.Cuerpo))
                return BadRequest("Cuerpo es requerido.");

            await _servicioEmail.EnviarEmail(request.Email, request.Tema, request.Cuerpo, ct);
            return Ok(new { ok = true });
        }
    }
}