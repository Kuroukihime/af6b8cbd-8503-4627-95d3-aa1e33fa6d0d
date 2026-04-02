using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.UI.ViewModels.History
{
    public sealed class HistoryViewModel : ViewModelBase
    {
        public IReadOnlyList<HistoryEntryViewModel> Sessions { get; }

        public bool HasSessions => Sessions.Count > 0;

        public HistoryViewModel(IReadOnlyList<HistorySessionSnapshot> snapshots)
        {
            Sessions = snapshots
                .Select(s => new HistoryEntryViewModel(s))
                .ToList();
        }
    }
}
