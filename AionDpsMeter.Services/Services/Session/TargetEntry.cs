using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;
using static System.Net.Mime.MediaTypeNames;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Tracks all combat attempts (tries) against a single mob.
    /// Detects mob HP resets to automatically start a new <see cref="TargetCombatSession"/>.
    /// Exposes the current in-progress session and the full history of completed sessions.
    /// </summary>
    public sealed class TargetEntry
    {
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ScarecrowIdleTimeout = TimeSpan.FromSeconds(5);

        private readonly EntityTracker entityTracker;
        private readonly List<TargetCombatSession> history = new();

        public int TargetId { get; }

        public TargetCombatSession? CurrentSession { get; private set; }

        public IReadOnlyList<TargetCombatSession> History => history;

        public IEnumerable<TargetCombatSession> AllSessions =>
            CurrentSession is null ? history : [.. history, CurrentSession];

        public TargetEntry(int targetId, EntityTracker entityTracker)
        {
            TargetId = targetId;
            this.entityTracker = entityTracker;
        }

        public void AddDamage(PlayerDamage damage)
        {
            var mob = entityTracker.GetTargetMob(damage.TargetEntity.Id) ?? damage.TargetEntity;

            if (CurrentSession is not null && IsNewTry(mob) || ShouldCompleteSession(damage.DateTime, mob))
            {
                CompleteCurrentSession(damage.DateTime);
                StartNewSession(mob, damage.DateTime);
            }
            else if (CurrentSession is null)
            {
                StartNewSession(mob, damage.DateTime);
            }

            CurrentSession!.AddDamage(damage);
        }

        public void CheckIdleTimeout(DateTime now)
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return;

            if (ShouldCompleteSession(now)) CompleteCurrentSession(now);
        }


        private bool ShouldCompleteSession(DateTime now, Mob? currentMobState = null)
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return false;

            if(currentMobState?.Name == "Training Scarecrow") 
                return now - CurrentSession.LastHitTime > ScarecrowIdleTimeout;

            return now - CurrentSession.LastHitTime > IdleTimeout;
        }

        public int CountRecentHits(DateTime cutoff)
            => CurrentSession?.CountRecentHits(cutoff) ?? 0;

        public DateTime? GetUserLastHitTime()
            => CurrentSession?.GetUserLastHitTime();

        public void Reset()
        {
            CurrentSession?.Reset();
            CurrentSession = null;
            foreach (var s in history) s.Reset();
            history.Clear();
        }


        private bool IsNewTry(Mob currentMobState)
        {
            if (CurrentSession is null) return false;

            if (currentMobState.Name == "Training Scarecrow") return false;

            // HP increasing means the mob respawned / fight restarted
            var lastKnownHp = CurrentSession.TargetInfo.HpCurrent;
            return currentMobState.HpCurrent > lastKnownHp && lastKnownHp > 0;
        }

        private void CompleteCurrentSession(DateTime at)
        {
            if (CurrentSession is null) return;
            CurrentSession.Complete(at);
            history.Add(CurrentSession);
            CurrentSession = null;
        }

        private void StartNewSession(Mob mob, DateTime at)
        {
            CurrentSession = new TargetCombatSession(mob, at, entityTracker);
        }
    }
}