using System.Net;
using System.Net.Mail;

namespace WebApiVinculacionProyectosV2.Servicios
{
    public interface IServicioEmail
    {
        Task EnviarEmail(string emailReceptor, string tema, string cuerpo, CancellationToken ct = default);
    }

    public class ServicioEmail : IServicioEmail
    {
        private readonly IConfiguration _configuration;

        public ServicioEmail(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmail(string emailReceptor, string tema, string cuerpo, CancellationToken ct = default)
        {
            // Validación de request
            if (string.IsNullOrWhiteSpace(emailReceptor))
                throw new ArgumentException("El email receptor es requerido.", nameof(emailReceptor));

            // Lee config
            var emailEmisor = _configuration["CONFIGURACIONES_EMAIL:EMAIL"];
            var password = _configuration["CONFIGURACIONES_EMAIL:PASSWORD"];
            var host = _configuration["CONFIGURACIONES_EMAIL:HOST"];
            var puertoStr = _configuration["CONFIGURACIONES_EMAIL:PUERTO"];

            // Validación de config
            if (string.IsNullOrWhiteSpace(emailEmisor))
                throw new InvalidOperationException("Falta configurar CONFIGURACIONES_EMAIL:EMAIL en appsettings o variables de entorno.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Falta configurar CONFIGURACIONES_EMAIL:PASSWORD en appsettings o variables de entorno.");
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Falta configurar CONFIGURACIONES_EMAIL:HOST en appsettings o variables de entorno.");
            if (string.IsNullOrWhiteSpace(puertoStr))
                throw new InvalidOperationException("Falta configurar CONFIGURACIONES_EMAIL:PUERTO en appsettings o variables de entorno.");

            if (!int.TryParse(puertoStr, out var puerto))
                throw new InvalidOperationException("PUERTO SMTP inválido en configuración.");

            using var smtp = new SmtpClient(host, puerto)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(emailEmisor, password),
                EnableSsl = true,
                Timeout = 15000
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(emailEmisor),
                Subject = tema ?? string.Empty,
                Body = cuerpo ?? string.Empty,
                IsBodyHtml = true
            };

            msg.To.Add(new MailAddress(emailReceptor));

            try
            {
                await smtp.SendMailAsync(msg, ct);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException($"Fallo SMTP ({ex.StatusCode}): {ex.Message}", ex);
            }
        }
    }
}
