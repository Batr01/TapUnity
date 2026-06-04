using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class AuthScreenUiSetup
    {
        private const float ButtonMinHeight = 200f;
        private const int ButtonFontSize = 55;
        private const float IconSlotSize = 140f;
        private const float TitleMinHeight = 100f;
        private const float SubtitleMinHeight = 50f;
        private const string RoundedButtonSpritePath = "Assets/Art/Sprites/buttonStart.png";
        private const string IconNewEmailPath = "Assets/Art/Sprites/icon-new-email.png";
        private const string IconMyAccountPath = "Assets/Art/Sprites/icon-my-account.png";
        private const string IconGooglePath = "Assets/Art/Sprites/icon-google.png";
        private const string IconApplePath = "Assets/Art/Sprites/icon-apple-logo.png";
        private const string IconGuestPath = "Assets/Art/Sprites/icon-guest.png";

        [MenuItem("Tap/Setup Auth Screen UI")]
        public static void SetupAuthScreenUi()
        {
            var controller = Object.FindFirstObjectByType<AuthScreenController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Auth UI", "В сцене нет AuthScreenController.", "OK");
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Auth UI", "В сцене нет Canvas.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Auth UI",
                    "Создать стандартную разметку экрана входа на Canvas?\nСуществующие дочерние объекты Root/Busy будут удалены.",
                    "Создать",
                    "Отмена"))
                return;

            Undo.SetCurrentGroupName("Setup Auth Screen UI");
            var group = Undo.GetCurrentGroup();

            RemoveIfExists(canvas.transform, "Root");
            RemoveIfExists(canvas.transform, "Busy");

            var root = CreateStretchPanel(canvas.transform, "Root");
            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(48, 48, 120, 48);
            rootLayout.spacing = 14;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Auth Root");

            var errorText = CreateText(root, "ErrorText", string.Empty, 22, new Color(1f, 0.45f, 0.45f), 36);
            var errLe = errorText.gameObject.GetComponent<LayoutElement>();
            if (errLe != null)
                errLe.minHeight = 36;

            var panelProviders = CreateProvidersPanel(root);
            var panelLogin = CreateLoginPanel(root);
            var panelRegister = CreateRegisterPanel(root);

            var busyOverlay = new GameObject("Busy", typeof(RectTransform), typeof(Image));
            busyOverlay.transform.SetParent(canvas.transform, false);
            StretchFull(busyOverlay.GetComponent<RectTransform>());
            busyOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var busyText = CreateText(busyOverlay.GetComponent<RectTransform>(), "BusyText", "Подождите…", 26, Color.white, 40);
            StretchFull(busyText.rectTransform);
            busyOverlay.SetActive(false);
            Undo.RegisterCreatedObjectUndo(busyOverlay, "Auth Busy");

            WireController(
                controller,
                panelProviders,
                panelLogin,
                panelRegister,
                busyOverlay,
                errorText);

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Auth] UI создан.");
        }

        private static void WireController(
            AuthScreenController controller,
            ProvidersPanelRefs providers,
            LoginPanelRefs login,
            RegisterPanelRefs register,
            GameObject busyOverlay,
            Text errorText)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("panelProviders").objectReferenceValue = providers.Root;
            so.FindProperty("panelLogin").objectReferenceValue = login.Root;
            so.FindProperty("panelRegister").objectReferenceValue = register.Root;
            so.FindProperty("busyOverlay").objectReferenceValue = busyOverlay;
            so.FindProperty("errorText").objectReferenceValue = errorText;
            so.FindProperty("loginEmail").objectReferenceValue = login.Email;
            so.FindProperty("loginPassword").objectReferenceValue = login.Password;
            so.FindProperty("loginSubmitButton").objectReferenceValue = login.Submit;
            so.FindProperty("loginBackButton").objectReferenceValue = login.Back;
            so.FindProperty("regUsername").objectReferenceValue = register.Username;
            so.FindProperty("regEmail").objectReferenceValue = register.Email;
            so.FindProperty("regPassword").objectReferenceValue = register.Password;
            so.FindProperty("regPasswordConfirm").objectReferenceValue = register.PasswordConfirm;
            so.FindProperty("registerSubmitButton").objectReferenceValue = register.Submit;
            so.FindProperty("registerBackButton").objectReferenceValue = register.Back;
            so.FindProperty("registerNavButton").objectReferenceValue = providers.RegisterNav;
            so.FindProperty("loginNavButton").objectReferenceValue = providers.LoginNav;
            so.FindProperty("googleButton").objectReferenceValue = providers.Google;
            so.FindProperty("appleButton").objectReferenceValue = providers.Apple;
            so.FindProperty("guestButton").objectReferenceValue = providers.Guest;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void RemoveIfExists(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }

        private sealed class ProvidersPanelRefs
        {
            public GameObject Root = null!;
            public Button RegisterNav = null!;
            public Button LoginNav = null!;
            public Button Google = null!;
            public Button Apple = null!;
            public Button Guest = null!;
        }

        private sealed class LoginPanelRefs
        {
            public GameObject Root = null!;
            public InputField Email = null!;
            public InputField Password = null!;
            public Button Submit = null!;
            public Button Back = null!;
        }

        private sealed class RegisterPanelRefs
        {
            public GameObject Root = null!;
            public InputField Username = null!;
            public InputField Email = null!;
            public InputField Password = null!;
            public InputField PasswordConfirm = null!;
            public Button Submit = null!;
            public Button Back = null!;
        }

        private static ProvidersPanelRefs CreateProvidersPanel(RectTransform parent)
        {
            var go = CreateStretchPanel(parent, "PanelProviders");
            var v = go.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            CreateText(go, "Title", "Tap Brawl", 72, Color.white, TitleMinHeight);
            CreateText(go, "Subtitle", "Войдите или создайте аккаунт", 32, new Color(0.9f, 0.9f, 0.9f), SubtitleMinHeight);

            return new ProvidersPanelRefs
            {
                Root = go.gameObject,
                RegisterNav = CreateButton(go, "RegisterNavButton", "Создать аккаунт (email)", LoadIcon(IconNewEmailPath)),
                LoginNav = CreateButton(go, "LoginNavButton", "У меня есть аккаунт", LoadIcon(IconMyAccountPath)),
                Google = CreateButton(go, "GoogleButton", "Войти через Google", LoadIcon(IconGooglePath)),
                Apple = CreateButton(go, "AppleButton", "Войти через Apple", LoadIcon(IconApplePath)),
                Guest = CreateButton(go, "GuestButton", "Играть гостем", LoadIcon(IconGuestPath)),
            };
        }

        private static LoginPanelRefs CreateLoginPanel(RectTransform parent)
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

            CreateText(go, "Header", "Вход по email", 30, Color.white, 44);
            var email = CreateInput(go, "LoginEmail", "Email", InputField.ContentType.EmailAddress);
            var password = CreateInput(go, "LoginPassword", "Пароль", InputField.ContentType.Password);

            return new LoginPanelRefs
            {
                Root = go.gameObject,
                Email = email,
                Password = password,
                Submit = CreateButton(go, "LoginSubmitButton", "Войти"),
                Back = CreateButton(go, "LoginBackButton", "Назад"),
            };
        }

        private static RegisterPanelRefs CreateRegisterPanel(RectTransform parent)
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

            CreateText(go, "Header", "Регистрация", 30, Color.white, 44);

            return new RegisterPanelRefs
            {
                Root = go.gameObject,
                Username = CreateInput(go, "RegUsername", "Имя (необязательно)", InputField.ContentType.Standard),
                Email = CreateInput(go, "RegEmail", "Email", InputField.ContentType.EmailAddress),
                Password = CreateInput(go, "RegPassword", "Пароль", InputField.ContentType.Password),
                PasswordConfirm = CreateInput(go, "RegPasswordConfirm", "Пароль ещё раз", InputField.ContentType.Password),
                Submit = CreateButton(go, "RegisterSubmitButton", "Зарегистрироваться"),
                Back = CreateButton(go, "RegisterBackButton", "Назад"),
            };
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

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
                return f;
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        private static Sprite? RoundedButtonSprite() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(RoundedButtonSpritePath);

        private static Sprite? LoadIcon(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        private static Text CreateText(RectTransform parent, string name, string msg, int fontSize, Color c, float minHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, minHeight);
            var t = go.GetComponent<Text>();
            t.font = UiFont();
            t.text = msg;
            t.fontSize = fontSize;
            t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.preferredHeight = minHeight;
            return t;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Sprite? icon = null)
        {
            var roundedSprite = RoundedButtonSprite();

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, ButtonMinHeight);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            if (roundedSprite != null)
            {
                img.sprite = roundedSprite;
                img.type = Image.Type.Simple;
            }

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            content.transform.SetParent(go.transform, false);
            StretchFull(content.GetComponent<RectTransform>());
            var hlg = content.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(40, 40, 0, 0);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = false;

            CreateButtonIcon(content.transform, icon);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textGo.transform.SetParent(content.transform, false);
            var tx = textGo.GetComponent<Text>();
            tx.font = UiFont();
            tx.text = label;
            tx.fontSize = ButtonFontSize;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;
            var textLe = textGo.GetComponent<LayoutElement>();
            textLe.flexibleWidth = 1;
            textLe.minHeight = ButtonMinHeight - 20f;

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = ButtonMinHeight;
            le.preferredHeight = ButtonMinHeight;
            return btn;
        }

        private static void CreateButtonIcon(Transform contentParent, Sprite? icon)
        {
            var iconSlot = new GameObject("IconSlot", typeof(RectTransform), typeof(LayoutElement));
            iconSlot.transform.SetParent(contentParent, false);
            var iconLe = iconSlot.GetComponent<LayoutElement>();
            iconLe.minWidth = IconSlotSize;
            iconLe.preferredWidth = IconSlotSize;
            iconLe.minHeight = IconSlotSize;
            iconLe.preferredHeight = IconSlotSize;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(UiButtonIcon));
            iconGo.transform.SetParent(iconSlot.transform, false);
            StretchFull(iconGo.GetComponent<RectTransform>());
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var iconSo = new SerializedObject(iconGo.GetComponent<UiButtonIcon>());
            iconSo.FindProperty("iconImage").objectReferenceValue = iconImg;
            iconSo.FindProperty("icon").objectReferenceValue = icon;
            iconSo.FindProperty("hideWhenEmpty").boolValue = true;
            iconSo.ApplyModifiedPropertiesWithoutUndo();

            if (icon != null)
                iconImg.sprite = icon;
        }

        private static InputField CreateInput(RectTransform parent, string name, string placeholder, InputField.ContentType content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
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
    }
}
