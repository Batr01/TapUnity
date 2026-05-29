namespace TapBrawl.Network
{
    /// <summary>
    /// Одноразовая передача выбранного скилла из сцены Skills в сцену SkillDetails.
    /// </summary>
    public static class PendingSkillDetails
    {
        private static bool _hasPending;
        private static int _skillId;

        public static void Set(int skillId)
        {
            _hasPending = true;
            _skillId = skillId;
        }

        public static bool TryConsume(out int skillId)
        {
            skillId = default;
            if (!_hasPending)
                return false;

            skillId = _skillId;
            _hasPending = false;
            return true;
        }

        public static void Clear() => _hasPending = false;
    }
}
