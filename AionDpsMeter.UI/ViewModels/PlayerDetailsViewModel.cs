using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels.History;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class PlayerDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly CombatSessionManager? _sessionManager;
        private readonly IAppSettingsService _settingsService;
        private readonly long _playerId;
        private readonly string? _playerIcon;
        private readonly string? _classIcon;
        private readonly DispatcherTimer? _updateTimer;

        /// <summary>True when this VM was created from a history snapshot — no live updates.</summary>
        public bool IsSnapshot { get; }

        [ObservableProperty] private ObservableCollection<SkillStatsViewModel> _skills = new();
        [ObservableProperty] private ObservableCollection<CombatLogEntryViewModel> _combatLog = new();
        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private string _playerNameDisplay;
        [ObservableProperty] private string _serverName = string.Empty;
        [ObservableProperty] private string _classNameDisplay = string.Empty;
        [ObservableProperty] private string? _playerIconDisplay;
        [ObservableProperty] private string? _classIconDisplay;
        [ObservableProperty] private int _combatPower;

        // ?? Summary stats ??????????????????????????????????????????????????????
        [ObservableProperty] private string _totalDamageDisplay = "0";
        [ObservableProperty] private string _dpsDisplay = "0";
        [ObservableProperty] private int _totalHits;
        [ObservableProperty] private string _criticalRateDisplay = "0%";
        [ObservableProperty] private string _backAttackRateDisplay = "0%";
        [ObservableProperty] private string _perfectRateDisplay = "0%";
        [ObservableProperty] private string _doubleDamageRateDisplay = "0%";
        [ObservableProperty] private string _parryRateDisplay = "0%";
        [ObservableProperty] private string _damageContributionDisplay = "0%";
        [ObservableProperty] private string _combatDurationDisplay = "00:00";
        [ObservableProperty] private int _skillCount;

        // ?? Active target ??????????????????????????????????????????????????????
        [ObservableProperty] private string _activeTargetName = string.Empty;
        [ObservableProperty] private int _activeTargetHpTotal;
        [ObservableProperty] private string _activeTargetHpTotalDisplay = string.Empty;
        [ObservableProperty] private bool _hasActiveTarget;

        // ?? View toggle ????????????????????????????????????????????????????????
        [ObservableProperty] private bool _showCombatLog;
        public bool ShowSkills => !ShowCombatLog;
        partial void OnShowCombatLogChanged(bool value) => OnPropertyChanged(nameof(ShowSkills));

        /// <summary>Nickname formatted as <c>Name[Server]</c> when server is known, otherwise just <c>Name</c>.</summary>
        public string PlayerNameWithServer
        {
            get
            {
                string name = _settingsService.IsNicknameHidden
                    ? NicknameObfuscator.Mask(_playerNameDisplay)
                    : _playerNameDisplay;
                return string.IsNullOrEmpty(_serverName)
                    ? name
                    : $"{name}[{_serverName}]";
            }
        }

        public bool HasPlayerIcon => !string.IsNullOrEmpty(_playerIcon);
        public bool HasClassIcon  => !string.IsNullOrEmpty(_classIcon);

        /// <summary>
        /// Creates a snapshot (read-only, no timer) VM from a <see cref="HistoryPlayerViewModel"/>.
        /// Used when opening player details from the history window.
        /// </summary>
        public static PlayerDetailsViewModel FromSnapshot(
            HistoryPlayerViewModel player,
            IAppSettingsService settingsService,
            string targetName)
        {
            var vm = new PlayerDetailsViewModel(
                sessionManager: null,
                playerId: player.PlayerId,
                playerName: player.PlayerName,
                className: player.ClassName,
                playerIcon: player.PlayerIcon,
                classIcon: player.ClassIcon,
                settingsService: settingsService,
                combatPower: player.CombatPower,
                serverName: player.ServerName,
                isSnapshot: true);

            // Populate from snapshot data immediately — no timer needed
            vm.TotalDamageDisplay        = player.TotalDamageDisplay;
            vm.DpsDisplay                = player.DpsDisplay;
            vm.TotalHits                 = player.HitCount;
            vm.CriticalRateDisplay       = player.CritRateDisplay;
            vm.BackAttackRateDisplay     = player.BackAttackRateDisplay;
            vm.PerfectRateDisplay        = player.PerfectRateDisplay;
            vm.DoubleDamageRateDisplay   = player.DoubleDamageRateDisplay;
            vm.ParryRateDisplay          = player.ParryRateDisplay;
            vm.DamageContributionDisplay = player.DamagePercentDisplay;
            vm.CombatDurationDisplay     = player.DurationDisplay;

            vm.HasActiveTarget            = !string.IsNullOrEmpty(targetName);
            vm.ActiveTargetName           = targetName;

            foreach (var skill in player.Skills)
                vm.Skills.Add(skill);
            vm.SkillCount = vm.Skills.Count;

            return vm;
        }

        public PlayerDetailsViewModel(
            CombatSessionManager? sessionManager,
            long playerId,
            string playerName,
            string className,
            string? playerIcon,
            string? classIcon,
            IAppSettingsService settingsService,
            int combatPower = 0,
            string serverName = "",
            bool isSnapshot = false)
        {
            IsSnapshot         = isSnapshot;
            _sessionManager    = sessionManager;
            _settingsService   = settingsService;
            _playerId          = playerId;
            _playerIcon        = playerIcon;
            _classIcon         = classIcon;
            _playerNameDisplay = playerName;
            _classNameDisplay  = className;
            _playerIconDisplay = playerIcon;
            _classIconDisplay  = classIcon;
            _combatPower       = combatPower;
            _serverName        = serverName;

            _settingsService.SettingsChanged += OnSettingsChanged;

            if (!isSnapshot)
            {
                _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(66) };
                _updateTimer.Tick += OnUpdateTimerTick;
                _updateTimer.Start();
                RefreshData();
            }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PlayerNameWithServer));
        }

        [RelayCommand]
        private void ToggleCombatLog() => ShowCombatLog = !ShowCombatLog;

        private void OnUpdateTimerTick(object? sender, EventArgs e) => RefreshData();

        private void RefreshData()
        {
            RefreshSkills();
            RefreshCombatLog();
            RefreshPlayerSummary();
        }

        private void RefreshPlayerSummary()
        {
            if (_sessionManager is null) return;

            var playerStats = _sessionManager.PlayerStats.FirstOrDefault(p => p.PlayerId == _playerId);
            if (playerStats is not null)
            {
                TotalDamageDisplay        = DamageFormatter.FormatFull(playerStats.TotalDamage);
                DpsDisplay                = DamageFormatter.Format(playerStats.DamagePerSecond);
                TotalHits                 = playerStats.HitCount;
                CriticalRateDisplay       = DamageFormatter.FormatRate(playerStats.CriticalRate);
                BackAttackRateDisplay     = DamageFormatter.FormatRate(playerStats.BackAttackRate);
                PerfectRateDisplay        = DamageFormatter.FormatRate(playerStats.PerfectRate);
                DoubleDamageRateDisplay   = DamageFormatter.FormatRate(playerStats.DoubleDamageRate);
                ParryRateDisplay          = DamageFormatter.FormatRate(playerStats.ParryRate);
                DamageContributionDisplay = DamageFormatter.FormatRate(playerStats.DamagePercentage);
                CombatDurationDisplay     = DamageFormatter.FormatDuration(playerStats.CombatDuration);
                if (playerStats.CombatPower > 0)
                    CombatPower = playerStats.CombatPower;
                if (!string.IsNullOrEmpty(playerStats.ServerName))
                {
                    ServerName = playerStats.ServerName;
                    OnPropertyChanged(nameof(PlayerNameWithServer));
                }
            }

            var targetInfo = _sessionManager.GetActiveTargetInfo();
            if (targetInfo is not null)
            {
                HasActiveTarget            = true;
                ActiveTargetName           = targetInfo.Name;
                ActiveTargetHpTotal        = targetInfo.HpTotal;
                ActiveTargetHpTotalDisplay = targetInfo.HpTotal > 0
                    ? $"HP: {DamageFormatter.FormatFull(targetInfo.HpTotal)}"
                    : string.Empty;
            }
            else
            {
                HasActiveTarget            = false;
                ActiveTargetName           = string.Empty;
                ActiveTargetHpTotal        = 0;
                ActiveTargetHpTotalDisplay = string.Empty;
            }
        }

        private void RefreshSkills()
        {
            if (_sessionManager is null) return;

            var skillStats = _sessionManager.GetPlayerSkillStats(_playerId);
            Skills.Clear();
            foreach (var skill in skillStats.OrderByDescending(s => s.TotalDamage))
                Skills.Add(new SkillStatsViewModel(skill));
            SkillCount = Skills.Count;
        }

        private void RefreshCombatLog()
        {
            if (_sessionManager is null) return;

            var combatLogEntries = _sessionManager.GetPlayerCombatLog(_playerId);
            CombatLog.Clear();
            foreach (var entry in combatLogEntries.Take(200))
                CombatLog.Add(new CombatLogEntryViewModel(entry));
        }

        public void Dispose()
        {
            _settingsService.SettingsChanged -= OnSettingsChanged;
            _updateTimer?.Stop();
        }
    }
}
