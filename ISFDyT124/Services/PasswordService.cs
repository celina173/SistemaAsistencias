using ISFDyT124.Models; // Usuario, tipo genérico requerido por PasswordHasher<TUser>
using Microsoft.AspNetCore.Identity; // PasswordHasher<TUser>, PasswordVerificationResult

namespace ISFDyT124.Services // Define espacio de nombres para servicios
{
    // CAMBIO: se reemplaza el hash SHA256 manual (sin salt, rápido por diseño) por
    // PasswordHasher<TUser> de ASP.NET Core Identity, que aplica PBKDF2 + HMAC-SHA512
    // con salt aleatorio de 128 bits por contraseña y 100.000 iteraciones. Motivo: SHA256
    // solo es apto para verificar integridad de datos, no para contraseñas — sin salt,
    // dos usuarios con la misma contraseña quedan con el mismo hash (vulnerable a rainbow
    // tables) y es demasiado rápido de calcular como para frenar fuerza bruta.
    public static class PasswordService // Clase estática para gestionar contraseñas
    {
        // El parámetro TUser no se usa en la implementación por defecto de PasswordHasher,
        // solo sirve como punto de extensión para hashers personalizados. Se pasa null!
        // porque acá no hace falta ese contexto.
        private static readonly PasswordHasher<Usuario> _hasher = new();

        // Método para generar el hash de una contraseña usando PBKDF2 (vía PasswordHasher)
        public static string HashPassword(string password)
        {
            // Valida que la contraseña no sea nula o vacía
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("La contraseña no puede estar vacía.");

            // Genera salt aleatorio + hash PBKDF2 y devuelve todo combinado en un solo string
            return _hasher.HashPassword(null!, password);
        }

        // Método para verificar si una contraseña coincide con un hash almacenado
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(password))
                return false;

            try
            {
                // VerifyHashedPassword extrae el salt guardado dentro de hashedPassword,
                // vuelve a derivar el hash con la contraseña recibida y compara ambos.
                var resultado = _hasher.VerifyHashedPassword(null!, hashedPassword, password);

                // Success y SuccessRehashNeeded implican contraseña correcta; solo Failed no lo es.
                return resultado != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                // VerifyHashedPassword hace Convert.FromBase64String(hashedPassword) SIN
                // try/catch, así que lanza FormatException si el valor guardado no es
                // Base64 válido. Eso pasa con las contraseñas viejas en texto plano que
                // todavía quedan en la base (ej: "3011122" o "miClave123"). No es un error
                // de la aplicación: simplemente ese valor no es un hash, así que no
                // verifica. Sin este catch, el login devuelve un 500 en vez de rechazar
                // las credenciales.
                return false;
            }
        }
    }
}