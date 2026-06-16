using System.Security.Cryptography;
using System.Text;

public static class SecurityUtils
{
    // Método para generar un hash SHA256 de un texto (en este caso, el código OTP)
    public static string Sha256(string input)
    {
        using var sha = SHA256.Create();  // Crea un objeto SHA256 para hacer el hashing
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));  // Genera el hash del input en bytes
        return Convert.ToHexString(bytes);  // Convierte los bytes a un string hexadecimal (en mayúsculas)
    }

    // (Opcional) Si alguna vez necesitas generar otros tipos de hashes o mejorar la seguridad
    public static string HashPassword(string password)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, 16, 100000, HashAlgorithmName.SHA256);
        return Convert.ToHexString(pbkdf2.GetBytes(32));  // Genera el hash de la contraseña
    }
}
