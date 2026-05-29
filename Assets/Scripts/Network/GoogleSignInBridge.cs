using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace TapBrawl.Network
{
    /// <summary>
    /// Android Google Sign-In bridge for Unity 6 without google-signin-unity package.
    /// </summary>
    public static class GoogleSignInBridge
    {
        public static string? LastError { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        private const string JavaBridgeClass = "com.tapbrawl.auth.GoogleSignInBridge";
        private const string CallbackGameObjectName = "__GoogleSignInCallbacks";
        private const string SuccessMethodName = nameof(GoogleSignInCallbacks.OnGoogleSignInSuccess);
        private const string ErrorMethodName = nameof(GoogleSignInCallbacks.OnGoogleSignInError);

        private static readonly object Sync = new();
        private static TaskCompletionSource<string?>? _pendingSignIn;
        private static GoogleSignInCallbacks? _callbacks;
#endif

        public static Task<string?> RequestIdTokenAsync(string webClientId, string? iosClientId, CancellationToken ct)
        {
            _ = iosClientId;

#if !UNITY_ANDROID || UNITY_EDITOR
            Debug.LogWarning("[GoogleSignIn] Доступно только в Android билдах.");
            return Task.FromResult<string?>(null);
#else
            if (string.IsNullOrWhiteSpace(webClientId))
            {
                LastError = "В BackendConfig не задан Google Web Client Id.";
                Debug.LogError("[GoogleSignIn] В BackendConfig не задан Google Web Client Id.");
                return Task.FromResult<string?>(null);
            }

            lock (Sync)
            {
                if (_pendingSignIn != null && !_pendingSignIn.Task.IsCompleted)
                {
                    LastError = "Вход уже выполняется.";
                    Debug.LogWarning("[GoogleSignIn] Вход уже выполняется.");
                    return Task.FromResult<string?>(null);
                }

                EnsureCallbacks();
                LastError = null;
                _pendingSignIn = new TaskCompletionSource<string?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    TaskCompletionSource<string?>? tcs = null;
                    lock (Sync)
                    {
                        if (_pendingSignIn != null && !_pendingSignIn.Task.IsCompleted)
                        {
                            tcs = _pendingSignIn;
                            _pendingSignIn = null;
                        }
                    }

                    tcs?.TrySetCanceled(ct);
                });
            }

            try
            {
                using var bridge = new AndroidJavaClass(JavaBridgeClass);
                bridge.CallStatic(
                    "signIn",
                    webClientId.Trim(),
                    CallbackGameObjectName,
                    SuccessMethodName,
                    ErrorMethodName);
            }
            catch (Exception ex)
            {
                CompleteWithError($"Не удалось вызвать Android bridge: {ex.Message}");
            }

            lock (Sync)
            {
                return _pendingSignIn?.Task ?? Task.FromResult<string?>(null);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void EnsureCallbacks()
        {
            if (_callbacks != null)
                return;

            var go = GameObject.Find(CallbackGameObjectName);
            if (go == null)
                go = new GameObject(CallbackGameObjectName);

            UnityEngine.Object.DontDestroyOnLoad(go);
            _callbacks = go.GetComponent<GoogleSignInCallbacks>();
            if (_callbacks == null)
                _callbacks = go.AddComponent<GoogleSignInCallbacks>();
        }

        private static void CompleteWithSuccess(string idToken)
        {
            TaskCompletionSource<string?>? tcs = null;
            lock (Sync)
            {
                if (_pendingSignIn != null)
                {
                    tcs = _pendingSignIn;
                    _pendingSignIn = null;
                }
            }

            if (string.IsNullOrWhiteSpace(idToken))
            {
                LastError = "id_token пустой. Проверьте Web Client Id и SHA-1.";
                tcs?.TrySetResult(null);
                Debug.LogWarning("[GoogleSignIn] id_token пустой. Проверьте Web Client Id и SHA-1.");
                return;
            }

            LastError = null;
            tcs?.TrySetResult(idToken.Trim());
        }

        private static void CompleteWithError(string message)
        {
            TaskCompletionSource<string?>? tcs = null;
            lock (Sync)
            {
                if (_pendingSignIn != null)
                {
                    tcs = _pendingSignIn;
                    _pendingSignIn = null;
                }
            }

            if (string.IsNullOrWhiteSpace(message))
                message = "Google Sign-In завершился с ошибкой.";

            LastError = message;
            Debug.LogWarning("[GoogleSignIn] " + message);
            tcs?.TrySetResult(null);
        }

        [Preserve]
        private sealed class GoogleSignInCallbacks : MonoBehaviour
        {
            [Preserve]
            public void OnGoogleSignInSuccess(string idToken)
            {
                CompleteWithSuccess(idToken);
            }

            [Preserve]
            public void OnGoogleSignInError(string error)
            {
                CompleteWithError(error);
            }
        }
#endif
    }
}
