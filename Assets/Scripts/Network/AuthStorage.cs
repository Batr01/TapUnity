using System;
using Newtonsoft.Json;
using TapBrawl.Models;
using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>Сохранение сессии: Keychain (iOS), Android Keystore, AES в Editor.</summary>
    public static class AuthStorage
    {
        private const string KeyAccess = "tb.auth.access";
        private const string KeyRefresh = "tb.auth.refresh";
        private const string KeyExpiresUnix = "tb.auth.expires_unix";
        private const string KeyPlayerJson = "tb.auth.player_json";
        private const string KeyDeviceId = "tb.device_id";

        public static string GetOrCreateDeviceId()
        {
            var id = ReadString(KeyDeviceId);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                WriteString(KeyDeviceId, id);
            }
            return id;
        }

        public static bool TryLoad(out AuthSession? session)
        {
            session = null;
            var access = ReadString(KeyAccess);
            var refresh = ReadString(KeyRefresh);
            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh))
                return false;

            var expiresUnix = ReadString(KeyExpiresUnix);
            if (!long.TryParse(expiresUnix, out var unix))
                return false;

            var playerJson = ReadString(KeyPlayerJson);
            if (string.IsNullOrEmpty(playerJson))
                return false;

            PlayerProfileDto? player;
            try
            {
                player = JsonConvert.DeserializeObject<PlayerProfileDto>(playerJson);
            }
            catch
            {
                return false;
            }

            if (player == null)
                return false;

            session = new AuthSession
            {
                AccessToken = access,
                RefreshToken = refresh,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(unix),
                Player = player,
            };
            return true;
        }

        public static void Save(AuthSession s)
        {
            WriteString(KeyAccess, s.AccessToken);
            WriteString(KeyRefresh, s.RefreshToken);
            WriteString(KeyExpiresUnix, s.AccessTokenExpiresAt.ToUnixTimeSeconds().ToString());
            WriteString(KeyPlayerJson, JsonConvert.SerializeObject(s.Player));
        }

        public static void Clear()
        {
            SecureStorage.DeleteKey(KeyAccess);
            SecureStorage.DeleteKey(KeyRefresh);
            SecureStorage.DeleteKey(KeyExpiresUnix);
            SecureStorage.DeleteKey(KeyPlayerJson);

            DeleteLegacyPlaintext(KeyAccess);
            DeleteLegacyPlaintext(KeyRefresh);
            DeleteLegacyPlaintext(KeyExpiresUnix);
            DeleteLegacyPlaintext(KeyPlayerJson);
        }

        private static string ReadString(string key)
        {
            if (SecureStorage.ContainsKey(key))
                return SecureStorage.GetString(key);

            var legacy = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(legacy))
                return string.Empty;

            if (SecureStorage.TrySetString(key, legacy))
                DeleteLegacyPlaintext(key);

            return legacy;
        }

        private static void WriteString(string key, string value)
        {
            if (!SecureStorage.TrySetString(key, value))
            {
                Debug.LogWarning($"AuthStorage: не удалось сохранить ключ {key} в secure storage.");
                return;
            }

            DeleteLegacyPlaintext(key);
        }

        private static void DeleteLegacyPlaintext(string key)
        {
            if (!PlayerPrefs.HasKey(key))
                return;

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}

