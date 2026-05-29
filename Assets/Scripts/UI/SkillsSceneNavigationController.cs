using UnityEngine;
using UnityEngine.SceneManagement;
using TapBrawl.Network;

namespace TapBrawl.UI
{
    /// <summary>
    /// Простая навигация для отдельной сцены прокачки скиллов.
    /// Все вызовы методов назначаются вручную через Unity Inspector.
    /// </summary>
    public sealed class SkillsSceneNavigationController : MonoBehaviour
    {
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private string skillsSceneName = "Skills";
        [SerializeField] private string skillDetailsSceneName = "SkillDetails";

        public void OpenSkillsScene()
        {
            if (!string.IsNullOrEmpty(skillsSceneName))
                SceneManager.LoadScene(skillsSceneName, LoadSceneMode.Single);
        }

        public void OpenLobbyScene()
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }

        public void OpenSkillDetailsSceneForSkill(int skillId)
        {
            if (string.IsNullOrEmpty(skillDetailsSceneName))
                return;

            PendingSkillDetails.Set(skillId);
            SceneManager.LoadScene(skillDetailsSceneName, LoadSceneMode.Single);
        }
    }
}
