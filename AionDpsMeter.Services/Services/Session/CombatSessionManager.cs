using AionDpsMeter.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class CombatSessionManager
    {
        private readonly ConcurrentDictionary<int, TargetEntry> targetEntries = new();
        private readonly ActiveTargetResolver targetResolver;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<CombatSessionManager> logger;
        private readonly Lock lockObject = new();


        public CombatSessionManager(EntityTracker entityTracker, ILoggerFactory loggerFactory)
        {
            this.entityTracker = entityTracker;
            targetResolver = new ActiveTargetResolver(entityTracker);
            logger = loggerFactory.CreateLogger<CombatSessionManager>();
        }

      

      
        public TargetCombatSession? GetActiveTargetSession()
        {
            lock (lockObject)
            {
                if (targetResolver.ActiveTargetId is not { } id) return null;
                return targetEntries.TryGetValue(id, out var entry) ? entry.CurrentSession : null;
            }
        }

      
        public IEnumerable<TargetCombatSession> GetTargetHistory(int targetId)
        {
            lock (lockObject)
            {
                return targetEntries.TryGetValue(targetId, out var entry)
                    ? entry.AllSessions.ToList()
                    : [];
            }
        }

        public IReadOnlyCollection<TargetEntry> AllTargetEntries
        {
            get { lock (lockObject) { return targetEntries.Values.ToList(); } }
        }

      
        public IReadOnlyList<HistorySessionSnapshot> GetHistorySnapshot()
        {
            lock (lockObject)
            {
                return targetEntries.Values
                    .SelectMany(e => e.AllSessions)
                    .OrderByDescending(s => s.LastHitTime)
                    .Select(HistorySessionSnapshot.From)
                    .ToList();
            }
        }

        public IReadOnlyCollection<PlayerStats> PlayerStats =>
            GetActiveTargetSession()?.GetPlayerStats() ?? [];

        public IReadOnlyList<PlayerDamage> GetPlayerCombatLog(long playerId) =>
            GetActiveTargetSession()?.GetCombatLog(playerId) ?? [];

        public IReadOnlyCollection<SkillStats> GetPlayerSkillStats(long playerId) =>
            GetActiveTargetSession()?.GetSkillStats(playerId) ?? [];

        public TimeSpan GetCombatDuration()
        {
            lock (lockObject)
            {
                return GetActiveTargetSession()?.GetCombatDuration() ?? TimeSpan.Zero;
            }
        }

        public Mob? GetActiveTargetInfo()
        {
            lock (lockObject) { return targetResolver.GetActiveTargetMob(); }
        }

       

        public void ProcessDamageEvent(PlayerDamage damageEvent)
        {
            try
            {
                lock (lockObject)
                {
                    if (entityTracker.IsSummon(damageEvent.SourceEntity.Id) &&
                        !ResolveSummonSource(damageEvent))
                        return;
                    RouteToTargetEntry(damageEvent);

                    // Check all other entries for idle timeout on each new event
                    CheckIdleTimeouts(damageEvent.DateTime, excludeTargetId: damageEvent.TargetEntity.Id);

                    targetResolver.Update(targetEntries.Values, damageEvent.DateTime);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing damage event");
            }
        }

        public void Reset()
        {
            lock (lockObject) { ResetInternal(); }
        }

        

        private void RouteToTargetEntry(PlayerDamage damageEvent)
        {
            var entry = targetEntries.GetOrAdd(
                damageEvent.TargetEntity.Id,
                id => new TargetEntry(id, entityTracker));

            entry.AddDamage(damageEvent);
        }

        private void CheckIdleTimeouts(DateTime now, int excludeTargetId)
        {
            foreach (var (id, entry) in targetEntries)
            {
                if (id == excludeTargetId) continue;
                entry.CheckIdleTimeout(now);
            }
        }

        private bool ResolveSummonSource(PlayerDamage damageEvent)
        {
            var ownerId = entityTracker.GetSummonOwner(damageEvent.SourceEntity.Id);
            if (ownerId is null)
            {
                logger.LogError("Summon owner not found for summon entity {SummonId}", damageEvent.SourceEntity.Id);
                return false;
            }

            damageEvent.SourceEntity = new Player
            {
                CharacterClass = damageEvent.SourceEntity.CharacterClass,
                Icon = damageEvent.SourceEntity.Icon,
                Name = damageEvent.SourceEntity.Name,
                Id = ownerId.Value,
            };
            return true;
        }

        private void ResetInternal()
        {
            foreach (var entry in targetEntries.Values)
                entry.Reset();
            targetEntries.Clear();
            targetResolver.Reset();
        }
    }
}