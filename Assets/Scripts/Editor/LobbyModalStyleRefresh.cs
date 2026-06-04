using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TapBrawl.Editor
{
    public static class LobbyModalStyleRefresh
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";

        // Меню: Tap → Apply Lobby Modal Styles (см. LobbySkillsModalSetup)

        private static void EnsureSkillDetails()
        {
            var modal = LobbyEditorObjectFind.Find("SkillDetailsModal");
            if (modal == null)
                return;

            if (modal.GetComponent<SkillDetailsModalLayoutSync>() == null)
                modal.AddComponent<SkillDetailsModalLayoutSync>();
        }

        private static void TouchSkillsModals()
        {
            var skills = LobbyEditorObjectFind.Find("SkillsModal");
            if (skills == null)
                return;

            if (skills.GetComponent<SkillsModalLayoutSync>() == null)
                skills.AddComponent<SkillsModalLayoutSync>();

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
