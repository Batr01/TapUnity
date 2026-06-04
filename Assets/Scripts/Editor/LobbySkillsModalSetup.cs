using System.Collections.Generic;
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
    /// Перенос Skills/Details UI в Lobby как модалки + сохранение префабов.
    /// </summary>
    public static class LobbySkillsModalSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string SkillsScenePath = "Assets/Scenes/Skills.unity";
        private const string DetailsScenePath = "Assets/Scenes/Details.unity";
        private const string SkillsPrefabPath = "Assets/Prefabs/UI/SkillsModal.prefab";
        private const string DetailsPrefabPath = "Assets/Prefabs/UI/SkillDetailsModal.prefab";

        [MenuItem("Tap/Setup Lobby Skills Modals")]
        public static void SetupLobbySkillsModals()
        {
            if (!EditorUtility.DisplayDialog(
                    "Lobby Skills Modals",
                    "Скопировать UI из Skills и Details в Lobby (Skills — 1:1 как в сцене), создать префабы?",
                    "Выполнить",
                    "Отмена"))
                return;

            Undo.SetCurrentGroupName("Setup Lobby Skills Modals");
            var group = Undo.GetCurrentGroup();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var lobbyCanvas = FindLobbyCanvas();
            if (lobbyCanvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Lobby нет Canvas.", "OK");
                return;
            }

            var lobbySceneRef = lobbyScene;
            var detailsContent = CloneDetailsUi(lobbySceneRef);
            if (detailsContent.Count == 0)
            {
                EditorUtility.DisplayDialog("Ошибка", "Не найден UI в Details.unity.", "OK");
                return;
            }

            RemoveIfExists(lobbyCanvas.transform, "SkillsModal");
            RemoveIfExists(lobbyCanvas.transform, "SkillDetailsModal");

            var skillsModal = BuildSkillsModal(lobbyCanvas.transform, lobbySceneRef);
            var detailsModal = BuildDetailsModalShell(lobbyCanvas.transform, detailsContent);

            skillsModal.SetActive(false);
            detailsModal.SetActive(false);

            EnsureDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(skillsModal, SkillsPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(detailsModal, DetailsPrefabPath);

            WireLobbyModals(lobbyCanvas.transform, skillsModal, detailsModal);
            RewireSkillsBackButton(skillsModal);
            RewireDetailsBackButton(detailsModal);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayDialog(
                "Готово",
                "Модалки обновлены.\n" + SkillsPrefabPath + "\n" + DetailsPrefabPath,
                "OK");
        }

        [MenuItem("Tap/Rebuild Skills Modal (match Skills scene)")]
        public static void RebuildSkillsModalOnly()
        {
            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var lobbyCanvas = FindLobbyCanvas();
            if (lobbyCanvas == null)
                return;

            Undo.SetCurrentGroupName("Rebuild Skills Modal");
            var group = Undo.GetCurrentGroup();

            RemoveIfExists(lobbyCanvas.transform, "SkillsModal");
            var skillsModal = BuildSkillsModal(lobbyCanvas.transform, lobbyScene);
            skillsModal.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(skillsModal, SkillsPrefabPath);

            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            if (host != null)
            {
                var hostSo = new SerializedObject(host);
                hostSo.FindProperty("skillsModal").objectReferenceValue = skillsModal.GetComponent<UiPanelToggle>();
                hostSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var skillsPanel = skillsModal.GetComponentInChildren<SkillsPanelController>(true);
            if (skillsPanel != null && host != null)
            {
                var panelSo = new SerializedObject(skillsPanel);
                panelSo.FindProperty("lobbyModals").objectReferenceValue = host;
                panelSo.FindProperty("navigationController").objectReferenceValue = null;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            RewireSkillsBackButton(skillsModal);
            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);
        }

        private static Canvas? FindLobbyCanvas()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.gameObject.scene.path == LobbyScenePath)
                    return canvas;
            }

            return null;
        }

        private static GameObject CloneSkillsCanvasRoot(Scene targetScene)
        {
            var scene = EditorSceneManager.OpenScene(SkillsScenePath, OpenSceneMode.Additive);

            try
            {
                var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.isRootCanvas && c.gameObject.scene.path == SkillsScenePath);
                if (canvas == null)
                    return null!;

                var copy = Object.Instantiate(canvas.gameObject);
                copy.name = "SkillsScreen";
                SceneManager.MoveGameObjectToScene(copy, targetScene);

                StripCanvasComponents(copy);
                PrepareRectForLobbyOverlay(copy.GetComponent<RectTransform>());

                var nav = copy.GetComponentInChildren<SkillsSceneNavigationController>(true);
                if (nav != null)
                    Object.DestroyImmediate(nav);

                return copy;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void StripCanvasComponents(GameObject root)
        {
            var raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Object.DestroyImmediate(raycaster);

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
                Object.DestroyImmediate(scaler);

            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
                Object.DestroyImmediate(canvas);
        }

        private static void PrepareRectForLobbyOverlay(RectTransform rt)
        {
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject BuildSkillsModal(Transform parent, Scene targetScene)
        {
            var screenRoot = CloneSkillsCanvasRoot(targetScene);
            if (screenRoot == null)
                throw new System.InvalidOperationException("Не удалось скопировать Canvas из Skills.unity");

            var modal = new GameObject("SkillsModal", typeof(RectTransform), typeof(UiPanelToggle));
            Undo.RegisterCreatedObjectUndo(modal, "Create SkillsModal");
            modal.transform.SetParent(parent, false);
            StretchFull(modal.GetComponent<RectTransform>());

            var backdrop = CreateBackdrop(modal.transform, UiModalStyle.BackdropColor);

            Undo.RegisterCreatedObjectUndo(screenRoot, "Skills screen root");
            screenRoot.transform.SetParent(modal.transform, false);
            screenRoot.transform.SetAsLastSibling();
            var screenRt = screenRoot.GetComponent<RectTransform>();
            UiModalStyle.ApplyPanelRect(screenRt);
            var screenImg = screenRoot.GetComponent<Image>();
            if (screenImg == null)
                screenImg = screenRoot.AddComponent<Image>();
            UiModalStyle.ApplyPanel(screenImg);

            var screenVlg = screenRoot.GetComponent<VerticalLayoutGroup>();
            if (screenVlg != null)
            {
                var pad = (int)UiModalStyle.SkillsModalPadding;
                screenVlg.padding = new RectOffset(pad, pad, pad, pad);
                screenVlg.spacing = UiModalStyle.SkillsSectionSpacing;
                screenVlg.childControlWidth = true;
                screenVlg.childControlHeight = true;
                screenVlg.childForceExpandWidth = true;
                screenVlg.childForceExpandHeight = false;
            }

            var toggle = modal.GetComponent<UiPanelToggle>();
            var so = new SerializedObject(toggle);
            so.FindProperty("panelRoot").objectReferenceValue = modal;
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var sync = modal.GetComponent<SkillsModalLayoutSync>();
            if (sync == null)
                sync = modal.AddComponent<SkillsModalLayoutSync>();
            sync.Apply();

            return modal;
        }

        private static GameObject CreateBackdrop(Transform parent, Color color)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.SetAsFirstSibling();
            StretchFull(backdrop.GetComponent<RectTransform>());
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = color;
            backdropImg.raycastTarget = true;
            var backdropBtn = backdrop.GetComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.targetGraphic = backdropImg;
            return backdrop;
        }

        private static List<GameObject> CloneDetailsUi(Scene targetScene)
        {
            var scene = EditorSceneManager.OpenScene(DetailsScenePath, OpenSceneMode.Additive);
            var clones = new List<GameObject>();

            try
            {
                var controller = Object.FindObjectsByType<SkillDetailsScreenController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.gameObject.scene.path == DetailsScenePath);
                if (controller == null)
                    return clones;

                var copy = Object.Instantiate(controller.gameObject);
                copy.name = "SkillDetailsContent";
                SceneManager.MoveGameObjectToScene(copy, targetScene);
                StripNestedCanvas(copy);
                clones.Add(copy);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            return clones;
        }

        private static void StripNestedCanvas(GameObject root)
        {
            var canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                return;

            var canvasTransform = canvas.transform;
            var parent = canvasTransform.parent;
            var children = new List<Transform>();
            foreach (Transform child in canvasTransform)
                children.Add(child);

            foreach (var child in children)
                child.SetParent(parent, false);

            Object.DestroyImmediate(canvas.gameObject);
        }

        private static GameObject BuildDetailsModalShell(Transform parent, List<GameObject> sourceRoots)
        {
            var modal = new GameObject(
                "SkillDetailsModal",
                typeof(RectTransform),
                typeof(UiPanelToggle),
                typeof(SkillDetailsModalLayoutSync));
            Undo.RegisterCreatedObjectUndo(modal, "Create SkillDetailsModal");
            modal.transform.SetParent(parent, false);
            StretchFull(modal.GetComponent<RectTransform>());

            var toggle = modal.GetComponent<UiPanelToggle>();
            var backdrop = CreateBackdrop(modal.transform, UiModalStyle.BackdropColor);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(modal.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            UiModalStyle.ApplyPanelRect(contentRt);
            UiModalStyle.ApplyPanel(content.GetComponent<Image>());

            foreach (var src in sourceRoots)
            {
                Undo.RegisterCreatedObjectUndo(src, "Place details UI");
                src.transform.SetParent(content.transform, false);
                PrepareRectForLobbyOverlay(src.GetComponent<RectTransform>());
                src.SetActive(true);
            }

            var so = new SerializedObject(toggle);
            so.FindProperty("panelRoot").objectReferenceValue = modal;
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return modal;
        }

        private static void WireLobbyModals(Transform canvas, GameObject skillsModal, GameObject detailsModal)
        {
            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            if (host == null)
            {
                var lobbyController = Object.FindFirstObjectByType<LobbyScreenController>();
                var go = lobbyController != null
                    ? lobbyController.gameObject
                    : new GameObject("LobbyModalsHost");
                if (lobbyController == null)
                    Undo.RegisterCreatedObjectUndo(go, "LobbyModalsHost");
                host = Undo.AddComponent<LobbyModalsHost>(go);
            }

            var hostSo = new SerializedObject(host);
            hostSo.FindProperty("skillsModal").objectReferenceValue = skillsModal.GetComponent<UiPanelToggle>();
            hostSo.FindProperty("skillDetailsModal").objectReferenceValue = detailsModal.GetComponent<UiPanelToggle>();
            var profileToggle = Object.FindFirstObjectByType<ProfilePanelToggle>();
            if (profileToggle != null)
                hostSo.FindProperty("profilePanel").objectReferenceValue = profileToggle;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            var skillsPanel = skillsModal.GetComponentInChildren<SkillsPanelController>(true);
            if (skillsPanel != null)
            {
                var panelSo = new SerializedObject(skillsPanel);
                panelSo.FindProperty("lobbyModals").objectReferenceValue = host;
                panelSo.FindProperty("navigationController").objectReferenceValue = null;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var detailsController = detailsModal.GetComponentInChildren<SkillDetailsScreenController>(true);
            if (detailsController != null)
            {
                var detailsSo = new SerializedObject(detailsController);
                detailsSo.FindProperty("panelToggle").objectReferenceValue =
                    detailsModal.GetComponent<UiPanelToggle>();
                detailsSo.FindProperty("lobbyModals").objectReferenceValue = host;
                detailsSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var lobby = Object.FindFirstObjectByType<LobbyScreenController>();
            if (lobby != null)
            {
                var lobbySo = new SerializedObject(lobby);
                lobbySo.FindProperty("lobbyModals").objectReferenceValue = host;
                lobbySo.ApplyModifiedPropertiesWithoutUndo();
            }

            var btnSkills = GameObject.Find("BtnSkills")?.GetComponent<Button>();
            if (btnSkills != null)
            {
                Undo.RecordObject(btnSkills, "Wire BtnSkills");
                btnSkills.onClick.RemoveAllListeners();
                btnSkills.onClick.AddListener(host.OpenSkills);
            }
        }

        private static void RewireSkillsBackButton(GameObject skillsModal)
        {
            var toggle = skillsModal.GetComponent<UiPanelToggle>();
            var back = FindChildButton(skillsModal.transform, "Back Button");
            if (back == null || toggle == null)
                return;

            Undo.RecordObject(back, "Wire skills back");
            UiModalStyle.PrepareBackButton(back);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(toggle.Hide);
        }

        private static void RewireDetailsBackButton(GameObject detailsModal)
        {
            var toggle = detailsModal.GetComponent<UiPanelToggle>();
            var back = FindChildButton(detailsModal.transform, "Back Button");
            if (back == null || toggle == null)
                return;

            Undo.RecordObject(back, "Wire details back");
            UiModalStyle.PrepareBackButton(back);
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(toggle.Hide);
        }

        private static Button? FindChildButton(Transform root, string name)
        {
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn.gameObject.name == name)
                    return btn;
            }

            return null;
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
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        [MenuItem("Tap/Apply Lobby Modal Styles")]
        public static void ApplyLobbyModalStyles()
        {
            var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            SetupProfileModalInScene(showDoneDialog: false);
            ApplySkillDetailsModalStyle();
            ApplySkillsModalStyle();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Готово", "Стили модалок применены к сцене Lobby.", "OK");
        }

        [MenuItem("Tap/Setup Lobby Profile Modal")]
        public static void SetupLobbyProfileModal()
        {
            if (!EditorUtility.DisplayDialog(
                    "Lobby Profile Modal",
                    "Привести ProfilePanel к единому стилю модалок?",
                    "Выполнить",
                    "Отмена"))
                return;

            SetupProfileModalInScene(showDoneDialog: true);
        }

        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        private static void SetupProfileModalInScene(bool showDoneDialog)
        {
            Undo.SetCurrentGroupName("Setup Lobby Profile Modal");
            var group = Undo.GetCurrentGroup();

            var panel = LobbyEditorObjectFind.FindProfilePanel();
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "ProfilePanel не найден в Lobby.", "OK");
                return;
            }

            var sync = panel.GetComponent<ProfileModalLayoutSync>();
            if (sync == null)
                sync = Undo.AddComponent<ProfileModalLayoutSync>(panel);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(BackArrowSpriteGuid));
            if (sprite != null)
            {
                var so = new SerializedObject(sync);
                so.FindProperty("backArrowSprite").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var rootImg = panel.GetComponent<Image>();
            if (rootImg != null)
                rootImg.enabled = false;

            sync.Apply();

            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            var toggle = Object.FindFirstObjectByType<ProfilePanelToggle>();
            if (host != null && toggle != null)
            {
                var hostSo = new SerializedObject(host);
                hostSo.FindProperty("profilePanel").objectReferenceValue = toggle;
                hostSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(panel.scene);
            Undo.CollapseUndoOperations(group);
            if (showDoneDialog)
                EditorUtility.DisplayDialog("Готово", "ProfilePanel обновлён.", "OK");
        }

        private static void ApplySkillDetailsModalStyle()
        {
            var modal = LobbyEditorObjectFind.Find("SkillDetailsModal");
            if (modal == null)
                return;

            if (modal.GetComponent<SkillDetailsModalLayoutSync>() == null)
                modal.AddComponent<SkillDetailsModalLayoutSync>();
        }

        private static void ApplySkillsModalStyle()
        {
            var skills = LobbyEditorObjectFind.Find("SkillsModal");
            if (skills == null)
                return;

            var sync = skills.GetComponent<SkillsModalLayoutSync>();
            if (sync == null)
                sync = skills.AddComponent<SkillsModalLayoutSync>();
            sync.Apply();

            foreach (var toggle in skills.GetComponents<UiPanelToggle>())
            {
                var so = new SerializedObject(toggle);
                if (so.FindProperty("openAnimDuration").floatValue <= 0f)
                {
                    so.FindProperty("openAnimDuration").floatValue = UiModalSlideAnimator.DefaultDuration;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
