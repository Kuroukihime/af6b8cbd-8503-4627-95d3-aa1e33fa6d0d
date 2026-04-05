using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class ActiveTargetResolver
    {
        private static readonly TimeSpan RecentHitWindow = TimeSpan.FromSeconds(5);

        private readonly EntityTracker entityTracker;

        public int? ActiveTargetId { get; private set; }

        public ActiveTargetResolver(EntityTracker entityTracker)
        {
            this.entityTracker = entityTracker;
        }

        public void Update(IEnumerable<TargetEntry> entries, DateTime lastHitTime)
        {
            var cutoff = lastHitTime - RecentHitWindow;

            var activeEntries = entries
                .Where(e => e.CurrentSession is not null && !e.CurrentSession.IsCompleted)
                .ToList();

            var userBest = activeEntries
                .Select(e => (entry: e, userLastHit: e.GetUserLastHitTime()))
                .Where(x => x.userLastHit.HasValue && x.userLastHit.Value >= cutoff)
                .OrderByDescending(x => x.userLastHit!.Value)
                .FirstOrDefault();

            if (userBest.entry is not null)
            {
                ActiveTargetId = userBest.entry.TargetId;
                return;
            }

            var best = activeEntries
                .Select(e => (entry: e, count: e.CountRecentHits(cutoff)))
                .Where(x => x.count > 0)
                .OrderByDescending(x => x.count)
                .FirstOrDefault();

            ActiveTargetId = best.entry?.TargetId;
        }

        public Mob? GetActiveTargetMob()
        {
            if (ActiveTargetId is not { } targetId) return null;
            return entityTracker.GetTargetMob(targetId);
        }

        public void Reset() => ActiveTargetId = null;
    }
}