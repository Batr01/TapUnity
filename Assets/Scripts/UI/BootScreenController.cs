using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Сцена Boot: восстановление сессии → Lobby; иначе → Auth.
    /// </summary>
    public sealed class BootScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private string authSceneName = "Auth";
        [SerializeField] private Text? statusText;
        [SerializeField] private UpdateRequiredModal? updateRequiredModal;

        private async void Start()
        {
            if (backendConfig == null)
            {
                LogStatus("BackendConfig не назначен.");
                return;
            }

            try
            {
                LogStatus("Проверка версии...");
                var api = new ApiClient(backendConfig);
                var versionCheck = await ClientVersionChecker.TryCheckAsync(api, CancellationToken.None)
                    .ConfigureAwait(true);
                if (versionCheck is { UpdateRequired: true } check)
                {
                    LogStatus("Требуется обновление.");
                    ShowUpdateRequired(check);
                    return;
                }

                LogStatus("Подключение...");
                var auth = new AuthManager(api);
                if (await auth.TryRestoreSessionAsync(CancellationToken.None).ConfigureAwait(true))
                {
                    LogStatus("Сессия найдена, загрузка лобби...");
                    if (!string.IsNullOrEmpty(lobbySceneName))
                        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
                    return;
                }

                LogStatus("Нужен вход…");
                if (!string.IsNullOrEmpty(authSceneName))
                    SceneManager.LoadScene(authSceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                LogStatus("Ошибка: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        private void ShowUpdateRequired(ClientVersionCheckResult check)
        {
            var modal = updateRequiredModal ?? FindFirstObjectByType<UpdateRequiredModal>(FindObjectsInactive.Include);
            if (modal == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                    modal = UpdateRequiredModal.EnsureOnCanvas(canvas.transform);
            }

            if (modal == null)
            {
                LogStatus($"Обновите приложение до {check.MinSupportedVersion}.");
                return;
            }

            modal.Show(check.MinSupportedVersion, check.CurrentVersion, check.StoreUrl);
        }

        private void LogStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log("[Boot] " + message);
        }
    }
}
