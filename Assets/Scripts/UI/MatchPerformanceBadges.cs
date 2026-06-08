using System.Collections.Generic;
using TapBrawl.Models;

namespace TapBrawl.UI
{
    public sealed class MatchBadge
    {
        public string Id { get; }
        public string Title { get; }
        public string Emoji { get; }
        public string Description { get; }

        public MatchBadge(string id, string title, string emoji, string description)
        {
            Id = id;
            Title = title;
            Emoji = emoji;
            Description = description;
        }
    }

    public static class MatchPerformanceBadges
    {
        public static IReadOnlyList<MatchBadge> Resolve(MatchPlayerResultDto me, bool won)
        {
            var badges = new List<MatchBadge>();
            var accuracy = me.AccuracyPercent;
            var tps = me.TapsPerSecond;

            if (accuracy >= 85)
                badges.Add(new MatchBadge("sniper", "Снайпер", "🎯", $"Точность {accuracy:0.#}%"));
            else if (accuracy >= 75)
                badges.Add(new MatchBadge("marksman", "Меткий стрелок", "🏹", $"Точность {accuracy:0.#}%"));

            if (won && accuracy >= 80)
                badges.Add(new MatchBadge("calm_winner", "Хладнокровный", "🧊", "Победа с высокой точностью"));

            if (tps >= 2.3 && accuracy >= 70)
                badges.Add(new MatchBadge("machine", "Машина", "⚡", $"{tps:0.#} тап/сек"));

            if (me.Taps >= 130)
                badges.Add(new MatchBadge("storm", "Шторм", "🌪", $"{me.Taps} тапов за матч"));

            if (me.Misses <= 15 && me.Taps >= 40)
                badges.Add(new MatchBadge("clean", "Чистая игра", "✨", $"Всего {me.Misses} промахов"));

            if (accuracy < 60 && me.Taps >= 90)
                badges.Add(new MatchBadge("panic", "Паникёр", "😵", "Много тапов, мало попаданий"));

            if (tps >= 2.3 && accuracy < 70)
                badges.Add(new MatchBadge("lightning", "Молния", "⚡", $"{tps:0.#} тап/сек"));

            if (tps >= 1.5 && tps <= 2.2 && accuracy >= 75)
                badges.Add(new MatchBadge("steady", "Размеренный", "🎵", "Спокойный темп и хорошая точность"));

            if (me.Taps < 80)
                badges.Add(new MatchBadge("rookie_spam", "Разминка", "🐣", "Мало тапов — есть куда расти"));

            return badges;
        }

        public static string FormatTopBadges(IReadOnlyList<MatchBadge> badges, int maxCount = 2)
        {
            if (badges.Count == 0)
                return string.Empty;

            var count = System.Math.Min(maxCount, badges.Count);
            var lines = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                var b = badges[i];
                lines.Add($"{b.Emoji} {b.Title} — {b.Description}");
            }

            return string.Join("\n", lines);
        }
    }
}
