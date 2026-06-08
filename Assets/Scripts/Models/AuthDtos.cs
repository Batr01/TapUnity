using System;
using Newtonsoft.Json;

namespace TapBrawl.Models
{
    [Serializable]
    public sealed class GuestAuthRequest
    {
        [JsonProperty("deviceId")] public string DeviceId { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RefreshRequest
    {
        [JsonProperty("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RegisterGuestRequest
    {
        [JsonProperty("deviceId")] public string DeviceId { get; set; } = string.Empty;
        [JsonProperty("username")] public string? Username { get; set; }
    }

    [Serializable]
    public sealed class LoginGuestRequest
    {
        [JsonProperty("deviceId")] public string DeviceId { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class EmailRegisterRequest
    {
        [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        [JsonProperty("password")] public string Password { get; set; } = string.Empty;
        [JsonProperty("username")] public string? Username { get; set; }
    }

    [Serializable]
    public sealed class EmailLoginRequest
    {
        [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        [JsonProperty("password")] public string Password { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class GoogleAuthRequest
    {
        [JsonProperty("idToken")] public string IdToken { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class AppleAuthRequest
    {
        [JsonProperty("identityToken")] public string IdentityToken { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class UpdatePlayerProfileRequest
    {
        [JsonProperty("username")] public string? Username { get; set; }
        [JsonProperty("avatarId")] public string? AvatarId { get; set; }
    }

    public sealed class PlayerPublicStatsDto
    {
        [JsonProperty("id")] public Guid Id { get; set; }
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("rankPoints")] public int RankPoints { get; set; }
        [JsonProperty("tier")] public string Tier { get; set; } = string.Empty;
        [JsonProperty("wins")] public int Wins { get; set; }
        [JsonProperty("losses")] public int Losses { get; set; }
        [JsonProperty("totalTaps")] public long TotalTaps { get; set; }
        [JsonProperty("bestStreak")] public int BestStreak { get; set; }
        [JsonProperty("avgReactionMs")] public double? AvgReactionMs { get; set; }
    }

    [Serializable]
    public sealed class AuthResponseDto
    {
        [JsonProperty("accessToken")] public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("accessTokenExpiresAt")] public DateTimeOffset AccessTokenExpiresAt { get; set; }
        [JsonProperty("player")] public PlayerProfileDto Player { get; set; } = new();
    }

    /// <summary>Текущая сессия после успешного guest/refresh.</summary>
    [Serializable]
    public sealed class AuthSession
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
        public PlayerProfileDto Player { get; set; } = new();

        public static AuthSession FromResponse(AuthResponseDto dto) => new()
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            AccessTokenExpiresAt = dto.AccessTokenExpiresAt,
            Player = dto.Player,
        };
    }
}
