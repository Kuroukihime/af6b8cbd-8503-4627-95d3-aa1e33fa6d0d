using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class PlayerDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly CombatSessionManager _sessionManager;
        private readonly long _playerId;
        private readonly string _playerName;
        private readonly string _className;
        private readonly string? _playerIcon;
        private readonly string? _classIcon;
        private readonly DispatcherTimer _updateTimer;

        [ObservableProperty]
        private ObservableCollection<SkillStatsViewModel> _skills = new();

        [ObservableProperty]
        private ObservableCollection<CombatLogEntryViewModel> _combatLog = new();

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private string _playerNameDisplay;

        [ObservableProperty]
        private string _classNameDisplay = string.Empty;

        [ObservableProperty]
        private string? _playerIconDisplay;

        [ObservableProperty]
        private string? _classIconDisplay;

        // Summary stats
        [ObservableProperty]
        private string _totalDamageDisplay = "0";

        [ObservableProperty]
        private string _dpsDisplay = "0";

        [ObservableProperty]
        private int _totalHits;

        [ObservableProperty]
        private string _criticalRateDisplay = "0%";

        [ObservableProperty]
        private string _backAttackRateDisplay = "0%";

        [ObservableProperty]
        private string _perfectRateDisplay = "0%";

        [ObservableProperty]
        private string _doubleDamageRateDisplay = "0%";

        [ObservableProperty]
        private string _parryRateDisplay = "0%";

        [ObservableProperty]
        private string _damageContributionDisplay = "0%";

        [ObservableProperty]
        private string _combatDurationDisplay = "00:00";

        [ObservableProperty]
        private int _skillCount;

        public bool HasPlayerIcon => !string.IsNullOrEmpty(_playerIcon);
        public bool HasClassIcon => !string.IsNullOrEmpty(_classIcon);

        public PlayerDetailsViewModel(CombatSessionManager sessionManager, long playerId, string playerName, string className, string? playerIcon, string? classIcon)
        {
            _sessionManager = sessionManager;
            _playerId = playerId;
            _playerName = playerName;
            _className = className;
            _playerIcon = playerIcon;
            _classIcon = classIcon;
            _playerNameDisplay = playerName;
            _classNameDisplay = className;
            _playerIconDisplay = playerIcon;
            _classIconDisplay = classIcon;

            // Setup update timer for live updates (15 FPS)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(66)
            };
            _updateTimer.Tick += OnUpdateTimerTick;
            _updateTimer.Start();

            // Initial load
            RefreshData();
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            RefreshSkills();
            RefreshCombatLog();
            RefreshPlayerSummary();
        }

        private void RefreshPlayerSummary()
        {
            var playerStats = _sessionManager.PlayerStats.FirstOrDefault(p => p.PlayerId == _playerId);
            if (playerStats != null)
            {
                TotalDamageDisplay = FormatDamageWithNumber(playerStats.TotalDamage);
                DpsDisplay = FormatDamage((long)playerStats.DamagePerSecond);
                TotalHits = playerStats.HitCount;
                CriticalRateDisplay = $"{playerStats.CriticalRate:F1}%";
                BackAttackRateDisplay = $"{playerStats.BackAttackRate:F1}%";
                PerfectRateDisplay = $"{playerStats.PerfectRate:F1}%";
                DoubleDamageRateDisplay = $"{playerStats.DoubleDamageRate:F1}%";
                ParryRateDisplay = $"{playerStats.ParryRate:F1}%";
                DamageContributionDisplay = $"{playerStats.DamagePercentage:F1}%";
                CombatDurationDisplay = FormatDuration(playerStats.CombatDuration);
            }
        }

        private void RefreshSkills()
        {
            var skillStats = _sessionManager.GetPlayerSkillStats(_playerId);

            Skills.Clear();
            foreach (var skill in skillStats.OrderByDescending(s => s.TotalDamage))
            {
                Skills.Add(new SkillStatsViewModel(skill));
            }
            SkillCount = Skills.Count;
        }

        private void RefreshCombatLog()
        {
            var combatLogEntries = _sessionManager.GetPlayerCombatLog(_playerId);

            CombatLog.Clear();
            foreach (var entry in combatLogEntries.Take(200)) // Limit to 200 entries for performance
            {
                CombatLog.Add(new CombatLogEntryViewModel(entry));
            }
        }

        private static string FormatDamage(long damage)
        {
            if (damage >= 1_000_000_000)
                return $"{damage / 1_000_000_000.0:F2}B";
            if (damage >= 1_000_000)
                return $"{damage / 1_000_000.0:F2}M";
            if (damage >= 1_000)
                return $"{damage / 1_000.0:F2}K";
            return damage.ToString();
        }

        private static string FormatDamageWithNumber(long damage)
        {
            return damage.ToString("N0");
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        public void Dispose()
        {
            _updateTimer.Stop();
        }
    }
}
