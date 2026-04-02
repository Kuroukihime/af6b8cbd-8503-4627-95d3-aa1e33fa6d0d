using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
 
    public sealed class HistorySessionSnapshot
    {
        public int TargetId { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public int TargetHpTotal { get; init; }
        public DateTime SessionStart { get; init; }
        public DateTime SessionEnd { get; init; }
        public SessionState State { get; init; }

        public TimeSpan Duration => SessionEnd > SessionStart ? SessionEnd - SessionStart : TimeSpan.Zero;

        public IReadOnlyList<PlayerStats> PlayerStats { get; init; } = [];

        public IReadOnlyDictionary<long, IReadOnlyCollection<SkillStats>> SkillStatsByPlayer { get; init; }
            = new Dictionary<long, IReadOnlyCollection<SkillStats>>();

        public static HistorySessionSnapshot From(TargetCombatSession session)
        {
            var playerStats = session.GetPlayerStats().ToList();

            var skillStats = playerStats.ToDictionary(
                p => p.PlayerId,
                p => (IReadOnlyCollection<SkillStats>)session.GetSkillStats(p.PlayerId).ToList());

            return new HistorySessionSnapshot
            {
                TargetId      = session.TargetId,
                TargetName    = session.TargetInfo.Name,
                TargetHpTotal = session.TargetInfo.HpTotal,
                SessionStart  = session.SessionStart,
                SessionEnd    = session.LastHitTime,
                State         = session.State,
                PlayerStats   = playerStats,
                SkillStatsByPlayer = skillStats,
            };
        }
    }
}
