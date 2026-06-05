using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbySettingsModalSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string SettingsPrefabPath = "Assets/Prefabs/UI/SettingsModal.prefab";
        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        [MenuItem("Tap/Setup Lobby Settings Modal")]
        public static void SetupLobbySettingsModal()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Lobby Settings Modal",
                    "Создать модалку настроек в Lobby и привязать BtnSettings?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Lobby Settings Modal");
            var group = Undo.GetCurrentGroup();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var canvas = FindLobbyCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Lobby нет Canvas.", "OK");
                return;
            }

            RemoveIfExists(canvas.transform, "SettingsModal");
            var settingsModal = BuildSettingsModal(canvas.transform);
            settingsModal.SetActive(false);

            EnsureDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(settingsModal, SettingsPrefabPath);
            WireSettingsModal(settingsModal);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "Модалка настроек создана.\n" + SettingsPrefabPath, "OK");
        }

        private static Canvas? FindLobbyCanvas()
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.gameObject.scene.path == LobbyScenePath)
                    return c;
            }

            return null;
        }

        private static GameObject BuildSettingsModal(Transform parent)
        {
            var modal = new GameObject("SettingsModal", typeof(RectTransform), typeof(UiPanelToggle), typeof(SettingsModalView));
            Undo.RegisterCreatedObjectUndo(modal, "Create SettingsModal");
            modal.transform.SetParent(parent, false);
            StretchFull(modal.GetComponent<RectTransform>());

            var backdrop = CreateBackdrop(modal.transform);
            var panel = CreatePanel(modal.transform);
            var header = CreateHeader(panel.transform);
            var titleText = CreateTitleText(header.transform);
            CreateBackButton(header.transform);
            var actions = CreateActions(panel.transform);
            var logoutButton = CreateLogoutButton(actions.transform);
            var messageText = CreateMessageText(panel.transform);

            var toggle = modal.GetComponent<UiPanelToggle>();
            var toggleSo = new SerializedObject(toggle);
            toggleSo.FindProperty("panelRoot").objectReferenceValue = modal;
            toggleSo.FindProperty("backdropCloseButton").objectReferenceValue = backdrop.GetComponent<Button>();
            toggleSo.ApplyModifiedPropertiesWithoutUndo();

            var view = modal.GetComponent<SettingsModalView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("titleText").objectReferenceValue = titleText;
            viewSo.FindProperty("messageText").objectReferenceValue = messageText;
            viewSo.FindProperty("logoutButton").objectReferenceValue = logoutButton;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            RewireBackButton(modal, toggle);
            return modal;
        }

        private static GameObject CreateBackdrop(Transform parent)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.SetAsFirstSibling();
            StretchFull(backdrop.GetComponent<RectTransform>());
            var img = backdrop.GetComponent<Image>();
            img.color = UiModalStyle.BackdropColor;
            img.raycastTarget = true;
            var btn = backdrop.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            return backdrop;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            UiModalStyle.ApplyPanelRect(panel.GetComponent<RectTransform>());
            UiModalStyle.ApplyPanel(panel.GetComponent<Image>());
            return panel;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            UiModalStyle.ApplyHeaderRect(header.GetComponent<RectTransform>());
            return header;
        }

        private static Text CreateTitleText(Transform header)
        {
            var go = new GameObject("Title Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(header, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(120f, 0f);
            rt.offsetMax = new Vector2(-120f, 0f);
            var text = go.GetComponent<Text>();
            ConfigureText(text, "Настройки", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            return text;
        }

        private static void CreateBackButton(Transform header)
        {
            var go = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(header, false);
            UiModalStyle.ApplyBackButtonRect(go.GetComponent<RectTransform>());

            var img = go.GetComponent<Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(BackArrowSpriteGuid));
            if (sprite != null)
                img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = true;
            go.GetComponent<Button>().targetGraphic = img;
        }

        private static GameObject CreateActions(Transform panel)
        {
            var go = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(panel, false);
            go.transform.SetSiblingIndex(1);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.45f);
            rt.anchorMax = new Vector2(0.92f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return go;
        }

        private static Button CreateLogoutButton(Transform actions)
        {
            return CreateActionButton(actions, "Logout Button", "Выйти из аккаунта", new Color(0.55f, 0.22f, 0.22f, 1f));
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Color bgColor)
        {
            const float height = 72f;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;

            var image = go.GetComponent<Image>();
            image.color = bgColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<Text>();
            ConfigureText(text, label, 32, FontStyle.Normal, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text CreateMessageText(Transform panel)
        {
            var go = new GameObject("Message Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.12f);
            rt.anchorMax = new Vector2(0.9f, 0.38f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureText(
                text,
                "Настройки всё ещё в разработке.",
                32,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            return text;
        }

        private static void ConfigureText(Text text, string content, int size, FontStyle style, TextAnchor anchor)
        {
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void WireSettingsModal(GameObject settingsModal)
        {
            var toggle = settingsModal.GetComponent<UiPanelToggle>();
            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            if (host == null)
            {
                var lobby = Object.FindFirstObjectByType<LobbyScreenController>();
                var go = lobby != null ? lobby.gameObject : new GameObject("LobbyModalsHost");
                if (lobby == null)
                    Undo.RegisterCreatedObjectUndo(go, "LobbyModalsHost");
                host = Undo.AddComponent<LobbyModalsHost>(go);
            }

            var hostSo = new SerializedObject(host);
            hostSo.FindProperty("settingsModal").objectReferenceValue = toggle;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            var btnSettings = GameObject.Find("BtnSettings")?.GetComponent<Button>();
            if (btnSettings != null)
            {
                Undo.RecordObject(btnSettings, "Wire BtnSettings");
                btnSettings.interactable = true;
                btnSettings.onClick.RemoveAllListeners();
                btnSettings.onClick.AddListener(host.OpenSettings);
            }
        }

        private static void RewireBackButton(GameObject modal, UiPanelToggle toggle)
        {
            var back = modal.transform.Find("Panel/Header/Back Button")?.GetComponent<Button>();
            if (back == null)
                return;

            Undo.RecordObject(back, "Wire settings back");
            UiModalStyle.PrepareBackButton(back);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(toggle.Hide);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void RemoveIfExists(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
    }
}
