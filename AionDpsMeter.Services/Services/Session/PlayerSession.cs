using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class PlayerSession
    {
        public long PlayerId { get; }
        public string PlayerName { get; }
        public string? PlayerIcon { get; }
        public long ClassId { get; }
        public string ClassName { get; }
        public string? ClassIcon { get; }

        private readonly List<PlayerDamage> damageHistory = new();
        private readonly Dictionary<long, SkillStats> skillStats = new();
        private readonly Dictionary<long, List<long>> skillDamageCache = new();
        private readonly ILogger<PlayerSession> logger;
        public PlayerStats Stats { get; private set; }

        public PlayerSession(PlayerDamage firstDamage, ILogger<PlayerSession> logger)
        {
            this.logger = logger;
            PlayerId = firstDamage.SourceEntity.Id;
            PlayerName = firstDamage.SourceEntity.Name;
            PlayerIcon = firstDamage.SourceEntity.Icon;
            ClassId = firstDamage.CharacterClass.Id;
            ClassName = firstDamage.CharacterClass.Name;
            ClassIcon = firstDamage.CharacterClass.Icon;
            Stats = new PlayerStats
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                PlayerIcon = PlayerIcon,
                ClassName = ClassName,
                ClassIcon = ClassIcon,
                FirstHit = firstDamage.DateTime,
                LastHit = firstDamage.DateTime
            };
        }

        public void AddDamage(PlayerDamage damage)
        {
            //var validatedDamage = ValidateAndCorrectDamage(damage);
            //if (validatedDamage is null)
            //{
            //    logger.LogWarning("Ignored invalid damage for player '{PlayerName}' ({ClassName}): Skill={SkillName}, Damage={Damage}",
            //        PlayerName, ClassName, damage.Skill.Name, damage.Damage);
            //    return;
            //}

            //sometimes we still get wrong damage offset so it goes to some abnormal values. Gonna use it till ff-ff stcruture fully solved
            if (!SanityCheck(damage.Damage))
            {
                logger.LogWarning($"Damage sanity check failed! SKill {damage.Skill.Name} Damage: {damage.Damage}");
                return; 
            }
            damageHistory.Add(damage);
           // CacheSkillDamage(validatedDamage);
        }

        private bool SanityCheck(long damage) => damage < 500_000;
       
       
        private PlayerDamage? ValidateAndCorrectDamage(PlayerDamage damage)
        {
            var oldDmg = damage.Damage;
            var (isValid, averageDamage, tolerance) = ValidateDamageValue(damage);

            if (isValid)
            {
                return damage;
            }

            var correctedDamage = FindBestPotentialDamage(damage, averageDamage, tolerance);
            if (correctedDamage > 0)
            {
                damage.Damage = correctedDamage;
                logger.LogInformation("Corrected damage for player '{PlayerName}' ({ClassName}): Skill={SkillName}, AvgDamage={AverageDamage}, PreviousDamage={OldDamage}, CorrectedDamage={CorrectedDamage}",
                    PlayerName, ClassName, damage.Skill.Name, averageDamage, oldDmg, damage.Damage);
                return damage;
            }

            return null;
        }

        private (bool IsValid, double AverageDamage, double Tolerance) ValidateDamageValue(PlayerDamage damage)
        {
            const long minDamage = 300;
            const long maxDamage = 1_000_000;

            var history = GetNormalizedDamageHistory(damage.Skill.Id);

            if (history.Count == 0)
            {
                bool basicValid = damage.Damage > minDamage && damage.Damage < maxDamage;
                return (basicValid, 0, 0);
            }

            double average = GetAvg(damage, history);
            var (minAllowed, maxAllowed, tolerance) = CalculateAllowedDamageRange(average, history.Count, minDamage, maxDamage);

            bool isValid = damage.Damage >= minAllowed && damage.Damage <= maxAllowed;
            return (isValid, average, tolerance);
        }
        private static (double MinAllowed, double MaxAllowed, double Tolerance) CalculateAllowedDamageRange(
            double averageDamage,
            int sampleCount,
            long absoluteMin,
            long absoluteMax)
        {
            const int normalSampleCount = 10;         // Number of hits required for stable statistics
            const double targetTolerance = 3.0;       // Target deviation (2x) with sufficient data
            const double maxInitialTolerance = 150.0; // Maximum initial deviation for the first hit

            // Formula: tolerance decreases as more data is accumulated
            // With 1 hit: tolerance ≈ maxInitialTolerance
            // With normalSampleCount hits: tolerance ≈ targetTolerance
            // Exponential decay is used for a smooth transition
            double decayFactor = Math.Exp(
                -Math.Log(maxInitialTolerance / targetTolerance) *
                (sampleCount - 1) / (normalSampleCount - 1));

            double tolerance = Math.Max(targetTolerance, maxInitialTolerance * decayFactor);

            // Clamp tolerance to reasonable bounds
            tolerance = Math.Clamp(tolerance, targetTolerance, maxInitialTolerance);

            // Calculate bounds based on average damage and tolerance
            double calculatedMin = averageDamage / tolerance;
            double calculatedMax = averageDamage * tolerance;

            // Apply absolute limits
            double minAllowed = Math.Max(absoluteMin, calculatedMin);
            double maxAllowed = Math.Min(absoluteMax, calculatedMax);

            // Ensure minimum does not exceed maximum
            if (minAllowed > maxAllowed)
            {
                minAllowed = absoluteMin;
                maxAllowed = absoluteMax;
            }

            return (minAllowed, maxAllowed, tolerance);
        }

        private double GetAvg(PlayerDamage damage, List<double> history)
        {

            double average = history.Average();
            if (damage.IsParry) average *= 0.5;
            if (damage.IsDoubleDamage) average *= 2;
            if(damage.IsCritical) average *= 1.5;
            return average;
        }

        private List<double> GetNormalizedDamageHistory(long key)
        {
            var result = new List<double>();

            if (skillDamageCache.TryGetValue(key, out var exactHistory))
            {
                result.AddRange(exactHistory.Select(d => (double)d));
            }

            return result;
        }

        private long FindBestPotentialDamage(PlayerDamage damage, double averageDamage, double tolerance)
        {
            if (damage.PotentialDamageData is null || damage.PotentialDamageData.Length == 0)
            {
                return 0;
            }
            
            const long minDamage = 10;
            const long maxDamage = 1_000_000;

            if (averageDamage == 0)
            {
                return 0;
            }

            long bestValue = 0;
            double minDeviation = double.MaxValue;

            foreach (var potentialDamage in damage.PotentialDamageData)
            {
                if (potentialDamage <= minDamage || potentialDamage >= maxDamage)
                    continue;

                double deviation = Math.Abs(potentialDamage - averageDamage) / averageDamage;

                if (deviation <= tolerance && deviation < minDeviation)
                {
                    minDeviation = deviation;
                    bestValue = potentialDamage;
                }
            }

            return bestValue;
        }

        private void CacheSkillDamage(PlayerDamage damage)
        {
            double normalizedDamage = damage.Damage;
            if (damage.IsParry) normalizedDamage *= 2;      
            if (damage.IsDoubleDamage) normalizedDamage *= 0.5;
            if (damage.IsCritical) normalizedDamage *= 0.75;


            if (!skillDamageCache.TryGetValue(damage.Skill.Id, out var list))
            {
                list = new List<long>();
                skillDamageCache[damage.Skill.Id] = list;
            }

            list.Add((long)normalizedDamage);
        }

        public void UpdateStats(long totalCombatDamage)
        {
            long totalDamage = 0;
            int hitCount = 0, criticalHits = 0, backAttacks = 0, perfectHits = 0, doubleDamageHits = 0, parryHits = 0;
            DateTime lastHit = Stats.FirstHit;

            foreach (var r in damageHistory)
            {
                totalDamage += r.Damage;
                hitCount++;
                if (r.IsCritical) criticalHits++;
                if (r.IsBackAttack) backAttacks++;
                if (r.IsPerfect) perfectHits++;
                if (r.IsDoubleDamage) doubleDamageHits++;
                if (r.IsParry) parryHits++;
                if (r.DateTime > lastHit) lastHit = r.DateTime;
            }

            Stats.TotalDamage = totalDamage;
            Stats.HitCount = hitCount;
            Stats.LastHit = lastHit;
            Stats.CriticalHits = criticalHits;
            Stats.BackAttacks = backAttacks;
            Stats.PerfectHits = perfectHits;
            Stats.DoubleDamageHits = doubleDamageHits;
            Stats.ParryHits = parryHits;

            var playerDuration = (Stats.LastHit - Stats.FirstHit).TotalSeconds;
            if (playerDuration < 0.1) playerDuration = 0.1;
            Stats.DamagePerSecond = totalDamage / playerDuration;
            Stats.DamagePercentage = totalCombatDamage > 0
                ? (double)totalDamage / totalCombatDamage * 100
                : 0;
        }

        public IReadOnlyList<PlayerDamage> GetDamageHistory() => damageHistory;

        public IReadOnlyCollection<SkillStats> GetSkillStats()
        {
            skillStats.Clear();

            foreach (var damage in damageHistory)
            {
                if (!skillStats.TryGetValue(damage.Skill.Id, out var skill))
                {
                    skill = new SkillStats
                    {
                        SkillId = damage.Skill.Id,
                        SkillName = damage.Skill.Name,
                        SkillIcon = damage.Skill.Icon,
                        MinHit = long.MaxValue
                    };
                    skillStats[damage.Skill.Id] = skill;
                }

                skill.TotalDamage += damage.Damage;
                skill.HitCount++;
                if (damage.IsCritical) skill.CriticalHits++;
                if (damage.IsBackAttack) skill.BackAttacks++;
                if (damage.IsPerfect) skill.PerfectHits++;
                if (damage.IsDoubleDamage) skill.DoubleDamageHits++;
                if (damage.IsParry) skill.ParryHits++;
                if (damage.Damage < skill.MinHit) skill.MinHit = damage.Damage;
                if (damage.Damage > skill.MaxHit) skill.MaxHit = damage.Damage;
            }

            var playerDuration = (Stats.LastHit - Stats.FirstHit).TotalSeconds;
            if (playerDuration < 0.1) playerDuration = 0.1;

            foreach (var skill in skillStats.Values)
            {
                skill.DamagePercentage = Stats.TotalDamage > 0
                    ? (double)skill.TotalDamage / Stats.TotalDamage * 100
                    : 0;
                skill.DamagePerSecond = skill.TotalDamage / playerDuration;
            }

            var result = new List<SkillStats>(skillStats.Values);
            result.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));
            return result;
        }

        public void Reset()
        {
            damageHistory.Clear();
            skillStats.Clear();
            skillDamageCache.Clear();
            Stats = new PlayerStats
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                PlayerIcon = PlayerIcon,
                ClassName = ClassName,
                ClassIcon = ClassIcon
            };
        }
    }
}