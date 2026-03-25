namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Tracks the combat timeline: when combat started, when the last hit occurred,
    /// and whether a gap between hits indicates the combat session should auto-reset.
    /// </summary>
    public sealed class CombatTimelineTracker
    {
        
        private static readonly TimeSpan HardResetThreshold = TimeSpan.FromSeconds(40);
        private static readonly TimeSpan SoftResetThreshold = TimeSpan.FromSeconds(20);

        private readonly HashSet<int> knownTargetIds = new();

        public DateTime? CombatStartTime { get; private set; }
        public DateTime? LastHitTime { get; private set; }

        
        public bool ShouldAutoReset(DateTime newHitTime, int targetId)
        {
            if (LastHitTime is not { } lastHit) return false;

            var gap = newHitTime - lastHit;

            if (gap > HardResetThreshold)
                return true;

            if (gap > SoftResetThreshold && !knownTargetIds.Contains(targetId))
                return true;

            return false;
        }

        public void RecordHit(DateTime hitTime, int targetId)
        {
            CombatStartTime ??= hitTime;

            if (LastHitTime is null || hitTime > LastHitTime)
                LastHitTime = hitTime;

            knownTargetIds.Add(targetId);
        }

        public TimeSpan GetCombatDuration()
        {
            if (CombatStartTime is null || LastHitTime is null) return TimeSpan.Zero;

            var duration = LastHitTime.Value - CombatStartTime.Value;
            return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        }

        public void Reset()
        {
            CombatStartTime = null;
            LastHitTime = null;
            knownTargetIds.Clear();
        }
    }
}
