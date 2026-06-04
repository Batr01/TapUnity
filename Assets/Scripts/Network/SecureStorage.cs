using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>Keychain (iOS) / Android Keystore / AES fallback в Editor.</summary>
    public static class SecureStorage
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string Dll = "__Internal";

        [DllImport(Dll)]
        private static extern void _TapSecureStorageSet(string key, string value);

        [DllImport(Dll)]
        private static extern IntPtr _TapSecureStorageGet(string key);

        [DllImport(Dll)]
        private static extern void _TapSecureStorageDelete(string key);

        [DllImport(Dll)]
        private static extern bool _TapSecureStorageContainsKey(string key);

        [DllImport(Dll)]
        private static extern void _TapSecureStorageFree(IntPtr ptr);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const string JavaBridgeClass = "com.tapbrawl.auth.SecureStorageBridge";
#endif

        public static bool ContainsKey(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var bridge = new AndroidJavaClass(JavaBridgeClass);
            return bridge.CallStatic<bool>("containsKey", key);
#elif UNITY_IOS && !UNITY_EDITOR
            return _TapSecureStorageContainsKey(key);
#else
            return SecureStorageFallback.ContainsKey(key);
#endif
        }

        public static string GetString(string key, string defaultValue = "")
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var bridge = new AndroidJavaClass(JavaBridgeClass);
            return bridge.CallStatic<string>("getString", key, defaultValue) ?? defaultValue;
#elif UNITY_IOS && !UNITY_EDITOR
            var ptr = _TapSecureStorageGet(key);
            if (ptr == IntPtr.Zero)
                return defaultValue;

            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? defaultValue;
            }
            finally
            {
                _TapSecureStorageFree(ptr);
            }
#else
            return SecureStorageFallback.GetString(key, defaultValue);
#endif
        }

        public static bool TrySetString(string key, string value)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using var bridge = new AndroidJavaClass(JavaBridgeClass);
                return bridge.CallStatic<bool>("setString", key, value);
#elif UNITY_IOS && !UNITY_EDITOR
                _TapSecureStorageSet(key, value);
                return true;
#else
                SecureStorageFallback.SetString(key, value);
                return true;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SecureStorage.TrySetString failed for key={key}: {ex.Message}");
                return false;
            }
        }

        public static void SetString(string key, string value)
        {
            if (!TrySetString(key, value))
                Debug.LogWarning($"SecureStorage.SetString failed for key={key}");
        }

        public static void DeleteKey(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var bridge = new AndroidJavaClass(JavaBridgeClass);
            bridge.CallStatic("deleteKey", key);
#elif UNITY_IOS && !UNITY_EDITOR
            _TapSecureStorageDelete(key);
#else
            SecureStorageFallback.DeleteKey(key);
#endif
        }
    }
}
