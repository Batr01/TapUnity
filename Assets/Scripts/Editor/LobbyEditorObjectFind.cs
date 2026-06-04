using TapBrawl.UI;
using UnityEditor;
using UnityEngine;

namespace TapBrawl.Editor
{
    /// <summary>Поиск объектов Lobby, в т.ч. неактивных (GameObject.Find их не видит).</summary>
    internal static class LobbyEditorObjectFind
    {
        public const string LobbyScenePath = "Assets/Scenes/Lobby.unity";

        public static GameObject? Find(string objectName)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (t.name != objectName || t.gameObject.scene.path != LobbyScenePath)
                    continue;
                return t.gameObject;
            }

            return null;
        }

        public static GameObject? FindProfilePanel()
        {
            foreach (var toggle in Object.FindObjectsByType<ProfilePanelToggle>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (toggle.gameObject.scene.path != LobbyScenePath)
                    continue;

                var so = new SerializedObject(toggle);
                var root = so.FindProperty("profilePanelRoot").objectReferenceValue as GameObject;
                if (root != null)
                    return root;
            }

            foreach (var ctrl in Object.FindObjectsByType<ProfilePanelController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (ctrl.gameObject.scene.path == LobbyScenePath)
                    return ctrl.gameObject;
            }

            return Find("ProfilePanel");
        }
    }
}
