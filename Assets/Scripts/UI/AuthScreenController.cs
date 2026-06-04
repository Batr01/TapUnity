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
    /// Экран входа. UI настраивается в сцене Auth (Canvas + ссылки в инспекторе).
    /// Начальную разметку можно создать через меню Tap → Setup Auth Screen UI.
    /// </summary>
    public sealed class AuthScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("Панели")]
        [SerializeField] private GameObject? panelProviders;
        [SerializeField] private GameObject? panelLogin;
        [SerializeField] private GameObject? panelRegister;
        [SerializeField] private GameObject? busyOverlay;
        [SerializeField] private Text? errorText;

        [Header("Вход")]
        [SerializeField] private InputField? loginEmail;
        [SerializeField] private InputField? loginPassword;
        [SerializeField] private Button? loginSubmitButton;
        [SerializeField] private Button? loginBackButton;

        [Header("Регистрация")]
        [SerializeField] private InputField? regUsername;
        [SerializeField] private InputField? regEmail;
        [SerializeField] private InputField? regPassword;
        [SerializeField] private InputField? regPasswordConfirm;
        [SerializeField] private Button? registerSubmitButton;
        [SerializeField] private Button? registerBackButton;

        [Header("Провайдеры")]
        [SerializeField] private Button? registerNavButton;
        [SerializeField] private Button? loginNavButton;
        [SerializeField] private Button? googleButton;
        [SerializeField] private Button? appleButton;
        [SerializeField] private Button? guestButton;

        private ApiClient? _api;
        private AuthManager? _auth;

        private void Awake()
        {
            if (backendConfig == null)
            {
                Debug.LogError("[Auth] BackendConfig не назначен.");
                return;
            }

            _api = new ApiClient(backendConfig);
            _auth = new AuthManager(_api);
        }

        private void Start()
        {
            WireButtons();
#if !UNITY_IOS || UNITY_EDITOR
            if (appleButton != null)
                appleButton.gameObject.SetActive(false);
#endif
            ShowProviders();
        }

        private void Update() => AppleSignInBridge.Tick();

        private void WireButtons()
        {
            if (registerNavButton != null) registerNavButton.onClick.AddListener(ShowRegister);
            if (loginNavButton != null) loginNavButton.onClick.AddListener(ShowLogin);
            if (googleButton != null) googleButton.onClick.AddListener(OnGoogleClicked);
            if (appleButton != null) appleButton.onClick.AddListener(OnAppleClicked);
            if (guestButton != null) guestButton.onClick.AddListener(OnGuestClicked);
            if (loginSubmitButton != null) loginSubmitButton.onClick.AddListener(OnLoginSubmit);
            if (loginBackButton != null) loginBackButton.onClick.AddListener(ShowProviders);
            if (registerSubmitButton != null) registerSubmitButton.onClick.AddListener(OnRegisterSubmit);
            if (registerBackButton != null) registerBackButton.onClick.AddListener(ShowProviders);
        }

        private void ClearError() => SetError(string.Empty);

        private void SetError(string msg)
        {
            if (errorText != null)
                errorText.text = msg ?? string.Empty;
        }

        private void ShowProviders()
        {
            ClearError();
            if (panelProviders != null) panelProviders.SetActive(true);
            if (panelLogin != null) panelLogin.SetActive(false);
            if (panelRegister != null) panelRegister.SetActive(false);
        }

        private void ShowLogin()
        {
            ClearError();
            if (panelProviders != null) panelProviders.SetActive(false);
            if (panelLogin != null) panelLogin.SetActive(true);
            if (panelRegister != null) panelRegister.SetActive(false);
        }

        private void ShowRegister()
        {
            ClearError();
            if (panelProviders != null) panelProviders.SetActive(false);
            if (panelLogin != null) panelLogin.SetActive(false);
            if (panelRegister != null) panelRegister.SetActive(true);
        }

        private async void OnLoginSubmit()
        {
            if (_auth == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                await _auth.LoginEmailAsync(loginEmail?.text ?? string.Empty, loginPassword?.text ?? string.Empty, CancellationToken.None)
                    .ConfigureAwait(true);
                GoLobby();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnRegisterSubmit()
        {
            if (_auth == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                var u = string.IsNullOrWhiteSpace(regUsername?.text) ? null : regUsername.text.Trim();
                var e = regEmail?.text?.Trim() ?? string.Empty;
                var p1 = regPassword?.text ?? string.Empty;
                var p2 = regPasswordConfirm?.text ?? string.Empty;
                if (p1 != p2)
                {
                    SetError("Пароли не совпадают.");
                    return;
                }

                await _auth.RegisterEmailAsync(e, p1, u, CancellationToken.None).ConfigureAwait(true);
                GoLobby();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnGuestClicked()
        {
            if (_auth == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                await _auth.GuestLoginAsync(CancellationToken.None).ConfigureAwait(true);
                GoLobby();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnGoogleClicked()
        {
            if (_auth == null || backendConfig == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                if (string.IsNullOrWhiteSpace(backendConfig.GoogleWebClientId))
                {
                    SetError("В BackendConfig не задан Google Web Client Id.");
                    return;
                }

                await _auth
                    .SignInWithGoogleAsync(backendConfig.GoogleWebClientId, backendConfig.GoogleIosClientId, CancellationToken.None)
                    .ConfigureAwait(true);
                GoLobby();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnAppleClicked()
        {
            if (_auth == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                await _auth.SignInWithAppleAsync(CancellationToken.None).ConfigureAwait(true);
                GoLobby();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void GoLobby()
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }

        private void SetBusy(bool busy)
        {
            if (busyOverlay != null)
                busyOverlay.SetActive(busy);
        }
    }
}
