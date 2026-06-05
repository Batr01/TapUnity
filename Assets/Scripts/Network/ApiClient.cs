using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapBrawl.Models;
using UnityEngine.Networking;

namespace TapBrawl.Network
{
    public readonly struct ApiResult<T>
    {
        public bool Success { get; }
        public int StatusCode { get; }
        public T? Data { get; }
        public string? ErrorBody { get; }

        public ApiResult(bool success, int statusCode, T? data, string? errorBody)
        {
            Success = success;
            StatusCode = statusCode;
            Data = data;
            ErrorBody = errorBody;
        }
    }

    /// <summary>REST-вызовы к TapBrawl API (UnityWebRequest).</summary>
    public sealed class ApiClient
    {
        private readonly string _baseUrl;
        private readonly BackendConfig _config;

        public ApiClient(BackendConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _config = config;
            _baseUrl = config.BaseUrl;
        }

        public Task<ApiResult<AuthResponseDto>> AuthGuestAsync(string deviceId, CancellationToken ct = default)
        {
            var body = new GuestAuthRequest { DeviceId = deviceId };
            return PostJsonAsync<AuthResponseDto>("/api/v1/auth/guest", body, bearer: null, ct);
        }

        public Task<ApiResult<AuthResponseDto>> AuthRefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var body = new RefreshRequest { RefreshToken = refreshToken };
            return PostJsonAsync<AuthResponseDto>("/api/v1/auth/refresh", body, bearer: null, ct);
        }

        public Task<ApiResult<PlayerProfileDto>> PlayersMeAsync(string bearer, CancellationToken ct = default) =>
            GetJsonAsync<PlayerProfileDto>("/api/v1/players/me", bearer, ct);

        public Task<ApiResult<PlayerProfileDto>> PlayersPutMeAsync(
            string bearer,
            UpdatePlayerProfileRequest body,
            CancellationToken ct = default) =>
            PutJsonAsync<PlayerProfileDto>("/api/v1/players/me", body, bearer, ct);

        public Task<ApiResult<PlayerSkillsStateDto>> PlayersMeSkillsAsync(string bearer, CancellationToken ct = default) =>
            GetJsonAsync<PlayerSkillsStateDto>("/api/v1/players/me/skills", bearer, ct);

        public Task<ApiResult<PlayerSkillsStateDto>> PlayersMeSkillsUpgradeAsync(
            string bearer,
            int skillId,
            CancellationToken ct = default) =>
            PostJsonAsync<PlayerSkillsStateDto>(
                "/api/v1/players/me/skills/upgrade",
                new UpgradePlayerSkillRequestDto { SkillId = skillId },
                bearer,
                ct);

        public Task<ApiResult<PlayerSkillsStateDto>> PlayersMeSkillsLoadoutAsync(
            string bearer,
            IReadOnlyList<int> skillIdsInSlotOrder,
            CancellationToken ct = default) =>
            PutJsonAsync<PlayerSkillsStateDto>(
                "/api/v1/players/me/skills/loadout",
                new SetPlayerSkillLoadoutRequestDto { SkillIds = new System.Collections.Generic.List<int>(skillIdsInSlotOrder) },
                bearer,
                ct);

        public Task<ApiResult<PlayerPublicStatsDto>> PlayersStatsAsync(Guid playerId, CancellationToken ct = default) =>
            GetJsonAsync<PlayerPublicStatsDto>($"/api/v1/players/{playerId:D}/stats", null, ct);

        public Task<ApiResult<AuthResponseDto>> AuthRegisterAsync(string deviceId, string? username, CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/register",
                new RegisterGuestRequest { DeviceId = deviceId, Username = username },
                bearer: null,
                ct);

        public Task<ApiResult<AuthResponseDto>> AuthLoginAsync(string deviceId, CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/login",
                new LoginGuestRequest { DeviceId = deviceId },
                bearer: null,
                ct);

        public Task<ApiResult<AuthResponseDto>> AuthEmailRegisterAsync(
            string email,
            string password,
            string? username,
            CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/email/register",
                new EmailRegisterRequest { Email = email, Password = password, Username = username },
                bearer: null,
                ct);

        public Task<ApiResult<AuthResponseDto>> AuthEmailLoginAsync(string email, string password, CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/email/login",
                new EmailLoginRequest { Email = email, Password = password },
                bearer: null,
                ct);

        public Task<ApiResult<AuthResponseDto>> AuthGoogleAsync(string idToken, CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/google",
                new GoogleAuthRequest { IdToken = idToken },
                bearer: null,
                ct);

