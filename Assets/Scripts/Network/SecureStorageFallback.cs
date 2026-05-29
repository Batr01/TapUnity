using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>AES-шифрование для Editor/десктопа, где нет Keychain/Keystore.</summary>
    internal static class SecureStorageFallback
    {
        private const string KeyDataEncryptionKey = "tb.secure.fallback_dek";

        public static bool ContainsKey(string key) =>
            PlayerPrefs.HasKey(PrefKey(key));

        public static string GetString(string key, string defaultValue)
        {
            var stored = PlayerPrefs.GetString(PrefKey(key), string.Empty);
            if (string.IsNullOrEmpty(stored))
                return defaultValue;

            try
            {
                return Decrypt(stored);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SecureStorageFallback: не удалось расшифровать ключ {key}: {ex.Message}");
                return defaultValue;
            }
        }

        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(PrefKey(key), Encrypt(value));
            PlayerPrefs.Save();
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(PrefKey(key));
            PlayerPrefs.Save();
        }

        private static string PrefKey(string key) => "tb.secure.enc." + key;

        private static byte[] GetOrCreateKey()
        {
            var encoded = PlayerPrefs.GetString(KeyDataEncryptionKey, string.Empty);
            if (!string.IsNullOrEmpty(encoded))
                return Convert.FromBase64String(encoded);

            var key = new byte[32];
            RandomNumberGenerator.Create().GetBytes(key);
            PlayerPrefs.SetString(KeyDataEncryptionKey, Convert.ToBase64String(key));
            PlayerPrefs.Save();
            return key;
        }

        private static string Encrypt(string plainText)
        {
            var key = GetOrCreateKey();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var iv = new byte[16];
            RandomNumberGenerator.Create().GetBytes(iv);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var payload = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, iv.Length, cipherBytes.Length);
            return Convert.ToBase64String(payload);
        }

        private static string Decrypt(string encoded)
        {
            var payload = Convert.FromBase64String(encoded);
            if (payload.Length <= 16)
                throw new CryptographicException("Invalid encrypted payload.");

            var iv = new byte[16];
            var cipherBytes = new byte[payload.Length - 16];
            Buffer.BlockCopy(payload, 0, iv, 0, 16);
            Buffer.BlockCopy(payload, 16, cipherBytes, 0, cipherBytes.Length);

            using var aes = Aes.Create();
            aes.Key = GetOrCreateKey();
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
