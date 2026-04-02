using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.UI.ViewModels.History
{
    public sealed class HistoryEntryViewModel : ViewModelBase
    {
        public HistorySessionSnapshot Snapshot { get; }

        public HistoryEntryViewModel(HistorySessionSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public string TargetName    => string.IsNullOrEmpty(Snapshot.TargetName)
            ? $"Mob #{Snapshot.TargetId}"
            : Snapshot.TargetName;

        public string DateDisplay   => Snapshot.SessionEnd.ToString("dd.MM  HH:mm:ss");
        public string Duration      => DamageFormatter.FormatDuration(Snapshot.Duration);
        public bool   IsCompleted   => Snapshot.State == SessionState.Completed;
        public string StateDisplay  => IsCompleted ? "?" : "?";
        public string StateColor    => IsCompleted ? "#888888" : "#4EC9B0";
        public int    PlayerCount   => Snapshot.PlayerStats.Count;

        public string TotalDamageDisplay
        {
            get
            {
                long total = Snapshot.PlayerStats.Sum(p => p.TotalDamage);
                return DamageFormatter.Format(total);
            }
        }
    }
}
