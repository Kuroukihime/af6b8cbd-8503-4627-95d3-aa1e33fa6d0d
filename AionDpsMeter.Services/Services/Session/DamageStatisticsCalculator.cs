using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Pure computation: aggregates damage events into <see cref="PlayerStats"/> and <see cref="SkillStats"/>.
    /// </summary>
    public static class DamageStatisticsCalculator
    {
        /// <summary>
        /// Calculates per-player combat stats from their damage history, filtered to the active target.
        /// </summary>
        public static void UpdatePlayerStats(PlayerStats stats, IReadOnlyList<PlayerDamage> damageHistory, int activeTargetId, long totalCombatDamage)
        {
            long totalDamage = 0;
            int hitCount = 0, criticalHits = 0, backAttacks = 0, perfectHits = 0, doubleDamageHits = 0, parryHits = 0;
            var firstHit = DateTime.MaxValue;
            var lastHit = DateTime.MinValue;

            foreach (var hit in damageHistory)
            {
                if (hit.TargetEntity.Id != activeTargetId) continue;

                totalDamage += hit.Damage;
                hitCount++;
                if (hit.IsCritical) criticalHits++;
                if (hit.IsBackAttack) backAttacks++;
                if (hit.IsPerfect) perfectHits++;
                if (hit.IsDoubleDamage) doubleDamageHits++;
                if (hit.IsParry) parryHits++;
                if (hit.DateTime < firstHit) firstHit = hit.DateTime;
                if (hit.DateTime > lastHit) lastHit = hit.DateTime;
            }

            stats.TotalDamage = totalDamage;
            stats.HitCount = hitCount;
            stats.CriticalHits = criticalHits;
            stats.BackAttacks = backAttacks;
            stats.PerfectHits = perfectHits;
            stats.DoubleDamageHits = doubleDamageHits;
            stats.ParryHits = parryHits;

            if (hitCount == 0)
            {
                stats.DamagePerSecond = 0;
                stats.DamagePercentage = 0;
                return;
            }

            stats.FirstHit = firstHit;
            stats.LastHit = lastHit;

            var duration = (lastHit - firstHit).TotalSeconds;
            if (duration < 0.1) duration = 0.1;

            stats.DamagePerSecond = totalDamage / duration;
            stats.DamagePercentage = totalCombatDamage > 0
                ? (double)totalDamage / totalCombatDamage * 100
                : 0;
        }

        /// <summary>
        /// Builds a sorted list of per-skill stats from a player's damage history, filtered to the active target.
        /// </summary>
        public static IReadOnlyCollection<SkillStats> CalculateSkillStats(
            IReadOnlyList<PlayerDamage> damageHistory,
            int activeTargetId,
            TimeSpan playerCombatDuration,
            long playerTotalDamage)
        {
            var skillMap = new Dictionary<long, SkillStats>();

            foreach (var hit in damageHistory)
            {
                if (hit.TargetEntity.Id != activeTargetId) continue;

                if (!skillMap.TryGetValue(hit.Skill.Id, out var skill))
                {
                    skill = new SkillStats
                    {
                        SkillId = hit.Skill.Id,
                        SkillName = hit.Skill.Name,
                        SkillIcon = hit.Skill.Icon,
                        MinHit = long.MaxValue
                    };
                    skillMap[hit.Skill.Id] = skill;
                }

                skill.TotalDamage += hit.Damage;
                skill.HitCount++;
                if (hit.IsCritical) skill.CriticalHits++;
                if (hit.IsBackAttack) skill.BackAttacks++;
                if (hit.IsPerfect) skill.PerfectHits++;
                if (hit.IsDoubleDamage) skill.DoubleDamageHits++;
                if (hit.IsParry) skill.ParryHits++;
                if (hit.Damage < skill.MinHit) skill.MinHit = hit.Damage;
                if (hit.Damage > skill.MaxHit) skill.MaxHit = hit.Damage;
            }

            var durationSeconds = playerCombatDuration.TotalSeconds;
            if (durationSeconds < 0.1) durationSeconds = 0.1;

            foreach (var skill in skillMap.Values)
            {
                skill.DamagePercentage = playerTotalDamage > 0
                    ? (double)skill.TotalDamage / playerTotalDamage * 100
                    : 0;
                skill.DamagePerSecond = skill.TotalDamage / durationSeconds;
            }

            var result = new List<SkillStats>(skillMap.Values);
            result.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));
            return result;
        }
    }
}
