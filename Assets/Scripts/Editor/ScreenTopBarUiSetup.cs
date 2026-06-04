using System.Linq;
using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    /// <summary>
    /// Единая верхняя панель: «Назад» слева, центр, слот справа (как в Skills).
    /// </summary>
    public static class ScreenTopBarUiSetup
    {
        public const string TopPanelName = "Top Panel";
        public const string BackButtonName = "Back Button";
        public const string CenterSpacerName = "TopBar Center Spacer";
        public const string RightSlotName = "TopBar Right Slot";

        public const float PanelWidth = 1080f;
        public const float PanelHeight = 100f;
        public const float BackButtonSize = 100f;
        public const float RightSlotSize = 100f;

        private const string BackIconPath = "Assets/Art/Sprites/icon-back.png";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Skills.unity",
            "Assets/Scenes/Details.unity",
        };

        [MenuItem("Tap/Sync Screen Top Bars")]
        public static void SyncAllConfiguredScenes()
        {
            Undo.SetCurrentGroupName("Sync Screen Top Bars");
            var group = Undo.GetCurrentGroup();

            var changed = 0;
            foreach (var path in ScenePaths)
            {
                if (!System.IO.File.Exists(path))
                    continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (SyncActiveScene(scene))
                    changed++;
            }

            Undo.CollapseUndoOperations(group);
            EditorUtility.DisplayDialog(
                "Top Bar",
                changed > 0 ? $"Обновлено сцен: {changed}." : "Подходящих Canvas/Top Panel не найдено.",
                "OK");
        }

        [MenuItem("Tap/Sync Screen Top Bar (Current Scene)")]
        public static void SyncCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Top Bar", "Нет активной сцены.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Sync Screen Top Bar");
            var group = Undo.GetCurrentGroup();
            var ok = SyncActiveScene(scene);
            Undo.CollapseUndoOperations(group);

            if (ok)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorUtility.DisplayDialog("Top Bar", "Верхняя панель обновлена.", "OK");
            }
            else
                EditorUtility.DisplayDialog("Top Bar", "В сцене нет Canvas.", "OK");
        }

        private static bool SyncActiveScene(Scene scene)
        {
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.isRootCanvas);
            if (canvas == null)
                return false;

            var topPanel = FindOrCreateTopPanel(canvas.transform);
            ConfigureTopPanelShell(topPanel);
            var backButton = ConfigureBackButton(topPanel);
            ConfigureCenterSpacer(topPanel);
            ConfigureRightSlot(topPanel);
            WireSceneControllers(scene, backButton);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static RectTransform FindOrCreateTopPanel(Transform canvas)
        {
            var existing = canvas.Find(TopPanelName);
            if (existing != null)
                return (RectTransform)existing;

            var go = new GameObject(TopPanelName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create Top Panel");
            go.transform.SetParent(canvas, false);
            go.transform.SetAsFirstSibling();
            return go.GetComponent<RectTransform>();
        }

        private static void ConfigureTopPanelShell(RectTransform topPanel)
        {
            Undo.RecordObject(topPanel, "Configure Top Panel");

            // Якорь к верхнему краю Canvas, растягивается по ширине
            topPanel.anchorMin = new Vector2(0f, 1f);
            topPanel.anchorMax = new Vector2(1f, 1f);
            topPanel.pivot = new Vector2(0.5f, 1f);
            topPanel.anchoredPosition = Vector2.zero;
            topPanel.sizeDelta = new Vector2(0f, PanelHeight);

            var image = topPanel.GetComponent<Image>() ?? Undo.AddComponent<Image>(topPanel.gameObject);
            image.color = new Color(1f, 1f, 1f, 0.392f);
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;

            var hlg = topPanel.GetComponent<HorizontalLayoutGroup>() ??
                      Undo.AddComponent<HorizontalLayoutGroup>(topPanel.gameObject);
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 0;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // IgnoreLayout: панель позиционируется якорями, а не VLG на Canvas
            var le = topPanel.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(topPanel.gameObject);
            le.ignoreLayout = true;
            le.minHeight = PanelHeight;
            le.preferredHeight = PanelHeight;
            le.flexibleHeight = 0f;

            // Canvas VLG: отступ сверху равен высоте панели
            var canvas = topPanel.parent?.GetComponent<VerticalLayoutGroup>();
            if (canvas != null)
                canvas.padding = new RectOffset(canvas.padding.left, canvas.padding.right, (int)PanelHeight, canvas.padding.bottom);
        }

        private static Button ConfigureBackButton(RectTransform topPanel)
        {
            var backTransform = FindChild(topPanel, BackButtonName);
            GameObject backGo;
            if (backTransform == null)
            {
                var legacy = topPanel.Find("Button");
                if (legacy != null)
                {
                    Undo.RecordObject(legacy.gameObject, "Rename Back Button");
                    legacy.name = BackButtonName;
                    backGo = legacy.gameObject;
                }
                else
                {
                    backGo = new GameObject(BackButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
                    Undo.RegisterCreatedObjectUndo(backGo, "Create Back Button");
                    backGo.transform.SetParent(topPanel, false);
                }
            }
            else
                backGo = backTransform.gameObject;

            backGo.transform.SetAsFirstSibling();

            var rt = backGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(BackButtonSize, BackButtonSize);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(BackIconPath);
            var img = backGo.GetComponent<Image>();
            img.sprite = icon;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.preserveAspect = true;

            var btn = backGo.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            EnsureBackButtonLabel(backGo.transform);

            return btn;
        }

        private static void EnsureBackButtonLabel(Transform backButton)
        {
            var label = backButton.Find("Text (TMP)");
            if (label == null)
                return;

            label.SetAsLastSibling();
            var rt = label.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private static void ConfigureCenterSpacer(RectTransform topPanel)
        {
            if (FindChild(topPanel, "Status Text") != null)
                return;

            RectTransform spacer;
            var found = FindChild(topPanel, CenterSpacerName);
            if (found == null)
            {
                var go = new GameObject(CenterSpacerName, typeof(RectTransform), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(go, "Create TopBar Spacer");
                spacer = go.GetComponent<RectTransform>();
                spacer.SetParent(topPanel, false);
            }
            else
                spacer = (RectTransform)found;

            spacer.SetSiblingIndex(1);
            spacer.sizeDelta = new Vector2(160f, 30f);

            var le = spacer.GetComponent<LayoutElement>();
            le.minWidth = 10f;
            le.flexibleWidth = 1f;
            le.minHeight = 30f;
            le.preferredHeight = 30f;
        }

        private static void ConfigureRightSlot(RectTransform topPanel)
        {
            if (FindChild(topPanel, "Save Button") != null)
                return;

            RectTransform slot;
            var found = FindChild(topPanel, RightSlotName);
            if (found == null)
            {
                var go = new GameObject(RightSlotName, typeof(RectTransform), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(go, "Create TopBar Right Slot");
                slot = go.GetComponent<RectTransform>();
                slot.SetParent(topPanel, false);
            }
            else
                slot = (RectTransform)found;

            slot.SetAsLastSibling();
            slot.sizeDelta = new Vector2(RightSlotSize, RightSlotSize);

            var le = slot.GetComponent<LayoutElement>();
            le.minWidth = RightSlotSize;
            le.preferredWidth = RightSlotSize;
            le.minHeight = RightSlotSize;
            le.preferredHeight = RightSlotSize;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
        }

        private static void WireSceneControllers(Scene scene, Button backButton)
        {
            var sceneName = scene.name;

            if (sceneName == "Details")
            {
                var details = Object.FindFirstObjectByType<SkillDetailsScreenController>();
                if (details != null)
                {
                    var so = new SerializedObject(details);
                    so.FindProperty("backButton").objectReferenceValue = backButton;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                return;
            }

            if (sceneName != "Skills")
                return;

            var nav = Object.FindFirstObjectByType<SkillsSceneNavigationController>();
            if (nav == null)
                return;

            var navSo = new SerializedObject(nav);
            navSo.FindProperty("lobbySceneName").stringValue = "Lobby";
            navSo.FindProperty("skillsSceneName").stringValue = "Skills";
            navSo.FindProperty("skillDetailsSceneName").stringValue = "Details";
            navSo.ApplyModifiedPropertiesWithoutUndo();

            var backCalls = backButton.onClick;
            var hasLobby = backCalls.GetPersistentEventCount() > 0 &&
                           backCalls.GetPersistentMethodName(0) == nameof(SkillsSceneNavigationController.OpenLobbyScene);
            if (!hasLobby)
            {
                Undo.RecordObject(backButton, "Wire Back Button");
                backCalls.RemoveAllListeners();
                backCalls.AddListener(nav.OpenLobbyScene);
            }

            var status = topPanelStatusText(backButton.transform.parent as RectTransform);
            var panel = Object.FindFirstObjectByType<SkillsPanelController>();
            if (panel != null && status != null)
            {
                var panelSo = new SerializedObject(panel);
                panelSo.FindProperty("statusText").objectReferenceValue = status;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var saveButton = FindSaveButton(backButton.transform.parent as RectTransform);
            if (panel != null && saveButton != null)
            {
                var panelSo = new SerializedObject(panel);
                panelSo.FindProperty("saveLoadoutButton").objectReferenceValue = saveButton;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Text? topPanelStatusText(RectTransform? topPanel)
        {
            if (topPanel == null)
                return null;

            var statusTr = topPanel.Cast<Transform>().FirstOrDefault(t => t.name == "Status Text");
            return statusTr != null ? statusTr.GetComponent<Text>() : null;
        }

        private static Button? FindSaveButton(RectTransform? topPanel)
        {
            if (topPanel == null)
                return null;

            var saveTr = topPanel.Cast<Transform>().FirstOrDefault(t => t.name == "Save Button");
            return saveTr != null ? saveTr.GetComponent<Button>() : null;
        }

        private static Transform? FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
