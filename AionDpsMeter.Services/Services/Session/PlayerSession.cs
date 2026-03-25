using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Stores the damage history for a single player within a combat session.
    /// Provides queries filtered by active target and delegates stat computation
    /// to <see cref="DamageStatisticsCalculator"/>.
    /// </summary>
    public sealed class PlayerSession
    {
        public long PlayerId { get; }
        public string PlayerName { get; }
        public string? PlayerIcon { get; }
        public long ClassId { get; }
        public string ClassName { get; }
        public string? ClassIcon { get; }

        private readonly List<PlayerDamage> damageHistory = new();

        public PlayerStats Stats { get; private set; }

        public PlayerSession(PlayerDamage firstDamage)
        {
            PlayerId = firstDamage.SourceEntity.Id;
            PlayerName = firstDamage.SourceEntity.Name;
            PlayerIcon = firstDamage.SourceEntity.Icon;
            ClassId = firstDamage.CharacterClass.Id;
            ClassName = firstDamage.CharacterClass.Name;
            ClassIcon = firstDamage.CharacterClass.Icon;
            Stats = CreateEmptyStats(firstDamage.DateTime);
        }

        public void AddDamage(PlayerDamage damage) => damageHistory.Add(damage);

        /// <summary>
        /// Counts how many hits each target received after <paramref name="cutoff"/>.
        /// Used by <see cref="ActiveTargetResolver"/> to determine the active target.
        /// </summary>
        public void CountRecentTargetHits(DateTime cutoff, Dictionary<int, int> targetCounts)
        {
            for (int i = damageHistory.Count - 1; i >= 0; i--)
            {
                var damage = damageHistory[i];
                if (damage.DateTime < cutoff) break;

                var targetId = damage.TargetEntity.Id;
                targetCounts.TryGetValue(targetId, out var count);
                targetCounts[targetId] = count + 1;
            }
        }

        /// <summary>
        /// Recalculates this player's stats against the given target.
        /// Called in two passes: first with totalCombatDamage=0 to compute per-player totals,
        /// then with the real total to compute damage percentage.
        /// </summary>
        public void UpdateStats(int activeTargetId, long totalCombatDamage)
        {
            DamageStatisticsCalculator.UpdatePlayerStats(Stats, damageHistory, activeTargetId, totalCombatDamage);
        }

        public IReadOnlyList<PlayerDamage> GetDamageHistory(int activeTargetId)
        {
            var result = new List<PlayerDamage>();
            foreach (var damage in damageHistory)
            {
                if (damage.TargetEntity.Id == activeTargetId)
                    result.Add(damage);
            }
            return result;
        }

        public IReadOnlyCollection<SkillStats> GetSkillStats(int activeTargetId)
        {
            return DamageStatisticsCalculator.CalculateSkillStats(
                damageHistory,
                activeTargetId,
                Stats.CombatDuration,
                Stats.TotalDamage);
        }

        public void Reset()
        {
            damageHistory.Clear();
            Stats = CreateEmptyStats();
        }

        private PlayerStats CreateEmptyStats(DateTime? initialTime = null)
        {
            return new PlayerStats
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                PlayerIcon = PlayerIcon,
                ClassName = ClassName,
                ClassIcon = ClassIcon,
                FirstHit = initialTime ?? default,
                LastHit = initialTime ?? default
            };
        }
    }
}