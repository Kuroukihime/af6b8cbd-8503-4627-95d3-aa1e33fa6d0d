using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.UI.ViewModels.History
{
   
    public sealed class HistorySessionViewModel : ViewModelBase
    {
        private readonly HistorySessionSnapshot _snapshot;

        public string TargetName  => string.IsNullOrEmpty(_snapshot.TargetName)
            ? $"Mob #{_snapshot.TargetId}"
            : _snapshot.TargetName;

        public string DateDisplay => _snapshot.SessionEnd.ToString("dd.MM.yyyy  HH:mm:ss");
        public string Duration    => DamageFormatter.FormatDuration(_snapshot.Duration);
        public bool   IsCompleted => _snapshot.State == SessionState.Completed;
        public string StateLabel  => IsCompleted ? "Completed" : "Active";
        public string StateColor  => IsCompleted ? "#888888" : "#4EC9B0";

        public IReadOnlyList<HistoryPlayerViewModel> Players { get; }

        public HistorySessionViewModel(HistorySessionSnapshot snapshot, IAppSettingsService settingsService)
        {
            _snapshot = snapshot;

            Players = snapshot.PlayerStats
                .OrderByDescending(p => p.TotalDamage)
                .Select(p => new HistoryPlayerViewModel(
                    p,
                    snapshot.SkillStatsByPlayer.TryGetValue(p.PlayerId, out var skills) ? skills : [],
                    settingsService))
                .ToList();
        }
    }
}
