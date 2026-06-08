using System.Collections;
using TapBrawl.Network;
using UnityEngine;

namespace TapBrawl.Core
{
    /// <summary>Симулирует скиллы и пинги бота в матчах без SignalR-соединения соперника.</summary>
    public sealed class BotOpponentSimulator : MonoBehaviour
    {
        private MatchController? _match;
        private Coroutine? _routine;

        public void StartSimulation(MatchController match, in OnlineMatchParams p)
        {
            StopSimulation();
            _match = match;
            var difficulty = ParseDifficulty(p.BotDifficulty);
            _routine = StartCoroutine(RunRoutine(match, p.DurationSec, difficulty));
        }

        public void StopSimulation()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _match = null;
        }

        private static BotDifficultyLevel ParseDifficulty(string? value) =>
            value?.ToLowerInvariant() switch
            {
                "weak" => BotDifficultyLevel.Weak,
                "master" => BotDifficultyLevel.Master,
                _ => BotDifficultyLevel.Medium,
            };

        private IEnumerator RunRoutine(MatchController match, int durationSec, BotDifficultyLevel difficulty)
        {
            var duration = Mathf.Max(1, durationSec);
            StartCoroutine(RunPingRoutine(match, duration, difficulty));
            yield return RunSkillsRoutine(match, duration, difficulty);
        }

        private IEnumerator RunSkillsRoutine(MatchController match, int duration, BotDifficultyLevel difficulty)
        {
            var (minSkills, maxSkills, minGap, maxGap) = difficulty switch
            {
                BotDifficultyLevel.Weak => (0, 1, 25f, 40f),
                BotDifficultyLevel.Master => (2, 3, 12f, 22f),
                _ => (1, 2, 18f, 30f),
            };

            var skillCount = Random.Range(minSkills, maxSkills + 1);
            var skillLevel = difficulty switch
            {
                BotDifficultyLevel.Weak => 3,
                BotDifficultyLevel.Master => 8,
                _ => 5,
            };

            if (skillCount > 0)
            {
                var firstDelay = Random.Range(8f, Mathf.Min(20f, duration * 0.25f));
                yield return new WaitForSecondsRealtime(firstDelay);

                for (var i = 0; i < skillCount; i++)
                {
                    if (!match.IsRunning)
                        yield break;

                    var skillType = Random.value < 0.5f
                        ? MatchSkillIds.OpponentRedDeceptionVisual
                        : MatchSkillIds.OpponentSmokeVeil;
                    match.SimulateBotOpponentSkill(skillType, skillLevel);

                    if (i < skillCount - 1)
                    {
                        var gap = Random.Range(minGap, maxGap);
                        yield return new WaitForSecondsRealtime(gap);
                    }
                }
            }
        }

        private IEnumerator RunPingRoutine(MatchController match, int duration, BotDifficultyLevel difficulty)
        {
            var chance = difficulty switch
            {
                BotDifficultyLevel.Weak => 0.30f,
                BotDifficultyLevel.Master => 0.50f,
                _ => 0.40f,
            };

            if (Random.value > chance)
                yield break;

            var atStart = Random.value < 0.5f;
            float delay;
            if (atStart)
                delay = Random.Range(duration * 0.05f, duration * 0.20f);
            else
                delay = Random.Range(duration * 0.80f, duration * 0.95f);

            yield return new WaitForSecondsRealtime(delay);
            if (!match.IsRunning && !match.IsPostGame)
                yield break;

            var pingType = Random.Range(MatchPingIds.Like, MatchPingIds.Sticker67 + 1);
            match.SimulateBotOpponentPing(pingType);
        }

        private enum BotDifficultyLevel
        {
            Weak,
            Medium,
            Master,
        }
    }
}
