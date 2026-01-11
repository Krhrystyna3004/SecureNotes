using System;
using System.Security.Cryptography;
using System.Text;

namespace SecureNotes
{
    public static class CryptoService
    {
        public static string GenerateSalt(int bytes = 16)
        {
            var salt = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public static string HashWithPBKDF2(string value, string saltBase64, int iterations = 100_000, int bytes = 32)
        {
            var salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(value, salt, iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(bytes));
            }
        }

        public static byte[] DeriveKeyFromPin(string pin, string saltBase64, int iterations = 150_000, int bytes = 32)
        {
            var salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(bytes);
            }
        }

        public static (string ivBase64, string cipherBase64) EncryptAes(string plain, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var enc = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plain);
                    var cipher = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return (Convert.ToBase64String(aes.IV), Convert.ToBase64String(cipher));
                }
            }
        }

        public static string DecryptAes(string ivBase64, string cipherBase64, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Convert.FromBase64String(ivBase64);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var dec = aes.CreateDecryptor())
                {
                    var cipher = Convert.FromBase64String(cipherBase64);
                    var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(plain);
                }
            }
        }
    }
}