using System;
using System.Security.Cryptography;
using System.Text;

namespace BOTGC.MembershipApplication.Common
{
    public static class HmacHelper
    {
        /// <summary>
        /// Generates a SHA-256 HMAC hash as a lowercase hex string.
        /// </summary>
        /// <param name="secret">The secret key used for hashing.</param>
        /// <param name="message">The message to hash (e.g., participant email).</param>
        /// <returns>The SHA-256 HMAC hash as a lowercase hex string.</returns>
        public static string GenerateSha256Hmac(string secret, string message)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

            return BitConverter.ToString(hashBytes)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }
}
