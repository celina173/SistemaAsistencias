using Azure.Core;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Policy;

namespace ISFDyT124.Services
{
    public interface IEmailRateLimiter
    {
        bool IsAllowed(string email);
    }

    public class EmailRateLimiter : IEmailRateLimiter
    {
        private readonly IMemoryCache _cache;
        private const int MaxAttempts = 3;                               // Máximo 3 correos por email
        private static readonly TimeSpan Window = TimeSpan.FromHours(1); // En una ventana de 1 hora

        public EmailRateLimiter(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsAllowed(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            string normalizedEmail = email.Trim().ToLowerInvariant();
            string cacheKey = $"rate_limit_pwd_reset:{normalizedEmail}";

            if (_cache.TryGetValue(cacheKey, out int currentCount))
            {
                if (currentCount >= MaxAttempts)
                {
                    return false; // Límite superado: no se envía el email
                }

                _cache.Set(cacheKey, currentCount + 1, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = Window
                });
                return true;
            }

            // Primer intento
            _cache.Set(cacheKey, 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Window
            });

            return true;
        }

    }
}