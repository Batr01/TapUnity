using System.Collections.Generic;
using TapBrawl.Models;

namespace TapBrawl.Core.Skills
{
    /// <summary>Снимок скиллов для текущей сессии (лоадаут + уровни). Обновляется из API лобби.</summary>
    public static class PlayerSkillsRuntimeState
    {
        private static readonly Dictionary<int, int> Levels = new();
        private static readonly int[] Loadout = { 1, 2, 3 };

        private static int[]? _trainingArenaLoadoutBackup;

        public static void ApplyFromServer(PlayerSkillsStateDto dto)
        {
            Levels.Clear();
            foreach (var s in dto.Skills)
                Levels[s.SkillId] = s.Level;

            for (var i = 0; i < 3 && i < dto.LoadoutSlotSkillIds.Count; i++)
                Loadout[i] = dto.LoadoutSlotSkillIds[i];
        }

        /// <summary>Тренировка / гость без сохранённого снимка: макс. уровни, лоадаут 1–2–3.</summary>
        public static void ApplyOfflineMaxDefaults()
        {
            Levels.Clear();
            foreach (var id in SkillBalance.KnownSkillIds)
                Levels[id] = SkillBalance.MaxLevel;
            Loadout[0] = TapBrawl.Core.MatchSkillIds.GiantCirclesSelfBuff;
            Loadout[1] = TapBrawl.Core.MatchSkillIds.OpponentRedDeceptionVisual;
            Loadout[2] = TapBrawl.Core.MatchSkillIds.OpponentSmokeVeil;
        }

        /// <summary>Сохраняет лоадаут и ставит «Цепная реакция» в средний слот (только арена тренировки).</summary>
        public static void PushTrainingArenaLoadoutOverride()
        {
            if (_trainingArenaLoadoutBackup != null)
                return;
            _trainingArenaLoadoutBackup = new[] { Loadout[0], Loadout[1], Loadout[2] };
            Loadout[0] = TapBrawl.Core.MatchSkillIds.GiantCirclesSelfBuff;
            Loadout[1] = TapBrawl.Core.MatchSkillIds.ChainDischargeSelfBuff;
            Loadout[2] = TapBrawl.Core.MatchSkillIds.OpponentSmokeVeil;
        }

        public static void PopTrainingArenaLoadoutOverride()
        {
            if (_trainingArenaLoadoutBackup == null)
                return;
            Loadout[0] = _trainingArenaLoadoutBackup[0];
            Loadout[1] = _trainingArenaLoadoutBackup[1];
            Loadout[2] = _trainingArenaLoadoutBackup[2];
            _trainingArenaLoadoutBackup = null;
        }

        public static bool HasAnyData => Levels.Count > 0;

        public static int GetLevel(int skillId) =>
            Levels.TryGetValue(skillId, out var l) ? l : SkillBalance.MaxLevel;

        public static int GetSkillIdInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
                return TapBrawl.Core.MatchSkillIds.GiantCirclesSelfBuff;
            return Loadout[slotIndex];
        }
    }
}
