using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IPacketService packetService;
        private readonly CombatSessionManager _sessionManager;
        private readonly Dispatcher _dispatcher;

        [ObservableProperty]
        private ObservableCollection<PlayerStatsViewModel> _players = new();

        [ObservableProperty]
        private string _combatDuration = "00:00";

        [ObservableProperty]
        private string _pingDisplay = "-- ms";

        [ObservableProperty]
        private string _pingColor = "#888888";

        [ObservableProperty]
        private string _activeTargetName = string.Empty;

        [ObservableProperty]
        private int _activeTargetHpTotal;

        [ObservableProperty]
        private int _activeTargetHpCurrent;

        [ObservableProperty]
        private bool _hasActiveTarget;

        [ObservableProperty]
        private string _activeTargetHpDisplay = string.Empty;

        [ObservableProperty]
        private double _activeTargetHpPercentage;

        private DispatcherTimer? _updateTimer;

        // Expose session manager for PlayerDetailsWindow
        public CombatSessionManager SessionManager => _sessionManager;


        public MainViewModel(IPacketService packetService, CombatSessionManager sessionManager)
        {
            this.packetService = packetService;
            _sessionManager = sessionManager;

            _dispatcher = Dispatcher.CurrentDispatcher;

            // Subscribe to damage events
            this.packetService.DamageReceived += OnPacketReceived;
            this.packetService.PingUpdated += OnPingUpdated;
            _sessionManager.CombatAutoReset += OnCombatAutoReset;

            // Setup update timer for UI refresh (30 FPS)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _updateTimer.Tick += OnUpdateTimerTick;

            // Auto-start: DPS meter is always active
            StartCapture();
        }

        private void StartCapture()
        {
            packetService.Start();
            _updateTimer?.Start();
        }

        private void OnPingUpdated(object? sender, int pingMs)
        {
            _dispatcher.BeginInvoke(() =>
            {
                PingDisplay = $"{pingMs} ms";
                PingColor = pingMs switch
                {
                    < 60 => "#4EC9B0",   // Green - excellent
                    < 100 => "#DCDCAA",  // Yellow - good
                    < 200 => "#CE9178",  // Orange - mediocre
                    _ => "#F44747"       // Red - bad
                };
            });
        }

        [RelayCommand]
        private void ResetData()
        {
            packetService.Reset();
            _sessionManager.Reset();

            Players.Clear();
            CombatDuration = "00:00";
            ActiveTargetName = string.Empty;
            ActiveTargetHpTotal = 0;
            ActiveTargetHpCurrent = 0;
            HasActiveTarget = false;
            ActiveTargetHpDisplay = string.Empty;
            ActiveTargetHpPercentage = 0;
        }

        private void OnPacketReceived(object? sender, PlayerDamage damageEvent)
        {
            _sessionManager.ProcessDamageEvent(damageEvent);
        }

        private void OnCombatAutoReset(object? sender, EventArgs e)
        {
            _dispatcher.BeginInvoke(() =>
            {
                Players.Clear();
                CombatDuration = "00:00";
                ActiveTargetName = string.Empty;
                ActiveTargetHpTotal = 0;
                ActiveTargetHpCurrent = 0;
                HasActiveTarget = false;
                ActiveTargetHpDisplay = string.Empty;
                ActiveTargetHpPercentage = 0;
            });
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
        {
            UpdatePlayerStats();
            UpdateCombatDuration();
            UpdateActiveTarget();
        }

        private void UpdatePlayerStats()
        {
            var currentStats = _sessionManager.PlayerStats;

            // Update existing players or add new ones
            foreach (var stats in currentStats)
            {
                var existingPlayer = Players.FirstOrDefault(p => p.PlayerId == stats.PlayerId);

                if (existingPlayer != null)
                {
                    existingPlayer.Update(stats);
                }
                else
                {
                    Players.Add(new PlayerStatsViewModel(stats));
                }
            }

            // Sort by total damage
            var sortedPlayers = Players.Where(r=> r.TotalDamage >0).OrderByDescending(p => p.TotalDamage).ToList();
            Players.Clear();
            foreach (var player in sortedPlayers)
            {
                Players.Add(player);
            }
        }

        private void UpdateCombatDuration()
        {
            var duration = _sessionManager.GetCombatDuration();
            CombatDuration = duration.ToString(@"mm\:ss");
        }

        private void UpdateActiveTarget()
        {
            var targetInfo = _sessionManager.GetActiveTargetInfo();
            if (targetInfo != null)
            {
                HasActiveTarget = true;
                ActiveTargetName = targetInfo.Name;
                ActiveTargetHpTotal = targetInfo.HpTotal;
                ActiveTargetHpCurrent = targetInfo.HpCurrent;
                ActiveTargetHpPercentage = targetInfo.HpTotal > 0
                    ? (double)targetInfo.HpCurrent / targetInfo.HpTotal * 100
                    : 0;
                ActiveTargetHpDisplay = targetInfo.HpTotal > 0
                    ? $"{FormatNumber(targetInfo.HpCurrent)} / {FormatNumber(targetInfo.HpTotal)}"
                    : string.Empty;
            }
            else
            {
                HasActiveTarget = false;
                ActiveTargetName = string.Empty;
                ActiveTargetHpTotal = 0;
                ActiveTargetHpCurrent = 0;
                ActiveTargetHpPercentage = 0;
                ActiveTargetHpDisplay = string.Empty;
            }
        }

        private static string FormatNumber(long number)
        {
            if (number >= 1_000_000_000)
                return $"{number / 1_000_000_000.0:F2}B";
            if (number >= 1_000_000)
                return $"{number / 1_000_000.0:F2}M";
            if (number >= 1_000)
                return $"{number / 1_000.0:F1}K";
            return number.ToString();
        }

        public void Dispose()
        {
            packetService.DamageReceived -= OnPacketReceived;
            packetService.PingUpdated -= OnPingUpdated;
            _sessionManager.CombatAutoReset -= OnCombatAutoReset;
            _updateTimer?.Stop();

            if (packetService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
