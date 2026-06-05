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
        private static readonly string BackArrowSpriteGuid = "21f5fdb6dcd364639938114508a30534";

        // Меню: Tap → Setup Lobby Profile Modal (см. LobbySkillsModalSetup)

        public static void RunSetup(bool showDoneDialog = true)
        {
            Undo.SetCurrentGroupName("Setup Lobby Profile Modal");
            var group = Undo.GetCurrentGroup();

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
            sync.Apply();
            WireHostProfileReference();
            WireProfileCurrencyReferences(panel);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);
            if (showDoneDialog)
                EditorUtility.DisplayDialog("Готово", "ProfilePanel обновлён.", "OK");
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
