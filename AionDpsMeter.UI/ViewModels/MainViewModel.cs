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

        private DateTime? _combatStartTime;
        private DispatcherTimer? _updateTimer;

        // Expose session manager for PlayerDetailsWindow
        public CombatSessionManager SessionManager => _sessionManager;


        public MainViewModel(IPacketService packetService, CombatSessionManager sessionManager)
        {
            this.packetService = packetService;
            _sessionManager = sessionManager;

            // Получаем Dispatcher из текущего потока
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Subscribe to damage events
            this.packetService.DamageReceived += OnPacketReceived;

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
            _combatStartTime = DateTime.Now;
        }

        [RelayCommand]
        private void ResetData()
        {
            packetService.Reset();
            _sessionManager.Reset();

            Players.Clear();
            _combatStartTime = DateTime.Now;
            CombatDuration = "00:00";
        }

        private void OnPacketReceived(object? sender, PlayerDamage damageEvent)
        {
            _sessionManager.ProcessDamageEvent(damageEvent);
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
        {
            UpdatePlayerStats();
            UpdateCombatDuration();
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
            var sortedPlayers = Players.OrderByDescending(p => p.TotalDamage).ToList();
            Players.Clear();
            foreach (var player in sortedPlayers)
            {
                Players.Add(player);
            }
        }

        private void UpdateCombatDuration()
        {
            if (_combatStartTime == null) return;

            var duration = DateTime.Now - _combatStartTime.Value;
            CombatDuration = duration.ToString(@"mm\:ss");
        }

        public void Dispose()
        {
            packetService.DamageReceived -= OnPacketReceived;
            _updateTimer?.Stop();

            if (packetService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
