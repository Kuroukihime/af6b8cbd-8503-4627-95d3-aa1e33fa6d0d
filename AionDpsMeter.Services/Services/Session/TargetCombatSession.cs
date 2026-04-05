using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public enum SessionState
    {
        Active,
        Completed
    }

    public sealed class TargetCombatSession
    {
        private readonly Dictionary<long, PlayerSession> playerSessions = new();
        private readonly EntityTracker entityTracker;

        public int TargetId { get; }
        public Mob TargetInfo { get; }
        public DateTime SessionStart { get; }
        public DateTime LastHitTime { get; private set; }

        public DateTime? CompletedAt { get; private set; }
        public SessionState State { get; private set; } = SessionState.Active;

        public bool IsCompleted => State == SessionState.Completed;

        public TargetCombatSession(Mob targetInfo, DateTime sessionStart, EntityTracker entityTracker)
        {
            TargetId = targetInfo.Id;
            TargetInfo = targetInfo;
            SessionStart = sessionStart;
            LastHitTime = sessionStart;
            this.entityTracker = entityTracker;
        }

        public void AddDamage(PlayerDamage damage)
        {
            if (!playerSessions.TryGetValue(damage.SourceEntity.Id, out var session))
            {
                session = new PlayerSession(damage.SourceEntity.Id, entityTracker);
                playerSessions[damage.SourceEntity.Id] = session;
            }

            session.AddDamage(damage);

            if (damage.DateTime > LastHitTime)
                LastHitTime = damage.DateTime;
        }

     
        public void Complete(DateTime completedAt)
        {
            if (IsCompleted) return;
            State = SessionState.Completed;
            CompletedAt = completedAt;
        }

        public TimeSpan GetCombatDuration()
        {
            var duration = LastHitTime - SessionStart;
            return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        }

        public IReadOnlyCollection<PlayerStats> GetPlayerStats()
        {
            var activeSessions = playerSessions.Values
                .Where(s => s.HitCount > 0)
                .ToList();

            if (activeSessions.Count == 0)
                return [];

            long totalDamage = activeSessions.Sum(s => s.TotalDamage);

            return activeSessions
                .Select(s => DamageStatisticsCalculator.ComputePlayerStats(s, totalDamage))
                .OrderByDescending(s => s.TotalDamage)
                .ToList();
        }

        public IReadOnlyList<PlayerDamage> GetCombatLog(long playerId)
        {
            if (!playerSessions.TryGetValue(playerId, out var session))
                return [];

            return [.. session.Hits.AsEnumerable().Reverse()];
        }

        public IReadOnlyCollection<SkillStats> GetSkillStats(long playerId)
        {
            if (!playerSessions.TryGetValue(playerId, out var session))
                return [];

            return DamageStatisticsCalculator.ComputeSkillStats(session);
        }

        public int CountRecentHits(DateTime cutoff)
            => playerSessions.Values.Sum(s => s.CountHitsAfter(cutoff));

        public DateTime? GetUserLastHitTime()
            => playerSessions.Values
                .Where(s => s.IsUser && s.LastHit.HasValue)
                .Select(s => s.LastHit!.Value)
                .Cast<DateTime?>()
                .Max();

        public void Reset()
        {
            foreach (var session in playerSessions.Values)
                session.Reset();
            playerSessions.Clear();
        }
    }
}