using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Determines the active target — the mob that is being hit most frequently
    /// within a recent time window across all player sessions.
    /// </summary>
    public sealed class ActiveTargetResolver
    {
        private static readonly TimeSpan RecentHitWindow = TimeSpan.FromSeconds(5);

        private readonly EntityTracker entityTracker;
        private readonly Dictionary<int, int> hitCountBuffer = new();

        public int? ActiveTargetId { get; private set; }

        public ActiveTargetResolver(EntityTracker entityTracker)
        {
            this.entityTracker = entityTracker;
        }

        /// <summary>
        /// Re-evaluates the active target by counting recent hits across all sessions.
        /// </summary>
        public void Update(IEnumerable<PlayerSession> sessions, DateTime lastHitTime)
        {
            var cutoff = lastHitTime - RecentHitWindow;
            hitCountBuffer.Clear();

            foreach (var session in sessions)
            {
                session.CountRecentTargetHits(cutoff, hitCountBuffer);
            }

            ActiveTargetId = FindMostHitTarget();
        }

        public Mob? GetActiveTargetMob()
        {
            if (ActiveTargetId is not { } targetId) return null;
            return entityTracker.GetTargetMob(targetId);
        }

        public void Reset()
        {
            ActiveTargetId = null;
            hitCountBuffer.Clear();
        }

        private int? FindMostHitTarget()
        {
            if (hitCountBuffer.Count == 0) return null;

            int bestTargetId = 0;
            int bestCount = 0;

            foreach (var (targetId, count) in hitCountBuffer)
            {
                if (count > bestCount)
                {
                    bestCount = count;
                    bestTargetId = targetId;
                }
            }

            return bestTargetId;
        }
    }
}
