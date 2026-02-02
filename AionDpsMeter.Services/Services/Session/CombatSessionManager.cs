using AionDpsMeter.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class CombatSessionManager
    {
        private readonly ConcurrentDictionary<long, PlayerSession> playerSessions = new();
        private readonly object lockObject = new();
        private DateTime? combatStartTime;
        private readonly ILogger<CombatSessionManager> logger;
        private readonly ILoggerFactory loggerFactory;
        public CombatSessionManager(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<CombatSessionManager>();    
        }

        private long TotalCombatDamage => playerSessions.Values.Sum(s => s.Stats.TotalDamage);

        public IReadOnlyCollection<PlayerStats> PlayerStats
        {
            get
            {
                lock (lockObject)
                {
                    return playerSessions.Values.Select(s => s.Stats).ToList();
                }
            }
        }

        public IReadOnlyList<PlayerDamage> GetPlayerCombatLog(long playerId)
        {
            lock (lockObject)
            {
                if (playerSessions.TryGetValue(playerId, out var session))
                    return session.GetDamageHistory().Reverse().ToList();
                return Array.Empty<PlayerDamage>();
            }
        }

        public void ProcessDamageEvent(PlayerDamage damageEvent)
        {
            try
            {
                lock (lockObject)
                {
                    combatStartTime ??= damageEvent.DateTime;

                    if (damageEvent.Skill.IsEntity)
                    {
                        ProcessEntityDamageEvent(damageEvent);
                        return;
                    }

                    AddDamageEvent(damageEvent);
                    RecalculateStatistics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private void ProcessEntityDamageEvent(PlayerDamage damageEvent)
        {
            var entityClassId = damageEvent.Skill.ClassId;
            var registeredEntitiesOfThisClass = playerSessions.Values.Where(r => r.ClassId == entityClassId).ToList();
            if (registeredEntitiesOfThisClass.Count == 1)
            {
                registeredEntitiesOfThisClass.First().AddDamage(damageEvent);
                return;
            }
            // If there are multiple entities of the same class, we cannot determine which one dealt the damage.
            // In this case, we will register damage event as new entity.
            AddDamageEvent(damageEvent);
            RecalculateStatistics();
        }

        private void AddDamageEvent(PlayerDamage damageEvent)
        {
            var session = playerSessions.GetOrAdd(damageEvent.SourceEntity.Id, _ => new PlayerSession(damageEvent, loggerFactory.CreateLogger<PlayerSession>()));
            session.AddDamage(damageEvent);
        }

        public IReadOnlyCollection<SkillStats> GetPlayerSkillStats(long playerId)
        {
            lock (lockObject)
            {
                if (playerSessions.TryGetValue(playerId, out var session)) return session.GetSkillStats();
                return Array.Empty<SkillStats>();
            }
        }

        private void RecalculateStatistics()
        {
            if (combatStartTime == null) return;

            foreach (var playerSess in playerSessions.Values)
            {
                playerSess.UpdateStats(0);
            }

            var totalDamage = TotalCombatDamage;
            foreach (var playerSess in playerSessions.Values)
            {
                playerSess.UpdateStats(totalDamage);
            }
        }

        public void Reset()
        {
            lock (lockObject)
            {
                foreach (var session in playerSessions.Values)
                    session.Reset();
                playerSessions.Clear();
                combatStartTime = null;
            }
        }
    }
}
