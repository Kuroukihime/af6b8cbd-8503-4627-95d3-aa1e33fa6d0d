using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
 
    public static class DamageStatisticsCalculator
    {
        public static PlayerStats ComputePlayerStats(PlayerSession session, long totalCombatDamage)
        {
            var hits = session.Hits;

            var nonDotHits = hits.Where(h => !h.IsDot).ToList();
            int hitCount = nonDotHits.Count;

            var duration = session.FirstHit.HasValue && session.LastHit.HasValue
                ? Math.Max((session.LastHit.Value - session.FirstHit.Value).TotalSeconds, 0.1)
                : 0.1;

            return new PlayerStats
            {
                PlayerId = session.PlayerId,
                PlayerName = session.PlayerName,
                PlayerIcon = session.PlayerIcon,
                ClassName = session.ClassName,
                ClassIcon = session.ClassIcon,
                IsUser = session.IsUser,
                CombatPower = session.CombatPower,
                ServerName = session.ServerName,
                TotalDamage = session.TotalDamage,
                HitCount = hitCount,
                CriticalHits = nonDotHits.Count(h => h.IsCritical),
                BackAttacks = nonDotHits.Count(h => h.IsBackAttack),
                PerfectHits = nonDotHits.Count(h => h.IsPerfect),
                DoubleDamageHits = nonDotHits.Count(h => h.IsDoubleDamage),
                ParryHits = nonDotHits.Count(h => h.IsParry),
                DamagePerSecond = session.TotalDamage / duration,
                DamagePercentage = totalCombatDamage > 0
                    ? (double)session.TotalDamage / totalCombatDamage * 100
                    : 0,
                FirstHit = session.FirstHit ?? default,
                LastHit = session.LastHit ?? default,
            };
        }

        public static IReadOnlyCollection<SkillStats> ComputeSkillStats(PlayerSession session)
        {
            var duration = session.FirstHit.HasValue && session.LastHit.HasValue
                ? Math.Max((session.LastHit.Value - session.FirstHit.Value).TotalSeconds, 0.1)
                : 0.1;

            return session.Hits
                .GroupBy(h => h.Skill.Id)
                .Select(g =>
                {
                    var nonDot = g.Where(h => !h.IsDot).ToList();
                    long totalDmg = g.Sum(h => h.Damage);
                    return new SkillStats
                    {
                        SkillId = g.Key,
                        SkillName = g.First().Skill.Name,
                        SkillIcon = g.First().Skill.Icon,
                        SpecializationFlags = g.First().Skill.SpecializationFlags,
                        IsDot = g.All(h => h.IsDot),
                        TotalDamage = totalDmg,
                        HitCount = g.Count(),
                        CriticalHits = nonDot.Count(h => h.IsCritical),
                        BackAttacks = nonDot.Count(h => h.IsBackAttack),
                        PerfectHits = nonDot.Count(h => h.IsPerfect),
                        DoubleDamageHits = nonDot.Count(h => h.IsDoubleDamage),
                        ParryHits = nonDot.Count(h => h.IsParry),
                        MinHit = g.Min(h => h.Damage),
                        MaxHit = g.Max(h => h.Damage),
                        DamagePerSecond = totalDmg / duration,
                        DamagePercentage = session.TotalDamage > 0
                            ? (double)totalDmg / session.TotalDamage * 100
                            : 0,
                    };
                })
                .OrderByDescending(s => s.TotalDamage)
                .ToList();
        }
    }
}