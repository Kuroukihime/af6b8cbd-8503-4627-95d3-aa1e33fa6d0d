using AionDpsMeter.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Orchestrates a combat session: routes damage events, delegates timeline tracking
    /// to <see cref="CombatTimelineTracker"/>, active target resolution to
    /// <see cref="ActiveTargetResolver"/>, and stat computation to <see cref="PlayerSession"/>.
    /// </summary>
    public sealed class CombatSessionManager
    {
        private readonly ConcurrentDictionary<long, PlayerSession> playerSessions = new();
        private readonly CombatTimelineTracker timeline = new();
        private readonly ActiveTargetResolver targetResolver;
        private readonly object lockObject = new();
        private readonly ILogger<CombatSessionManager> logger;
        private readonly EntityTracker entityTracker;

        public event EventHandler? CombatAutoReset;


        public CombatSessionManager(EntityTracker entityTracker, ILoggerFactory loggerFactory)
        {
            this.entityTracker = entityTracker;
            targetResolver = new ActiveTargetResolver(entityTracker);
            logger = loggerFactory.CreateLogger<CombatSessionManager>();
        }

       

        public IReadOnlyCollection<PlayerStats> PlayerStats
        {
            get
            {
                lock (lockObject)
                {
                    return playerSessions.Values
                        .Where(s => s.Stats.HitCount > 0)
                        .Select(s => s.Stats)
                        .ToList();
                }
            }
        }

        public IReadOnlyList<PlayerDamage> GetPlayerCombatLog(long playerId)
        {
            lock (lockObject)
            {
                if (targetResolver.ActiveTargetId is { } targetId
                    && playerSessions.TryGetValue(playerId, out var session))
                {
                    var filtered = session.GetDamageHistory(targetId);
                    var result = new List<PlayerDamage>(filtered);
                    result.Reverse();
                    return result;
                }
                return Array.Empty<PlayerDamage>();
            }
        }

        public IReadOnlyCollection<SkillStats> GetPlayerSkillStats(long playerId)
        {
            lock (lockObject)
            {
                if (targetResolver.ActiveTargetId is { } targetId
                    && playerSessions.TryGetValue(playerId, out var session))
                    return session.GetSkillStats(targetId);
                return Array.Empty<SkillStats>();
            }
        }

        public TimeSpan GetCombatDuration()
        {
            lock (lockObject)
            {
                return timeline.GetCombatDuration();
            }
        }

        public Mob? GetActiveTargetInfo()
        {
            lock (lockObject)
            {
                return targetResolver.GetActiveTargetMob();
            }
        }


        public void ProcessDamageEvent(PlayerDamage damageEvent)
        {
            try
            {
                lock (lockObject)
                {
                    if (timeline.ShouldAutoReset(damageEvent.DateTime, damageEvent.TargetEntity.Id))
                    {
                        ResetInternal();
                        CombatAutoReset?.Invoke(this, EventArgs.Empty);
                    }

                    timeline.RecordHit(damageEvent.DateTime, damageEvent.TargetEntity.Id);

                    if (entityTracker.IsSummon(damageEvent.SourceEntity.Id))
                    {
                        if (!ResolveSummonDamageEvent(damageEvent)) return;
                    }

                    AddDamageToSession(damageEvent);

                    RecalculateStatistics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing damage event");
            }
        }

        private bool ResolveSummonDamageEvent(PlayerDamage damageEvent)
        {
            var ownerId = entityTracker.GetSummonOwner(damageEvent.SourceEntity.Id);
            if (ownerId == null)
            {
                // This shouldn't happen since IsSummon returned true, but log just in case
                logger.LogError("Summon owner not found for summon entity {SummonId}", damageEvent.SourceEntity.Id);
                return false;
            }

            damageEvent.SourceEntity = new Player()
            {
                CharacterClass = damageEvent.SourceEntity.CharacterClass,
                Icon = damageEvent.SourceEntity.Icon,
                Name = damageEvent.SourceEntity.Name,
                Id = ownerId.Value,
            };
            return true;
        }


        public void Reset()
        {
            lock (lockObject)
            {
                ResetInternal();
            }
        }

        private void AddDamageToSession(PlayerDamage damageEvent)
        {
            var session = playerSessions.GetOrAdd(
                damageEvent.SourceEntity.Id,
                _ => new PlayerSession(damageEvent));
            session.AddDamage(damageEvent);
        }

        private void RecalculateStatistics()
        {
            if (timeline.LastHitTime is not { } lastHitTime) return;

            targetResolver.Update(playerSessions.Values, lastHitTime);

            if (targetResolver.ActiveTargetId is not { } targetId) return;

            // Pass 1: compute per-player totals (percentage = 0 since total is unknown)
            foreach (var session in playerSessions.Values)
                session.UpdateStats(targetId, totalCombatDamage: 0);

            // Pass 2: now compute percentages with the real total
            long totalDamage = playerSessions.Values.Sum(s => s.Stats.TotalDamage);
            foreach (var session in playerSessions.Values)
                session.UpdateStats(targetId, totalDamage);
        }

        private void ResetInternal()
        {
            foreach (var session in playerSessions.Values)
                session.Reset();
            playerSessions.Clear();
            timeline.Reset();
            targetResolver.Reset();
        }
    }
}
