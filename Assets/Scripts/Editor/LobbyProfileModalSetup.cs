using TapBrawl.Avatars;
using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbyProfileModalSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string AvatarCatalogPath = "Assets/Resources/Avatars/AvatarCatalog.asset";
        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        private static readonly (string Id, string SpriteGuid)[] AvatarSprites =
        {
            ("default", "d55d00e7292104043912bf7b4ba49611"),
            ("blue", "2e2f83f4b6e8d480ab6731798ffd9c30"),
            ("purple", "7bfadc849de524686ba26bb38daeb92b"),
        };

        [MenuItem("Tap/Setup Lobby Profile Modal")]
        public static void RunSetupMenu() => RunSetup(true);

        public static void RunSetup(bool showDoneDialog = true)
        {
            Undo.SetCurrentGroupName("Setup Lobby Profile Modal");
            var group = Undo.GetCurrentGroup();

            EnsureAvatarCatalogAsset();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
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

            EnsureProfileCurrencyTexts(panel.transform);
            EnsureProfileAvatarPicker(panel);
            sync.Apply();
            WireHostProfileReference();
            WireProfileCurrencyReferences(panel);
            WireProfileAvatarReferences(panel);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);
            if (showDoneDialog)
                EditorUtility.DisplayDialog("Готово", "ProfilePanel обновлён (аватары).", "OK");
        }

        private static void EnsureAvatarCatalogAsset()
        {
            EnsureDirectory("Assets/Resources/Avatars");

            var catalog = AssetDatabase.LoadAssetAtPath<AvatarCatalogAsset>(AvatarCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AvatarCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, AvatarCatalogPath);
            }

            var so = new SerializedObject(catalog);
            var entriesProp = so.FindProperty("entries");
            entriesProp.arraySize = AvatarSprites.Length;

            for (var i = 0; i < AvatarSprites.Length; i++)
            {
                var element = entriesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = AvatarSprites[i].Id;
                var path = AssetDatabase.GUIDToAssetPath(AvatarSprites[i].SpriteGuid);
                element.FindPropertyRelative("sprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureProfileAvatarPicker(GameObject panel)
        {
            Transform? block = null;
            foreach (var t in panel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockProfil")
                {
                    block = t;
                    break;
                }
            }

            if (block == null)
                return;

            var avatarUrl = block.Find("avatarUrlInput");
            if (avatarUrl != null)
                avatarUrl.gameObject.SetActive(false);

            var oldRow = block.Find("AvatarRow");
            if (oldRow != null)
            {
                var oldPicker = oldRow.Find("AvatarPickerRow");
                if (oldPicker != null)
                    oldPicker.gameObject.SetActive(false);
            }

            var preview = block.Find("AvatarPreview");
            if (preview == null && oldRow != null)
                preview = oldRow.Find("AvatarPreview");

            if (preview == null)
            {
                var previewGo = new GameObject(
                    "AvatarPreview",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(previewGo, "Create AvatarPreview");
                previewGo.transform.SetParent(block, false);
                previewGo.transform.SetAsFirstSibling();
                preview = previewGo.transform;

                var size = UiModalStyle.AvatarProfileSize;
                var previewLe = previewGo.GetComponent<LayoutElement>();
                previewLe.preferredWidth = size;
                previewLe.preferredHeight = size;
                previewLe.minWidth = size;
                previewLe.minHeight = size;

                var previewImg = previewGo.GetComponent<Image>();
                previewImg.preserveAspect = true;
                previewImg.color = Color.white;
            }

            var picker = panel.GetComponent<ProfileAvatarPicker>();
            if (picker == null)
                picker = Undo.AddComponent<ProfileAvatarPicker>(panel);

            var modal = panel.GetComponent<ProfileAvatarPickModalController>();
            if (modal == null)
                modal = Undo.AddComponent<ProfileAvatarPickModalController>(panel);
        }

        private static void WireProfileAvatarReferences(GameObject panel)
        {
            Transform? block = null;
            foreach (var t in panel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockProfil")
                {
                    block = t;
                    break;
                }
            }

            if (block == null)
                return;

            var preview = block.Find("AvatarPreview");
            if (preview == null)
            {
                var oldRow = block.Find("AvatarRow");
                preview = oldRow?.Find("AvatarPreview");
            }

            if (preview == null)
                return;

            var picker = panel.GetComponent<ProfileAvatarPicker>();
            if (picker == null)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<AvatarCatalogAsset>(AvatarCatalogPath);
            var so = new SerializedObject(picker);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("previewImage").objectReferenceValue = preview.GetComponent<Image>();
            so.FindProperty("previewButton").objectReferenceValue = preview.GetComponent<Button>();
            so.FindProperty("pickModal").objectReferenceValue = panel.GetComponent<ProfileAvatarPickModalController>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var controller = panel.GetComponent<ProfilePanelController>();
            if (controller == null)
                return;

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("avatarPicker").objectReferenceValue = picker;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void EnsureProfileCurrencyTexts(Transform profileRoot)
        {
            Transform? block = null;
            foreach (var t in profileRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockCoint")
                {
                    block = t;
                    break;
                }
            }

            if (block == null)
                return;

            EnsureTextChild(block, "GemsText", "Adipoint: 0");
            EnsureTextChild(block, "EquivalentText", "≈ 0 монет");
        }

        private static void EnsureTextChild(Transform parent, string name, string placeholder)
        {
            if (parent.Find(name) != null)
                return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = placeholder;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = UiModalStyle.ProfileAccentTextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
        }

        private static void WireProfileCurrencyReferences(GameObject panel)
        {
            Transform? block = null;
            foreach (var t in panel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BlockCoint")
                {
                    block = t;
                    break;
                }
            }

            if (block == null)
                return;

            var controller = panel.GetComponent<ProfilePanelController>();
            if (controller == null)
                return;

            var so = new SerializedObject(controller);
            so.FindProperty("coinsText").objectReferenceValue = block.Find("CoinsText")?.GetComponent<Text>();
            so.FindProperty("gemsText").objectReferenceValue = block.Find("GemsText")?.GetComponent<Text>();
            so.FindProperty("equivalentText").objectReferenceValue = block.Find("EquivalentText")?.GetComponent<Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHostProfileReference()
        {
            var host = Object.FindFirstObjectByType<LobbyModalsHost>();
            var toggle = Object.FindFirstObjectByType<ProfilePanelToggle>();
            if (host == null || toggle == null)
                return;

            var hostSo = new SerializedObject(host);
            hostSo.FindProperty("profilePanel").objectReferenceValue = toggle;
            hostSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
