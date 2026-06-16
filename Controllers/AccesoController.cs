using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WebApiVinculacionProyectosV2.Custom;
using WebApiVinculacionProyectosV2.Models.DTOs;
using WebApiVinculacionProyectosV2.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using WebApiVinculacionProyectosV2.Servicios;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;


public class OtpData
{
    public string CodeHash { get; set; }
    public DateTimeOffset ExpiryTime { get; set; }
    public int Attempts { get; set; }
    public bool Used { get; set; }
}


namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class AccesoController : ControllerBase
    {
        private readonly ResidenciasDbContext _dbContext;
        private readonly Utilidades _utilidades;

        private readonly IMemoryCache _cache; // Memoria temporal para almacenar el OTP
        private readonly IServicioEmail _emailService; // Servicio para enviar el correo

        private readonly IConfiguration _config;

        public record PasswordChangeRequestDto(string Email);  // Solicitar OTP
        public record PasswordVerifyAndChangeDto(
            Guid OtpId,
            string Code,
            string NewPassword,
            string Email
        );  // Validar OTP y cambiar contraseña

        public AccesoController(ResidenciasDbContext dbContext, Utilidades utilidades, IMemoryCache cache, IServicioEmail emailService, IConfiguration config)
        {
            _dbContext = dbContext;
            _utilidades = utilidades;
            _cache = cache;
            _emailService = emailService;
            _config = config;

        }

        [HttpPost]
        [Route("Registrarse")]
        public async Task<IActionResult> Registrarse(LoginDTO objeto)
        {

            var modeloUsuario = new Usuarios
            {

                Correo = objeto.Email,
                PasswordHash = _utilidades.encriptarSHA256(objeto.Password)
            };

            await _dbContext.Usuarios.AddAsync(modeloUsuario);
            await _dbContext.SaveChangesAsync();

            if (modeloUsuario.Id != 0)
                return StatusCode(StatusCodes.Status200OK, new { isSuccess = true });
            else
                return StatusCode(StatusCodes.Status200OK, new { isSuccess = false });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email y contraseña son requeridos." });

            var passHash = _utilidades.encriptarSHA256(req.Password);

            var usuario = await _dbContext.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == req.Email && u.PasswordHash == passHash);

            if (usuario is null)
                return Unauthorized(new { message = "Credenciales inválidas." });

            var token = _utilidades.generarJWT(usuario);

            var userDto = new UserDto
            {
                Id = usuario.Id,
                Correo = usuario.Correo,
                Nombre = usuario.Nombre,
                ApellidoPaterno = usuario.ApellidoPaterno,
                ApellidoMaterno = usuario.ApellidoMaterno
            };

            var resp = new LoginResponseDto { Token = token, User = userDto };
            return Ok(resp);
        }


        [HttpGet("ValidarToken")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> ValidarToken(
             [FromHeader(Name = "Authorization")] string? authorization,
             [FromQuery] string? token = null)
        {
            try
            {
                // 1) Extraer token de Authorization o query
                string? rawToken = null;

                if (!string.IsNullOrWhiteSpace(authorization) &&
                    authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    rawToken = authorization.Substring("Bearer ".Length).Trim();
                }
                else if (!string.IsNullOrWhiteSpace(token))
                {
                    rawToken = token.Trim();
                }

                if (string.IsNullOrWhiteSpace(rawToken))
                    return Ok(false);

                // 2) Validar firma y expiración (sin issuer/audience)
                var key = _config["Jwt:key"];
                if (string.IsNullOrWhiteSpace(key))
                    return Ok(false);

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal principal = tokenHandler.ValidateToken(rawToken, validationParams, out _);

                // 3) Extraer userId del claim y verificar en BD
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Ok(false);

                var usuario = await _dbContext.Usuarios.FindAsync(userId);
                if (usuario is null)
                    return Ok(false);

                // 4) Todo OK
                return Ok(true);
            }
            catch
            {
                return Ok(false);
            }
        }


        // POST: api/Acceso/password/request-change
        [HttpPost("password/request-change")]
        public async Task<IActionResult> RequestPasswordChange([FromBody] PasswordChangeRequestDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "El correo es obligatorio." });

            var email = dto.Email.Trim().ToLowerInvariant();

            // Verificar si el usuario existe
            var user = await _dbContext.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == email, ct);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado." });

            // Generar el código OTP (6 dígitos)
            var code = new Random().Next(100000, 999999).ToString();  // Genera un código OTP de 6 dígitos

            // Almacenar el OTP en la memoria
            var otpId = Guid.NewGuid();
            var otpData = new OtpData
            {
                CodeHash = SecurityUtils.Sha256(code),  // Hashear el código para no almacenarlo en texto plano
                ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10),
                Attempts = 0
            };

            _cache.Set(otpId.ToString(), otpData, otpData.ExpiryTime);  // Guardar en cache

            // Enviar el código al correo (ahora lo hace el backend)
            var subject = "Código para cambiar tu contraseña";
            var body = $"Tu código de verificación es: {code}\nEste código expira en 10 minutos.";
            await _emailService.EnviarEmail(email, subject, body, ct);  // Enviar el código por correo

            // Devolver el otpId para usarlo en la validación
            return Ok(new { otpId });
        }

        // POST: api/Acceso/password/verify-and-change
        [HttpPost("password/verify-and-change")]
        public async Task<IActionResult> VerifyAndChangePassword([FromBody] PasswordVerifyAndChangeDto dto, CancellationToken ct)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Datos incompletos." });

            // Obtener el OTP desde la memoria
            if (!_cache.TryGetValue(dto.OtpId.ToString(), out OtpData otpData))
            {
                return BadRequest(new { message = "Código OTP inválido o expirado." });
            }

            // Validar que el código no haya expirado
            if (DateTimeOffset.UtcNow > otpData.ExpiryTime)
            {
                _cache.Remove(dto.OtpId.ToString());  // Eliminar OTP expirado
                return BadRequest(new { message = "El código ha expirado." });
            }

            // Validar el código ingresado
            var enteredCodeHash = SecurityUtils.Sha256(dto.Code);
            if (enteredCodeHash != otpData.CodeHash)
            {
                otpData.Attempts++;
                _cache.Set(dto.OtpId.ToString(), otpData, otpData.ExpiryTime);  // Actualizar intentos en memoria

                return BadRequest(new { message = "Código incorrecto." });
            }

            // Obtener el usuario desde el DTO
            var user = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Email, ct);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Cambiar la contraseña
            user.PasswordHash = _utilidades.encriptarSHA256(dto.NewPassword);  // Encriptar la nueva contraseña
            await _dbContext.SaveChangesAsync(ct);

            // Marcar el OTP como usado
            otpData.Used = true;
            _cache.Remove(dto.OtpId.ToString());  // Eliminar OTP usado

            return NoContent();  // No content si todo fue exitoso
        }
    }
}