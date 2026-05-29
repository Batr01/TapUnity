using System;
using System.Collections.Generic;
using TapBrawl.Core;
using UnityEngine;

namespace TapBrawl.Core.Skills
{
    /// <summary>
    /// Единый ассет: иконки, тексты, loop-звуки, префабы VFX по skillId. Логика матча остаётся в коде.
    /// </summary>
    [CreateAssetMenu(menuName = "TapBrawl/Skill Catalog", fileName = "SkillCatalog")]
    public sealed class SkillCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public int skillId = MatchSkillIds.GiantCirclesSelfBuff;

            [Tooltip("Пусто — подставится из SkillDefinitions.")]
            public string displayName = "";

            [Tooltip("Пусто — подставится из SkillDefinitions.")]
            [TextArea(1, 4)]
            public string shortHint = "";

            public Sprite? icon;

            [Tooltip("Вторая строка подписи слота на матче (если не задана — текст из сцены по умолчанию).")]
            public string idleSlotSuffix = "";

            [Tooltip("Строка уведомления, когда соперник применил этот скилл по сети.")]
            public string opponentAppliedNotice = "";

            [Tooltip("Подсказка «на вас» при одном активном дебаффе. {0} — оставшиеся секунды.")]
            public string incomingDebuffSingleFormat = "";

            [Header("Loop-звук (пока эффект активен на клиенте)")]
            public AudioClip? loopClip;

            [Range(0f, 1f)]
            public float loopVolume = 0.5f;

            [Header("Только для соответствующих skillId")]
            public GameObject? smokeVeilEffectPrefab;

            public GameObject? chainBoltEffectPrefab;

            [Min(0f)]
            public float chainDischargeStepDelayUnscaledSec = 0.12f;
        }

        [Tooltip("Когда одновременно красная пелена и дым. {0} — сек пелены, {1} — сек дыма.")]
        [SerializeField]
        private string incomingDebuffBothFormat = "На вас: пелена {0:0.#}с, дым {1:0.#}с";

        [SerializeField]
        private string defaultOpponentUnknownSkillNotice = "Соперник применил скилл";

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<int, Entry> _byId = new();

        private void OnEnable() => RebuildCache();

        private void OnValidate()
        {
            RebuildCache();
            ValidateEditor();
        }

        public void RebuildCache()
        {
            _byId = new Dictionary<int, Entry>();
            foreach (var e in entries)
            {
                if (e == null)
                    continue;
                if (_byId.ContainsKey(e.skillId))
                {
                    Debug.LogWarning($"[SkillCatalog] Дубликат skillId {e.skillId} в {name}", this);
                    continue;
                }

                _byId[e.skillId] = e;
            }
        }

        private void ValidateEditor()
        {
            foreach (var known in SkillBalance.KnownSkillIds)
            {
                if (!_byId.ContainsKey(known))
                    Debug.LogWarning($"[SkillCatalog] Нет записи для skillId {known} ({name})", this);
            }
        }

        public bool TryGetEntry(int skillId, out Entry entry) => _byId.TryGetValue(skillId, out entry!);

        public string GetDisplayName(int skillId) =>
            TryGetEntry(skillId, out var e) && !string.IsNullOrWhiteSpace(e.displayName)
                ? e.displayName
                : SkillDefinitions.GetDisplayName(skillId);

        public string GetShortHint(int skillId) =>
            TryGetEntry(skillId, out var e) && !string.IsNullOrWhiteSpace(e.shortHint)
                ? e.shortHint
                : SkillDefinitions.GetShortHint(skillId);

        public Sprite? GetIcon(int skillId) =>
            TryGetEntry(skillId, out var e) ? e.icon : null;

        public string? GetIdleSlotSuffix(int skillId) =>
            TryGetEntry(skillId, out var e) && !string.IsNullOrWhiteSpace(e.idleSlotSuffix)
                ? e.idleSlotSuffix
                : null;

        public string GetOpponentAppliedNotice(int skillType) =>
            TryGetEntry(skillType, out var e) && !string.IsNullOrWhiteSpace(e.opponentAppliedNotice)
                ? e.opponentAppliedNotice
                : defaultOpponentUnknownSkillNotice;

        public string? GetIncomingDebuffSingleFormat(int skillId) =>
            TryGetEntry(skillId, out var e) && !string.IsNullOrWhiteSpace(e.incomingDebuffSingleFormat)
                ? e.incomingDebuffSingleFormat
                : null;

        public string FormatIncomingDebuffBoth(float redSeconds, float smokeSeconds) =>
            string.Format(incomingDebuffBothFormat, redSeconds, smokeSeconds);

        public GameObject? GetSmokeVeilEffectPrefab() =>
            TryGetEntry(MatchSkillIds.OpponentSmokeVeil, out var e) ? e.smokeVeilEffectPrefab : null;

        public GameObject? GetChainBoltEffectPrefab() =>
            TryGetEntry(MatchSkillIds.ChainDischargeSelfBuff, out var e) ? e.chainBoltEffectPrefab : null;

        public float GetChainDischargeStepDelayUnscaledSec()
        {
            if (TryGetEntry(MatchSkillIds.ChainDischargeSelfBuff, out var e))
                return Mathf.Max(0f, e.chainDischargeStepDelayUnscaledSec);
            return 0.12f;
        }

        public IEnumerable<Entry> EnumerateEntriesWithLoopAudio()
        {
            foreach (var e in entries)
            {
                if (e != null && e.loopClip != null)
                    yield return e;
            }
        }
    }
}
