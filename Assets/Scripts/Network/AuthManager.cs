using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Models;

namespace TapBrawl.Network
{
    /// <summary>
    /// Сессия: восстановление по refresh, вход email/Google/Apple/гость.
    /// </summary>
    public sealed class AuthManager
    {
        private readonly ApiClient _api;

        public AuthManager(ApiClient api) => _api = api ?? throw new ArgumentNullException(nameof(api));

        /// <summary>Восстановить сессию из secure storage и при необходимости обновить access-токен.</summary>
        /// <returns>true, если сессия готова (в т.ч. после refresh).</returns>
        public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
        {
            if (!AuthStorage.TryLoad(out var stored) || stored == null)
                return false;

            if (stored.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(60))
            {
                var r = await _api.AuthRefreshAsync(stored.RefreshToken, ct).ConfigureAwait(true);
                if (!r.Success || r.Data == null)
                {
                    AuthStorage.Clear();
                    return false;
                }

                ApplyResponse(r.Data);
                return true;
            }

            AuthContext.Current = stored;
            return true;
        }

        public async Task RegisterEmailAsync(string email, string password, string? username, CancellationToken ct = default)
        {
            var r = await _api.AuthEmailRegisterAsync(email, password, username, ct).ConfigureAwait(true);
            if (!r.Success || r.Data == null)
                throw BuildAuthException("Регистрация", r);
            ApplyResponse(r.Data);
        }

        public async Task LoginEmailAsync(string email, string password, CancellationToken ct = default)
        {
            var r = await _api.AuthEmailLoginAsync(email, password, ct).ConfigureAwait(true);
            if (!r.Success || r.Data == null)
                throw BuildAuthException("Вход", r);
            ApplyResponse(r.Data);
        }

        public async Task GuestLoginAsync(CancellationToken ct = default)
        {
            var deviceId = AuthStorage.GetOrCreateDeviceId();
            var r = await _api.AuthGuestAsync(deviceId, ct).ConfigureAwait(true);
            if (!r.Success || r.Data == null)
                throw BuildAuthException("Гостевой вход", r);
            ApplyResponse(r.Data);
        }

        public async Task SignInWithGoogleAsync(string webClientId, string? iosClientId, CancellationToken ct = default)
        {
            var idToken = await GoogleSignInBridge.RequestIdTokenAsync(webClientId, iosClientId, ct).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(idToken))
            {
                var details = GoogleSignInBridge.LastError;
                if (!string.IsNullOrWhiteSpace(details))
                    throw new InvalidOperationException("Не удалось получить Google id_token: " + details);
                throw new InvalidOperationException("Не удалось получить Google id_token.");
            }

            var r = await _api.AuthGoogleAsync(idToken, ct).ConfigureAwait(true);
            if (!r.Success || r.Data == null)
                throw BuildAuthException("Google", r);
            ApplyResponse(r.Data);
        }

        public async Task SignInWithAppleAsync(CancellationToken ct = default)
        {
            var token = await AppleSignInBridge.RequestIdentityTokenAsync(ct).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Не удалось получить Apple identity token.");

            var r = await _api.AuthAppleAsync(token, ct).ConfigureAwait(true);
            if (!r.Success || r.Data == null)
                throw BuildAuthException("Apple", r);
            ApplyResponse(r.Data);
        }

        public static void Logout()
        {
            AuthStorage.Clear();
            AuthContext.Current = null;
        }

        private static Exception BuildAuthException<T>(string label, ApiResult<T> r)
        {
            var body = string.IsNullOrWhiteSpace(r.ErrorBody) ? string.Empty : " " + r.ErrorBody;
            return new InvalidOperationException($"{label} не удался: HTTP {r.StatusCode}.{body}");
        }

        private static void ApplyResponse(AuthResponseDto dto)
        {
            var session = AuthSession.FromResponse(dto);
            AuthContext.Current = session;
            AuthStorage.Save(session);
        }
    }

    /// <summary>Текущая сессия в памяти (после Boot/Auth). Лобби читает отсюда.</summary>
    public static class AuthContext
    {
        public static AuthSession? Current { get; set; }
    }
}