        public Task<ApiResult<AuthResponseDto>> AuthAppleAsync(string identityToken, CancellationToken ct = default) =>
            PostJsonAsync<AuthResponseDto>(
                "/api/v1/auth/apple",
                new AppleAuthRequest { IdentityToken = identityToken },
                bearer: null,
                ct);

        public Task<ApiResult<SubmitMatchStatsResponseDto>> MatchesSubmitMyStatsAsync(
            string bearer,
            Guid matchId,
            SubmitMyMatchStatsBody body,
            CancellationToken ct = default) =>
            PostJsonAsync<SubmitMatchStatsResponseDto>(
                $"/api/v1/matches/{matchId:D}/submit-my-stats",
                body,
                bearer,
                ct);

        public Task<ApiResult<MatchResultResponseDto>> MatchesResultAsync(Guid matchId, CancellationToken ct = default) =>
            GetJsonAsync<MatchResultResponseDto>($"/api/v1/matches/{matchId:D}/result", null, ct);

        public Task<ApiResult<MatchmakingStatusDto>> MatchmakingStatusAsync(string bearer, CancellationToken ct = default) =>
            GetJsonAsync<MatchmakingStatusDto>("/api/v1/matchmaking/status", bearer, ct);

        public Task<ApiResult<ClientConfigDto>> GetConfigAsync(CancellationToken ct = default) =>
            GetJsonAsync<ClientConfigDto>("/api/v1/config", null, ct);

        public async Task<ApiResult<T>> GetJsonAsync<T>(string path, string bearer, CancellationToken ct = default)
        {
            await ApplyDevRequestDelayAsync(ct);
            var url = _baseUrl + path;
            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(bearer))
                req.SetRequestHeader("Authorization", "Bearer " + bearer);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            var code = (int)req.responseCode;
            var text = req.downloadHandler?.text ?? string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
                return new ApiResult<T>(false, code, default, BuildNetworkError(req, text));

            if (code is < 200 or >= 300)
            {
                T? empty = default;
                return new ApiResult<T>(false, code, empty, text);
            }

            try
            {
                var data = JsonConvert.DeserializeObject<T>(text);
                return new ApiResult<T>(true, code, data, null);
            }
            catch (Exception ex)
            {
                return new ApiResult<T>(false, code, default, ex.Message);
            }
        }

        public async Task<ApiResult<T>> PostJsonAsync<T>(
            string path,
            object body,
            string? bearer,
            CancellationToken ct = default)
        {
            await ApplyDevRequestDelayAsync(ct);
            var url = _baseUrl + path;
            var json = JsonConvert.SerializeObject(body);
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(bearer))
                req.SetRequestHeader("Authorization", "Bearer " + bearer);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            var code = (int)req.responseCode;
            var text = req.downloadHandler?.text ?? string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
                return new ApiResult<T>(false, code, default, BuildNetworkError(req, text));

            if (code is < 200 or >= 300)
            {
                T? empty = default;
                return new ApiResult<T>(false, code, empty, text);
            }

            try
            {
                var data = JsonConvert.DeserializeObject<T>(text);
                return new ApiResult<T>(true, code, data, null);
            }
            catch (Exception ex)
            {
                return new ApiResult<T>(false, code, default, ex.Message);
            }
        }

        public async Task<ApiResult<T>> PutJsonAsync<T>(
            string path,
            object body,
            string bearer,
            CancellationToken ct = default)
        {
            await ApplyDevRequestDelayAsync(ct);
            var url = _baseUrl + path;
            var json = JsonConvert.SerializeObject(body);
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(bearer))
                req.SetRequestHeader("Authorization", "Bearer " + bearer);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            var code = (int)req.responseCode;
            var text = req.downloadHandler?.text ?? string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
                return new ApiResult<T>(false, code, default, BuildNetworkError(req, text));

            if (code is < 200 or >= 300)
            {
                T? empty = default;
                return new ApiResult<T>(false, code, empty, text);
            }

            try
            {
                var data = JsonConvert.DeserializeObject<T>(text);
                return new ApiResult<T>(true, code, data, null);
            }
            catch (Exception ex)
            {
                return new ApiResult<T>(false, code, default, ex.Message);
            }
        }

        // TODO: убрать после теста LoadingOverlay.
        private Task ApplyDevRequestDelayAsync(CancellationToken ct)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var ms = _config.DevSimulateRequestDelayMs;
            if (ms > 0)
                return Task.Delay(ms, ct);
#endif
            return Task.CompletedTask;
        }

        private static string BuildNetworkError(UnityWebRequest req, string responseText)
        {
            var transport = req.error ?? string.Empty;
            if (string.IsNullOrWhiteSpace(responseText))
                return transport;
            if (string.IsNullOrWhiteSpace(transport))
                return responseText;
            return transport + " | " + responseText;
        }
    }
}
