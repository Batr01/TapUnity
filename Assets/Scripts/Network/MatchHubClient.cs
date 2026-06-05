using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using TapBrawl.Models;
using UnityEngine;

namespace TapBrawl.Network
{
    /// <summary>
    /// Клиент SignalR для <c>/hubs/match</c>. Требует NuGet-пакет <b>Microsoft.AspNetCore.SignalR.Client</b> (см. Docs/Unity/06-SIGNALR-CLIENT-UNITY.md).
    /// </summary>
    public sealed class MatchHubClient : IDisposable
    {
        /// <summary>Полезная нагрузка <c>QueueJoined</c> с сервера (STJ). Не использовать <see cref="System.Text.Json.JsonElement"/> на IL2CPP.</summary>
        [Serializable]
        private sealed class QueueJoinedPayload
        {
            [JsonPropertyName("mode")]
            public string? Mode { get; set; }
        }

        private HubConnection? _hub;

        public event Action? QueueJoined;
        public event Action<MatchFoundDto>? MatchFound;
        public event Action<OpponentVisualSkillDto>? OpponentVisualSkill;
        public event Action<PlayerPingDto>? PlayerPing;
        public event Action<MatchResultResponseDto>? MatchResultReady;

        public bool IsConnected => _hub?.State == HubConnectionState.Connected;

        /// <param name="baseUrl">Тот же, что в BackendConfig (например http://localhost:5088)</param>
        /// <param name="accessToken">JWT access token без префикса Bearer</param>
        public async Task ConnectAsync(string baseUrl, string accessToken, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/match";
            // Не вызываем AddJsonProtocol: в части Unity-сборок расширение не попадает в ссылки (CS1061).
            // HubConnectionBuilder по умолчанию подключает JSON-протокол с настройками веб-дефолта.
            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(accessToken);
                })
                .Build();

            _hub.On<QueueJoinedPayload>(HubEvents.QueueJoined, _ => { QueueJoined?.Invoke(); });

            _hub.On<MatchFoundDto>(HubEvents.MatchFound, dto =>
            {
                if (dto != null)
                    MatchFound?.Invoke(dto);
            });

            _hub.On<OpponentVisualSkillDto>(HubEvents.OpponentVisualSkill, dto =>
            {
                if (dto != null)
                    OpponentVisualSkill?.Invoke(dto);
            });

            _hub.On<PlayerPingDto>(HubEvents.PlayerPing, dto =>
            {
                if (dto != null)
                    PlayerPing?.Invoke(dto);
            });

            _hub.On<MatchResultResponseDto>(HubEvents.MatchResultReady, dto =>
            {
                if (dto != null)
                    MatchResultReady?.Invoke(dto);
            });

            _hub.Closed += ex =>
            {
                if (ex != null)
                    Debug.LogWarning("[MatchHub] Closed: " + ex.Message);
                return Task.CompletedTask;
            };

            await _hub.StartAsync(cancellationToken);
        }

        public Task JoinQueue1v1Async(CancellationToken cancellationToken = default)
        {
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            return _hub.InvokeCoreAsync(HubMethods.JoinQueue, new object[] { "1v1" }, cancellationToken);
        }

        public Task LeaveQueueAsync(CancellationToken cancellationToken = default)
        {
            if (_hub == null)
                return Task.CompletedTask;
            return _hub.InvokeAsync(HubMethods.LeaveQueue, cancellationToken);
        }

        /// <summary>Типы 2 и 3 — визуальные дебаффы сопернику (см. <see cref="TapBrawl.Core.MatchSkillIds"/>).</summary>
        public Task SendOpponentVisualSkillAsync(Guid matchId, int skillType, CancellationToken cancellationToken = default)
        {
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            return _hub.InvokeAsync(HubMethods.SendOpponentVisualSkill, matchId, skillType, cancellationToken);
        }

        public Task SendPingAsync(Guid matchId, int pingType, CancellationToken cancellationToken = default)
        {
            if (_hub == null)
                throw new InvalidOperationException("Hub не подключён.");
            return _hub.InvokeAsync(HubMethods.SendPing, matchId, pingType, cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            if (_hub == null)
                return;
            try
            {
                await _hub.StopAsync();
                await _hub.DisposeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MatchHub] Disconnect: " + ex.Message);
            }
            _hub = null;
        }

        public void Dispose() => _ = DisconnectAsync();

        private static class HubEvents
        {
            public const string QueueJoined = "QueueJoined";
            public const string MatchFound = "MatchFound";
            public const string OpponentVisualSkill = "OpponentVisualSkill";
            public const string PlayerPing = "PlayerPing";
            public const string MatchResultReady = "MatchResultReady";
        }

        private static class HubMethods
        {
            public const string JoinQueue = "JoinQueue";
            public const string LeaveQueue = "LeaveQueue";
            public const string SendOpponentVisualSkill = "SendOpponentVisualSkill";
            public const string SendPing = "SendPing";
        }
    }
}
