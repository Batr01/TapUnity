using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>
    /// Вызов com.lupidan.apple-signin-unity через рефлексию + обязательный <see cref="Tick"/> из Update.
    /// Добавьте пакет: <c>https://github.com/lupidan/apple-signin-unity.git?path=/Source</c>.
    /// </summary>
    public static class AppleSignInBridge
    {
        private static object? _manager;

        public static void Tick()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_manager == null)
                return;
            _manager.GetType().GetMethod("Update", BindingFlags.Public | BindingFlags.Instance)?.Invoke(_manager, null);
#endif
        }

        public static async Task<string?> RequestIdentityTokenAsync(CancellationToken ct)
        {
#if !UNITY_IOS || UNITY_EDITOR
            Debug.LogWarning("[AppleSignIn] Доступно только на устройстве iOS (не в Editor).");
            return null;
#else
            var mgrType = FindType("AppleAuth.AppleAuthManager");
            var deserType = FindType("AppleAuth.Native.PayloadDeserializer");
            var loginArgsType = FindType("AppleAuth.AppleAuthLoginArgs");
            var loginOptionsType = FindType("AppleAuth.Enums.LoginOptions");
            if (mgrType == null || deserType == null || loginArgsType == null || loginOptionsType == null)
            {
                Debug.LogError(
                    "[AppleSignIn] Пакет не найден. Добавьте com.lupidan.apple-signin-unity " +
                    "(https://github.com/lupidan/apple-signin-unity).");
                return null;
            }

            var supportedProp = mgrType.GetProperty("IsCurrentPlatformSupported", BindingFlags.Public | BindingFlags.Static);
            if (supportedProp?.GetValue(null) is not true)
            {
                Debug.LogWarning("[AppleSignIn] Платформа не поддерживает Sign in with Apple.");
                return null;
            }

            if (_manager == null)
            {
                var deser = Activator.CreateInstance(deserType);
                _manager = Activator.CreateInstance(mgrType, deser);
            }

            var tcs = new TaskCompletionSource<string?>();
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));

            var includeEmail = Enum.Parse(loginOptionsType, "IncludeEmail");
            var includeName = Enum.Parse(loginOptionsType, "IncludeFullName");
            var combined = Convert.ToInt32(includeEmail) | Convert.ToInt32(includeName);
            var loginOptions = Enum.ToObject(loginOptionsType, combined);

            var loginArgs = Activator.CreateInstance(loginArgsType, new[] { loginOptions, null, null });

            MethodInfo? loginMethod = null;
            foreach (var m in mgrType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.Name != "LoginWithAppleId")
                    continue;
                var ps = m.GetParameters();
                if (ps.Length != 3 || ps[0].ParameterType != loginArgsType)
                    continue;
                loginMethod = m;
                break;
            }

            if (loginMethod == null)
            {
                Debug.LogError("[AppleSignIn] Не найден метод LoginWithAppleId.");
                return null;
            }

            var successParamType = loginMethod.GetParameters()[1].ParameterType;
            var errorParamType = loginMethod.GetParameters()[2].ParameterType;

            var successDel = CreateActionDelegate(successParamType, credential =>
            {
                try
                {
                    var tokenProp = credential?.GetType().GetProperty("IdentityToken");
                    var raw = tokenProp?.GetValue(credential);
                    var token = raw switch
                    {
                        string s => s,
                        byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                        _ => null,
                    };
                    tcs.TrySetResult(string.IsNullOrWhiteSpace(token) ? null : token);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            var errorDel = CreateActionDelegate(errorParamType, _ => tcs.TrySetResult(null));

            loginMethod.Invoke(_manager, new[] { loginArgs, successDel, errorDel });

            try
            {
                while (!tcs.Task.IsCompleted)
                {
                    Tick();
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (tcs.Task.IsFaulted)
                {
                    Debug.LogWarning(tcs.Task.Exception?.GetBaseException().Message);
                    return null;
                }

                if (tcs.Task.IsCanceled)
                    return null;

                return tcs.Task.Result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static Delegate CreateActionDelegate(Type actionType, Action<object?> handler)
        {
            if (actionType.IsGenericType && actionType.GetGenericTypeDefinition() == typeof(Action<>))
            {
                var argType = actionType.GenericTypeArguments[0];
                var shim = typeof(AppleSignInBridge).GetMethod(nameof(Shim), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(argType);
                return (Delegate)shim.Invoke(null, new object[] { handler })!;
            }

            return (Action)(() => handler(null));
        }

        private static Action<T> Shim<T>(Action<object?> handler) => arg => handler(arg!);

        private static Type? FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? t = null;
                try
                {
                    t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                }
                catch
                {
                    // ignored
                }

                if (t != null)
                    return t;
            }

            return null;
        }
#endif
    }
}
