using System;
using System.Threading;
using System.Threading.Tasks;
using TapBrawl.Network;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    /// <summary>
    /// Экран выбора способа входа. UI создаётся в коде (не нужно собирать Canvas вручную).
    /// </summary>
    public sealed class AuthScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig = null!;
        [SerializeField] private string lobbySceneName = "Lobby";

        private ApiClient? _api;
        private AuthManager? _auth;
        private Canvas? _canvas;
        private Text? _errorText;
        private GameObject? _busyOverlay;

        private GameObject? _panelProviders;
        private GameObject? _panelLogin;
        private GameObject? _panelRegister;

        private InputField? _loginEmail;
        private InputField? _loginPassword;
        private InputField? _regUsername;
        private InputField? _regEmail;
        private InputField? _regPassword;
        private InputField? _regPasswordConfirm;

        private void Awake()
        {
            if (backendConfig == null)
            {
                Debug.LogError("[Auth] BackendConfig не назначен.");
                return;
            }

            _api = new ApiClient(backendConfig);
            _auth = new AuthManager(_api);
            BuildUi();
        }

        private void Update() => AppleSignInBridge.Tick();

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas!.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var root = CreateStretchPanel(canvasGo.transform, "Root");
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(48, 48, 120, 48);
            v.spacing = 14;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            _errorText = CreateText(root, "Error", string.Empty, 22, new Color(1f, 0.45f, 0.45f));
            var errLe = _errorText.gameObject.AddComponent<LayoutElement>();
            errLe.minHeight = 36;

            _panelProviders = CreateProvidersPanel(root);
            _panelLogin = CreateLoginPanel(root);
            _panelRegister = CreateRegisterPanel(root);

            _busyOverlay = new GameObject("Busy", typeof(RectTransform), typeof(Image));
            _busyOverlay.transform.SetParent(_canvas.transform, false);
            StretchFull(_busyOverlay.GetComponent<RectTransform>());
            _busyOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var busyText = CreateText(_busyOverlay.GetComponent<RectTransform>(), "BusyText", "Подождите…", 26, Color.white);
            StretchFull(busyText.rectTransform);
            _busyOverlay.SetActive(false);

            ShowProviders();
        }

        private GameObject CreateProvidersPanel(RectTransform parent)
        {
            var go = CreateStretchPanel(parent, "PanelProviders");
            var v = go.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            CreateText(go, "Title", "Tap Brawl", 40, Color.white);
            CreateText(go, "Subtitle", "Войдите или создайте аккаунт", 22, new Color(0.9f, 0.9f, 0.9f));
            CreateButton(go, "Создать аккаунт (email)", ShowRegister);
            CreateButton(go, "У меня есть аккаунт", ShowLogin);
            CreateButton(go, "Войти через Google", OnGoogleClicked);
#if UNITY_IOS && !UNITY_EDITOR
            CreateButton(go, "Войти через Apple", OnAppleClicked);
#endif
            CreateButton(go, "Играть гостем", OnGuestClicked);
            return go.gameObject;
        }

        private GameObject CreateLoginPanel(RectTransform parent)
        {
            var go = CreateStretchPanel(parent, "PanelLogin");
            go.gameObject.SetActive(false);
            var v = go.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            CreateText(go, "H", "Вход по email", 30, Color.white);
            _loginEmail = CreateInput(go, "Email", InputField.ContentType.EmailAddress);
            _loginPassword = CreateInput(go, "Пароль", InputField.ContentType.Password);
            CreateButton(go, "Войти", OnLoginSubmit);
            CreateButton(go, "Назад", ShowProviders);
            return go.gameObject;
        }

        private GameObject CreateRegisterPanel(RectTransform parent)
        {
            var go = CreateStretchPanel(parent, "PanelRegister");
            go.gameObject.SetActive(false);
            var v = go.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            CreateText(go, "H", "Регистрация", 30, Color.white);
            _regUsername = CreateInput(go, "Имя (необязательно)", InputField.ContentType.Standard);
            _regEmail = CreateInput(go, "Email", InputField.ContentType.EmailAddress);
            _regPassword = CreateInput(go, "Пароль", InputField.ContentType.Password);
            _regPasswordConfirm = CreateInput(go, "Пароль ещё раз", InputField.ContentType.Password);
            CreateButton(go, "Зарегистрироваться", OnRegisterSubmit);
            CreateButton(go, "Назад", ShowProviders);
            return go.gameObject;
        }

        private static RectTransform CreateStretchPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            StretchFull(rt);
            var le = go.GetComponent<LayoutElement>();
            le.flexibleHeight = 1;
            le.flexibleWidth = 1;
            le.minHeight = 100;
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Text CreateText(RectTransform parent, string name, string msg, int size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
            var t = go.GetComponent<Text>();
            t.font = UiFont();
            t.text = msg;
            t.fontSize = size;
            t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = size + 12;
            le.preferredHeight = size + 12;
            return t;
        }

        private static void CreateButton(RectTransform parent, string label, UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 56);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            StretchFull(tr);
            var tx = textGo.GetComponent<Text>();
            tx.font = UiFont();
            tx.text = label;
            tx.fontSize = 22;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 56;
            le.preferredHeight = 56;
        }

        private static InputField CreateInput(RectTransform parent, string placeholder, InputField.ContentType content)
        {
            var go = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 52);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.12f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            StretchFull(tr);
            tr.offsetMin = new Vector2(12, 4);
            tr.offsetMax = new Vector2(-12, -4);
            var text = textGo.GetComponent<Text>();
            text.font = UiFont();
            text.fontSize = 22;
            text.color = Color.white;
            text.supportRichText = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(go.transform, false);
            var pr = phGo.GetComponent<RectTransform>();
            StretchFull(pr);
            pr.offsetMin = new Vector2(12, 4);
            pr.offsetMax = new Vector2(-12, -4);
            var ph = phGo.GetComponent<Text>();
            ph.font = text.font;
            ph.fontSize = 22;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = placeholder;
            ph.fontStyle = FontStyle.Italic;

            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = ph;
            field.contentType = content;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 52;
            le.preferredHeight = 52;
            return field;
        }

        private void ClearError() => SetError(string.Empty);

        private void SetError(string msg)
        {
            if (_errorText != null)
                _errorText.text = msg ?? string.Empty;
        }

        private void ShowProviders()
        {
            ClearError();
            if (_panelProviders != null) _panelProviders.SetActive(true);
            if (_panelLogin != null) _panelLogin.SetActive(false);
            if (_panelRegister != null) _panelRegister.SetActive(false);
        }

        private void ShowLogin()
        {
            ClearError();
            if (_panelProviders != null) _panelProviders.SetActive(false);
            if (_panelLogin != null) _panelLogin.SetActive(true);
            if (_panelRegister != null) _panelRegister.SetActive(false);
        }

        private void ShowRegister()
        {
            ClearError();
            if (_panelProviders != null) _panelProviders.SetActive(false);
            if (_panelLogin != null) _panelLogin.SetActive(false);
            if (_panelRegister != null) _panelRegister.SetActive(true);
        }

        private async void OnLoginSubmit()
        {
            if (_auth == null)
                return;
            try
            {
                SetBusy(true);
                ClearError();
                await _auth.LoginEmailAsync(_loginEmail?.text ?? string.Empty, _loginPassword?.text ?? string.Empty, CancellationToken.None)
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
                var u = string.IsNullOrWhiteSpace(_regUsername?.text) ? null : _regUsername.text.Trim();
                var e = _regEmail?.text?.Trim() ?? string.Empty;
                var p1 = _regPassword?.text ?? string.Empty;
                var p2 = _regPasswordConfirm?.text ?? string.Empty;
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

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
                return f;
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 16);
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
            if (_busyOverlay != null)
                _busyOverlay.SetActive(busy);
        }
    }
}
