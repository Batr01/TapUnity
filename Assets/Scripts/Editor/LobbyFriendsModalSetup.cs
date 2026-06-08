using TapBrawl.Network;
using TapBrawl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbyFriendsModalSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string FriendsPrefabPath = "Assets/Prefabs/UI/FriendsModal.prefab";
        private const string BackendConfigPath = "Assets/ScriptableObjects/BackendConfig.asset";
        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        [MenuItem("Tap/Setup Lobby Friends Modal")]
        public static void SetupLobbyFriendsModal()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Lobby Friends Modal",
                    "Создать модалку друзей, BtnFriends, LobbyHubHost и FriendChallengeOverlay?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Lobby Friends Modal");
            var group = Undo.GetCurrentGroup();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var canvas = FindLobbyCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Lobby нет Canvas.", "OK");
                return;
            }

            EnsureLobbyHubHost();
            EnsureChallengeOverlay(canvas.transform);

            RemoveIfExists(canvas.transform, "FriendsModal");
            var friendsModal = BuildFriendsModal(canvas.transform);
            friendsModal.SetActive(false);

            EnsureDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(friendsModal, FriendsPrefabPath);
            WireFriendsModal(friendsModal);
            EnsureBtnFriends();

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "Модалка друзей создана.\n" + FriendsPrefabPath, "OK");
        }

        private static GameObject BuildFriendsModal(Transform parent)
        {
            var modal = new GameObject("FriendsModal", typeof(RectTransform), typeof(UiPanelToggle), typeof(FriendsModalView));
            Undo.RegisterCreatedObjectUndo(modal, "Create FriendsModal");
            modal.transform.SetParent(parent, false);
            StretchFull(modal.GetComponent<RectTransform>());

            var backdrop = CreateBackdrop(modal.transform);
            var panel = CreatePanel(modal.transform);
            var header = CreateHeader(panel.transform);
            var titleText = CreateTitleText(header.transform, "Друзья");
            var backButton = CreateBackButton(header.transform);
            var countText = CreateCountText(panel.transform);
            var tabs = CreateTabToggles(panel.transform);
            var friendsSection = CreateFriendsSection(panel.transform, out var friendsContainer, out var friendRowPrefab);
            var searchSection = CreateSearchSection(panel.transform, out var searchInput, out var searchButton, out var searchContainer, out var searchRowPrefab);
            var requestsSection = CreateRequestsSection(panel.transform, out var requestsContainer, out var requestRowPrefab);
            var statusText = CreateStatusText(panel.transform);
            var backendConfig = AssetDatabase.LoadAssetAtPath<BackendConfig>(BackendConfigPath);

            var toggle = modal.GetComponent<UiPanelToggle>();
            var toggleSo = new SerializedObject(toggle);
            toggleSo.FindProperty("panelRoot").objectReferenceValue = modal;
            toggleSo.FindProperty("backdropCloseButton").objectReferenceValue = backdrop.GetComponent<Button>();
            toggleSo.ApplyModifiedPropertiesWithoutUndo();

            var view = modal.GetComponent<FriendsModalView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("backendConfig").objectReferenceValue = backendConfig;
            viewSo.FindProperty("titleText").objectReferenceValue = titleText;
            viewSo.FindProperty("statusText").objectReferenceValue = statusText;
            viewSo.FindProperty("countText").objectReferenceValue = countText;
            viewSo.FindProperty("tabToggles").arraySize = 3;
            viewSo.FindProperty("tabToggles").GetArrayElementAtIndex(0).objectReferenceValue = tabs[0];
            viewSo.FindProperty("tabToggles").GetArrayElementAtIndex(1).objectReferenceValue = tabs[1];
            viewSo.FindProperty("tabToggles").GetArrayElementAtIndex(2).objectReferenceValue = tabs[2];
            viewSo.FindProperty("friendsSection").objectReferenceValue = friendsSection;
            viewSo.FindProperty("searchSection").objectReferenceValue = searchSection;
            viewSo.FindProperty("requestsSection").objectReferenceValue = requestsSection;
            viewSo.FindProperty("friendsContainer").objectReferenceValue = friendsContainer;
            viewSo.FindProperty("searchContainer").objectReferenceValue = searchContainer;
            viewSo.FindProperty("requestsContainer").objectReferenceValue = requestsContainer;
            viewSo.FindProperty("searchInput").objectReferenceValue = searchInput;
            viewSo.FindProperty("searchButton").objectReferenceValue = searchButton;
            viewSo.FindProperty("friendRowPrefab").objectReferenceValue = friendRowPrefab;
            viewSo.FindProperty("searchRowPrefab").objectReferenceValue = searchRowPrefab;
            viewSo.FindProperty("requestRowPrefab").objectReferenceValue = requestRowPrefab;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            RewireBackButton(modal, toggle, backButton);
            searchSection.SetActive(false);
            requestsSection.SetActive(false);
            friendRowPrefab.gameObject.SetActive(false);
            searchRowPrefab.gameObject.SetActive(false);
            requestRowPrefab.gameObject.SetActive(false);
            return modal;
        }

        private static Toggle[] CreateTabToggles(Transform panel)
        {
            var row = new GameObject("Tab Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(panel, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.86f);
            rt.anchorMax = new Vector2(0.92f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            return new[]
            {
                CreateTabToggle(row.transform, "Tab Friends", "Друзья", true),
                CreateTabToggle(row.transform, "Tab Search", "Поиск", false),
                CreateTabToggle(row.transform, "Tab Requests", "Заявки", false),
            };
        }

        private static Toggle CreateTabToggle(Transform parent, string name, string label, bool isOn)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.18f, 0.28f, 0.95f);
            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.targetGraphic = bg;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var text = textGo.GetComponent<Text>();
            ConfigureText(text, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            return toggle;
        }

        private static GameObject CreateFriendsSection(Transform panel, out Transform container, out Button rowPrefab)
        {
            var section = CreateSection(panel, "Friends Section", 0.12f, 0.84f);
            container = CreateScrollContent(section.transform, "Scroll");
            rowPrefab = CreateFriendRow(container);
            return section;
        }

        private static GameObject CreateSearchSection(
            Transform panel,
            out InputField searchInput,
            out Button searchButton,
            out Transform container,
            out Button rowPrefab)
        {
            var section = CreateSection(panel, "Search Section", 0.12f, 0.84f);

            var inputGo = new GameObject("Search Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(section.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.05f, 0.88f);
            inputRt.anchorMax = new Vector2(0.65f, 0.98f);
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;
            inputGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 1f);
            searchInput = inputGo.GetComponent<InputField>();

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            StretchFull(placeholderGo.GetComponent<RectTransform>());
            var placeholder = placeholderGo.GetComponent<Text>();
            ConfigureText(placeholder, "Ник (мин. 3 символа)", 24, FontStyle.Italic, TextAnchor.MiddleLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var text = textGo.GetComponent<Text>();
            ConfigureText(text, string.Empty, 24, FontStyle.Normal, TextAnchor.MiddleLeft);
            searchInput.textComponent = text;
            searchInput.placeholder = placeholder;

            searchButton = CreateActionButton(section.transform, "Search Button", "Найти",
                new Vector2(0.68f, 0.88f), new Vector2(0.95f, 0.98f));

            container = CreateScrollContent(section.transform, "Scroll", 0.05f, 0.84f);
            rowPrefab = CreateSearchRow(container);
            return section;
        }

        private static GameObject CreateRequestsSection(Transform panel, out Transform container, out Button rowPrefab)
        {
            var section = CreateSection(panel, "Requests Section", 0.12f, 0.84f);
            container = CreateScrollContent(section.transform, "Scroll");
            rowPrefab = CreateRequestRow(container);
            return section;
        }

        private static GameObject CreateSection(Transform panel, string name, float bottom, float top)
        {
            var section = new GameObject(name, typeof(RectTransform));
            section.transform.SetParent(panel, false);
            var rt = section.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, bottom);
            rt.anchorMax = new Vector2(0.92f, top);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return section;
        }

        private static Transform CreateScrollContent(Transform parent, string name, float bottom = 0f, float top = 1f)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, bottom);
            scrollRt.anchorMax = new Vector2(1f, top);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            return content.transform;
        }

        private static Button CreateFriendRow(Transform parent)
        {
            var row = CreateRow(parent, "Friend Row Prefab");
            CreateActionButton(row.transform, "Challenge Button", "Вызов",
                new Vector2(0.55f, 0.1f), new Vector2(0.75f, 0.9f));
            CreateActionButton(row.transform, "Remove Button", "Удалить",
                new Vector2(0.78f, 0.1f), new Vector2(0.98f, 0.9f));
            return row;
        }

        private static Button CreateSearchRow(Transform parent)
        {
            var row = CreateRow(parent, "Search Row Prefab");
            CreateActionButton(row.transform, "Add Button", "Добавить",
                new Vector2(0.65f, 0.1f), new Vector2(0.98f, 0.9f));
            return row;
        }

        private static Button CreateRequestRow(Transform parent)
        {
            var row = CreateRow(parent, "Request Row Prefab");
            CreateActionButton(row.transform, "Accept Button", "Принять",
                new Vector2(0.45f, 0.1f), new Vector2(0.7f, 0.9f));
            CreateActionButton(row.transform, "Decline Button", "Отклонить",
                new Vector2(0.73f, 0.1f), new Vector2(0.98f, 0.9f));
            return row;
        }

        private static Button CreateRow(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
            go.GetComponent<LayoutElement>().preferredHeight = 72f;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.03f, 0.1f);
            labelRt.anchorMax = new Vector2(0.52f, 0.9f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            ConfigureText(label, "Player", 24, FontStyle.Normal, TextAnchor.MiddleLeft);

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        private static Button CreateActionButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.75f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            ConfigureText(textGo.GetComponent<Text>(), label, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            return btn;
        }

        private static Text CreateCountText(Transform panel)
        {
            var go = new GameObject("Count Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.82f);
            rt.anchorMax = new Vector2(0.9f, 0.86f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureText(text, "0/50", 24, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.color = new Color(0.85f, 0.9f, 1f, 1f);
            return text;
        }

        private static void EnsureLobbyHubHost()
        {
            var lobby = Object.FindFirstObjectByType<LobbyScreenController>();
            if (lobby == null)
                return;

            if (lobby.GetComponent<LobbyHubHost>() == null)
            {
                Undo.AddComponent<LobbyHubHost>(lobby.gameObject);
                var hostSo = new SerializedObject(lobby.GetComponent<LobbyHubHost>());
                var backendConfig = AssetDatabase.LoadAssetAtPath<BackendConfig>(BackendConfigPath);
                hostSo.FindProperty("backendConfig").objectReferenceValue = backendConfig;
                hostSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureChallengeOverlay(Transform canvas)
        {
            var existing = canvas.Find("FriendChallengeOverlay");
            if (existing != null)
                return;

            var overlay = new GameObject("FriendChallengeOverlay", typeof(RectTransform), typeof(FriendChallengeOverlay));
            Undo.RegisterCreatedObjectUndo(overlay, "FriendChallengeOverlay");
            overlay.transform.SetParent(canvas, false);
            StretchFull(overlay.GetComponent<RectTransform>());
            overlay.SetActive(false);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.15f, 0.35f);
            panelRt.anchorMax = new Vector2(0.85f, 0.65f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.2f, 0.98f);

            var title = new GameObject("Title Text", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(panel.transform, false);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.08f, 0.55f);
            titleRt.anchorMax = new Vector2(0.92f, 0.88f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            ConfigureText(title.GetComponent<Text>(), "Вызов на бой", 32, FontStyle.Bold, TextAnchor.MiddleCenter);

            var status = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
            status.transform.SetParent(panel.transform, false);
            var statusRt = status.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.08f, 0.42f);
            statusRt.anchorMax = new Vector2(0.92f, 0.54f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;
            ConfigureText(status.GetComponent<Text>(), string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter);

            var accept = CreateActionButton(panel.transform, "Accept Button", "Принять",
                new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.35f));
            var decline = CreateActionButton(panel.transform, "Decline Button", "Отклонить",
                new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.35f));

            var overlayComp = overlay.GetComponent<FriendChallengeOverlay>();
            var so = new SerializedObject(overlayComp);
            so.FindProperty("root").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<Text>();
            so.FindProperty("acceptButton").objectReferenceValue = accept;
            so.FindProperty("declineButton").objectReferenceValue = decline;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureBtnFriends()
        {
            var bottomBar = GameObject.Find("BottomBar")?.transform;
            if (bottomBar == null)
                return;

            var btn = bottomBar.Find("BtnFriends")?.GetComponent<Button>();
            if (btn == null)
            {
                var template = bottomBar.Find("BtnSettings") ?? bottomBar.Find("BtnShop");
                if (template == null)
                    return;

                var clone = Object.Instantiate(template.gameObject, bottomBar);
                clone.name = "BtnFriends";
                Undo.RegisterCreatedObjectUndo(clone, "BtnFriends");
                btn = clone.GetComponent<Button>();
            }

            SetButtonLabel(btn, "Друзья");

            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            if (host != null && btn != null)
                WireBottomBarButton(btn, host.OpenFriends);
        }

        private static void SetButtonLabel(Button btn, string label)
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "BtnFriends label");
                tmp.text = label;
                return;
            }

            var text = btn.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                Undo.RecordObject(text, "BtnFriends label");
                text.text = label;
            }
        }

        private static void WireBottomBarButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            Undo.RecordObject(btn, "Wire bottom bar button");
            btn.interactable = true;

            while (btn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(btn.onClick, 0);

            btn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }

        private static void WireFriendsModal(GameObject friendsModal)
        {
            var toggle = friendsModal.GetComponent<UiPanelToggle>();
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
            hostSo.FindProperty("friendsModal").objectReferenceValue = toggle;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            var btnFriends = GameObject.Find("BtnFriends")?.GetComponent<Button>();
            if (btnFriends != null)
            {
                SetButtonLabel(btnFriends, "Друзья");
                WireBottomBarButton(btnFriends, host.OpenFriends);
            }
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

        private static Text CreateTitleText(Transform header, string title)
        {
            var go = new GameObject("Title Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(header, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(120f, 0f);
            rt.offsetMax = new Vector2(-120f, 0f);
            var text = go.GetComponent<Text>();
            ConfigureText(text, title, 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            return text;
        }

        private static Button CreateBackButton(Transform header)
        {
            var go = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(header, false);
            UiModalStyle.ApplyBackButtonRect(go.GetComponent<RectTransform>());
            var img = go.GetComponent<Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(BackArrowSpriteGuid));
            if (sprite != null)
                img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static Text CreateStatusText(Transform panel)
        {
            var go = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.04f);
            rt.anchorMax = new Vector2(0.9f, 0.1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureText(text, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.color = new Color(0.85f, 0.9f, 1f, 1f);
            return text;
        }

        private static void RewireBackButton(GameObject modal, UiPanelToggle toggle, Button backButton)
        {
            UiModalStyle.PrepareBackButton(backButton);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(toggle.Hide);
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
