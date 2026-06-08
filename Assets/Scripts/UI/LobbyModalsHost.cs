using TapBrawl.Network;
using UnityEngine;

namespace TapBrawl.UI
{
    /// <summary>
    /// Модалки лобби: скиллы и детали скилла поверх Lobby без смены сцены.
    /// </summary>
    public sealed class LobbyModalsHost : MonoBehaviour
    {
        public static LobbyModalsHost? Instance { get; private set; }

        [SerializeField] private UiPanelToggle skillsModal = null!;
        [SerializeField] private UiPanelToggle skillDetailsModal = null!;
        [SerializeField] private UiPanelToggle? shopModal;
        [SerializeField] private UiPanelToggle? settingsModal;
        [SerializeField] private UiPanelToggle? friendsModal;
        [SerializeField] private ProfilePanelToggle? profilePanel;

        private void Awake()
        {
            TryAutoWire();
            Instance = this;
        }

        private void TryAutoWire()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                if (skillsModal == null)
                    skillsModal = FindToggle(canvas.transform, "SkillsModal");
                if (skillDetailsModal == null)
                    skillDetailsModal = FindToggle(canvas.transform, "SkillDetailsModal");
                if (shopModal == null)
                    shopModal = FindToggle(canvas.transform, "ShopModal");
                if (settingsModal == null)
                    settingsModal = FindToggle(canvas.transform, "SettingsModal");
                if (friendsModal == null)
                    friendsModal = FindToggle(canvas.transform, "FriendsModal");
            }

            if (profilePanel == null)
                profilePanel = Object.FindFirstObjectByType<ProfilePanelToggle>();
        }

        private static UiPanelToggle? FindToggle(Transform parent, string modalName)
        {
            var t = parent.Find(modalName);
            return t != null ? t.GetComponent<UiPanelToggle>() : null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OpenFriends()
        {
            TryAutoWire();
            if (friendsModal == null)
            {
                Debug.LogError(
                    "[LobbyModalsHost] FriendsModal не найден. В Unity: Tap → Setup Lobby Friends Modal.",
                    this);
                return;
            }

            CloseProfile();
            CloseShop();
            CloseSettings();
            CloseAllSkillModals();
            friendsModal.Show();
        }

        public void CloseFriends()
        {
            if (friendsModal != null)
                friendsModal.Hide();
        }

        public void OpenShop()
        {
            TryAutoWire();
            if (shopModal == null)
            {
                Debug.LogError(
                    "[LobbyModalsHost] ShopModal не найден. В Unity: Tap → Setup Lobby Shop Modal.",
                    this);
                return;
            }

            CloseProfile();
            CloseSettings();
            CloseFriends();
            CloseAllSkillModals();
            shopModal.Show();
        }

        public void CloseShop()
        {
            if (shopModal != null)
                shopModal.Hide();
        }

        public void OpenSettings()
        {
            TryAutoWire();
            if (settingsModal == null)
            {
                Debug.LogError(
                    "[LobbyModalsHost] SettingsModal не найден. В Unity: Tap → Setup Lobby Settings Modal.",
                    this);
                return;
            }

            CloseProfile();
            CloseShop();
            CloseFriends();
            CloseAllSkillModals();
            settingsModal.Show();
        }

        public void CloseSettings()
        {
            if (settingsModal != null)
                settingsModal.Hide();
        }

        public void OpenSkills()
        {
            if (!EnsureModalsReady())
                return;
            CloseShop();
            CloseSettings();
            CloseFriends();
            CloseProfile();
            CloseSkillDetails();
            skillsModal.Show();
        }

        public void CloseSkills()
        {
            if (skillsModal != null)
                skillsModal.Hide();
        }

        public void OpenSkillDetails(int skillId)
        {
            if (!EnsureModalsReady())
                return;
            CloseShop();
            CloseSettings();
            CloseFriends();
            CloseProfile();
            PendingSkillDetails.Set(skillId);
            skillDetailsModal.Show();
        }

        public void CloseProfile()
        {
            if (profilePanel != null)
                profilePanel.Hide();
        }

        public void CloseAllSkillModals()
        {
            CloseSkills();
            CloseSkillDetails();
        }

        public void CloseSkillDetails()
        {
            if (skillDetailsModal != null)
                skillDetailsModal.Hide();
        }

        private bool EnsureModalsReady()
        {
            TryAutoWire();
            if (skillsModal != null && skillDetailsModal != null)
                return true;

            Debug.LogError(
                "[LobbyModalsHost] Модалки не найдены. В Unity: Tap → Setup Lobby Skills Modals (закройте Play Mode).",
                this);
            return false;
        }
    }
}
