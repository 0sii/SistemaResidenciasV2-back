namespace WebApiVinculacionProyectosV2.Models.DTOs
{
    public class LoginDTO
    {
        public string Nombre { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class LoginRequestDto
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string ApellidoPaterno { get; set; } = "";
        public string ApellidoMaterno { get; set; } = "";
        public string Correo { get; set; } = "";
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = default!;
        public UserDto User { get; set; } = default!;
    }


}
