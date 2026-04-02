using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IPacketService _packetService;
        private readonly CombatSessionManager _sessionManager;
        private readonly IAppSettingsService _settingsService;
        private readonly Dispatcher _dispatcher;
        private DispatcherTimer? _updateTimer;

        [ObservableProperty] private ObservableCollection<PlayerStatsViewModel> _players = new();
        [ObservableProperty] private string _combatDuration = "00:00";
        [ObservableProperty] private string _pingDisplay = "-- ms";
        [ObservableProperty] private string _pingColor = "#888888";
        [ObservableProperty] private string _activeTargetName = string.Empty;
        [ObservableProperty] private int _activeTargetHpTotal;
        [ObservableProperty] private int _activeTargetHpCurrent;
        [ObservableProperty] private bool _hasActiveTarget;
        [ObservableProperty] private string _activeTargetHpDisplay = string.Empty;
        [ObservableProperty] private double _activeTargetHpPercentage;

        /// <summary>Exposes the session manager for <c>PlayerDetailsWindow</c>.</summary>
        public CombatSessionManager SessionManager => _sessionManager;

        public MainViewModel(IPacketService packetService, CombatSessionManager sessionManager, IAppSettingsService settingsService)
        {
            _packetService  = packetService;
            _sessionManager = sessionManager;
            _settingsService = settingsService;
            _dispatcher     = Dispatcher.CurrentDispatcher;

            _packetService.DamageReceived    += OnPacketReceived;
            _packetService.PingUpdated       += OnPingUpdated;

            // UI refresh at ~30 FPS
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _updateTimer.Tick += OnUpdateTimerTick;

            StartCapture();
        }

        private void StartCapture()
        {
            _packetService.Start();
            _updateTimer?.Start();
        }

        private void OnPingUpdated(object? sender, int pingMs)
        {
            _dispatcher.BeginInvoke(() =>
            {
                PingDisplay = $"{pingMs} ms";
                PingColor   = pingMs switch
                {
                    < 60  => "#4EC9B0",  // excellent
                    < 100 => "#DCDCAA",  // good
                    < 200 => "#CE9178",  // mediocre
                    _     => "#F44747"   // bad
                };
            });
        }

        [RelayCommand]
        private void ResetData()
        {
            _packetService.Reset();
            _sessionManager.Reset();
            ClearUiState();
        }

        private void OnPacketReceived(object? sender, PlayerDamage damageEvent)
            => _sessionManager.ProcessDamageEvent(damageEvent);

        private void OnCombatAutoReset(object? sender, EventArgs e)
            => _dispatcher.BeginInvoke(ClearUiState);

        private void ClearUiState()
        {
            Players.Clear();
            CombatDuration        = "00:00";
            ActiveTargetName      = string.Empty;
            ActiveTargetHpTotal   = 0;
            ActiveTargetHpCurrent = 0;
            HasActiveTarget       = false;
            ActiveTargetHpDisplay = string.Empty;
            ActiveTargetHpPercentage = 0;
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
        {
            UpdatePlayerStats();
            UpdateCombatDuration();
            UpdateActiveTarget();
        }

        private void UpdatePlayerStats()
        {
            foreach (var stats in _sessionManager.PlayerStats)
            {
                var existing = Players.FirstOrDefault(p => p.PlayerId == stats.PlayerId);
                if (existing is not null)
                    existing.Update(stats);
                else
                    Players.Add(new PlayerStatsViewModel(stats, _settingsService));
            }

            var sorted = Players.Where(p => p.TotalDamage > 0)
                                 .OrderByDescending(p => p.TotalDamage)
                                 .ToList();
            Players.Clear();
            foreach (var player in sorted)
                Players.Add(player);
        }

        private void UpdateCombatDuration()
            => CombatDuration = _sessionManager.GetCombatDuration().ToString(@"mm\:ss");

        private void UpdateActiveTarget()
        {
            var targetInfo = _sessionManager.GetActiveTargetInfo();
            if (targetInfo is not null)
            {
                HasActiveTarget      = true;
                ActiveTargetName     = targetInfo.Name;
                ActiveTargetHpTotal  = targetInfo.HpTotal;
                ActiveTargetHpCurrent = targetInfo.HpCurrent;
                ActiveTargetHpPercentage = targetInfo.HpTotal > 0
                    ? (double)targetInfo.HpCurrent / targetInfo.HpTotal * 100
                    : 0;
                ActiveTargetHpDisplay = targetInfo.HpTotal > 0
                    ? $"{DamageFormatter.Format(targetInfo.HpCurrent)} / {DamageFormatter.Format(targetInfo.HpTotal)}"
                    : string.Empty;
            }
            else
            {
                HasActiveTarget          = false;
                ActiveTargetName         = string.Empty;
                ActiveTargetHpTotal      = 0;
                ActiveTargetHpCurrent    = 0;
                ActiveTargetHpPercentage = 0;
                ActiveTargetHpDisplay    = string.Empty;
            }
        }

        public void Dispose()
        {
            _packetService.DamageReceived   -= OnPacketReceived;
            _packetService.PingUpdated      -= OnPingUpdated;
            _updateTimer?.Stop();

            if (_packetService is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
