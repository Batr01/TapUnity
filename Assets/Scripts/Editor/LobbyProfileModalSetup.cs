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

            sync.Apply();
            WireHostProfileReference();

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);
            if (showDoneDialog)
                EditorUtility.DisplayDialog("Готово", "ProfilePanel обновлён.", "OK");
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
