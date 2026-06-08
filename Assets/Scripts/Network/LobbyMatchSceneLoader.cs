using System;
using System.Threading;
using TapBrawl.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using SynchronizationContext = System.Threading.SynchronizationContext;

namespace TapBrawl.Network
{
    public static class LobbyMatchSceneLoader
    {
        public static void LoadFromMatchFound(
            MatchFoundDto dto,
            MatchHubClient? hub,
            string matchSceneName,
            SynchronizationContext? uiSyncContext,
            Action<string>? setStatus = null,
            Action? onLeavingToMatch = null)
        {
            void Load()
            {
                if (hub == null)
                {
                    setStatus?.Invoke("Ошибка: соединение с хабом потеряно.");
                    Debug.LogError("[Lobby] MatchFound при hub == null.");
                    return;
                }

                setStatus?.Invoke($"Матч! {dto.OpponentUsername}, загрузка…");
                Debug.Log($"[Match] id={dto.MatchId} seed={dto.Seed} duration={dto.DurationSec}s");

                PendingOnlineMatch.SetFromDto(dto);

                var holder = MatchConnectionHolder.Ensure();
                holder.AttachHub(hub);
                onLeavingToMatch?.Invoke();

                if (!string.IsNullOrEmpty(matchSceneName))
                    SceneManager.LoadScene(matchSceneName, LoadSceneMode.Single);
            }

            var ctx = uiSyncContext ?? SynchronizationContext.Current;
            if (ctx != null)
                ctx.Post(_ => Load(), null);
            else
                Load();
        }
    }
}
