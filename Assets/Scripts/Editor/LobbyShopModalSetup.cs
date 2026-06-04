using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbyShopModalSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string ShopPrefabPath = "Assets/Prefabs/UI/ShopModal.prefab";
        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        [MenuItem("Tap/Setup Lobby Shop Modal")]
        public static void SetupLobbyShopModal()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Lobby Shop Modal",
                    "Создать модалку магазина в Lobby и привязать BtnShop?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Lobby Shop Modal");
            var group = Undo.GetCurrentGroup();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var canvas = FindLobbyCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Lobby нет Canvas.", "OK");
                return;
            }

            RemoveIfExists(canvas.transform, "ShopModal");
            var shopModal = BuildShopModal(canvas.transform);
            shopModal.SetActive(false);

            EnsureDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(shopModal, ShopPrefabPath);
            WireShopModal(shopModal);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "Модалка магазина создана.\n" + ShopPrefabPath, "OK");
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

        private static GameObject BuildShopModal(Transform parent)
        {
            var modal = new GameObject("ShopModal", typeof(RectTransform), typeof(UiPanelToggle), typeof(ShopModalView));
            Undo.RegisterCreatedObjectUndo(modal, "Create ShopModal");
            modal.transform.SetParent(parent, false);
            StretchFull(modal.GetComponent<RectTransform>());

            var backdrop = CreateBackdrop(modal.transform);
            var panel = CreatePanel(modal.transform);
            var header = CreateHeader(panel.transform);
            var titleText = CreateTitleText(header.transform);
            var backButton = CreateBackButton(header.transform);
            var messageText = CreateMessageText(panel.transform);

            var toggle = modal.GetComponent<UiPanelToggle>();
            var toggleSo = new SerializedObject(toggle);
            toggleSo.FindProperty("panelRoot").objectReferenceValue = modal;
            toggleSo.FindProperty("backdropCloseButton").objectReferenceValue = backdrop.GetComponent<Button>();
            toggleSo.ApplyModifiedPropertiesWithoutUndo();

            var view = modal.GetComponent<ShopModalView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("titleText").objectReferenceValue = titleText;
            viewSo.FindProperty("messageText").objectReferenceValue = messageText;
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
            var img = panel.GetComponent<Image>();
            UiModalStyle.ApplyPanel(img);
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
            ConfigureText(text, "Магазин", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            return text;
        }

        private static Button CreateBackButton(Transform header)
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

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static Text CreateMessageText(Transform panel)
        {
            var go = new GameObject("Message Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.35f);
            rt.anchorMax = new Vector2(0.9f, 0.65f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            ConfigureText(
                text,
                "Магазин всё ещё в разработке.",
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

        private static void WireShopModal(GameObject shopModal)
        {
            var toggle = shopModal.GetComponent<UiPanelToggle>();
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
            hostSo.FindProperty("shopModal").objectReferenceValue = toggle;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            var btnShop = GameObject.Find("BtnShop")?.GetComponent<Button>();
            if (btnShop != null)
            {
                Undo.RecordObject(btnShop, "Wire BtnShop");
                btnShop.interactable = true;
                btnShop.onClick.RemoveAllListeners();
                btnShop.onClick.AddListener(host.OpenShop);
            }
        }

        private static void RewireBackButton(GameObject modal, UiPanelToggle toggle)
        {
            var back = modal.transform.Find("Panel/Header/Back Button")?.GetComponent<Button>();
            if (back == null)
                return;

            Undo.RecordObject(back, "Wire shop back");
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
