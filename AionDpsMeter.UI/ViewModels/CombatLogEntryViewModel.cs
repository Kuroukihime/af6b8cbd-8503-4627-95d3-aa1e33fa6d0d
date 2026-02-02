using AionDpsMeter.Core.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class CombatLogEntryViewModel : ViewModelBase
    {
        private readonly PlayerDamage _damageEvent;

        public CombatLogEntryViewModel(PlayerDamage damageEvent)
        {
            _damageEvent = damageEvent;
        }

        public DateTime DateTime => _damageEvent.DateTime;
        public string TimeFormatted => _damageEvent.DateTime.ToString("HH:mm:ss.fff");
        public string SourceName => _damageEvent.SourceEntity.Name;
        public string TargetName => _damageEvent.TargetEntity.Name;
        public string SkillName => _damageEvent.Skill.Name;
        public string? SkillIcon => _damageEvent.Skill.Icon;
        public bool HasSkillIcon => !string.IsNullOrEmpty(_damageEvent.Skill.Icon);
        public long Damage => _damageEvent.Damage;
        public string DamageFormatted => FormatDamage(_damageEvent.Damage);
        public bool IsCritical => _damageEvent.IsCritical;
        public bool IsBackAttack => _damageEvent.IsBackAttack;
        public bool IsPerfect => _damageEvent.IsPerfect;
        public bool IsDoubleDamage => _damageEvent.IsDoubleDamage;
        public bool IsParry => _damageEvent.IsParry;

        public string Flags
        {
            get
            {
                var flags = new List<string>();
                if (IsCritical) flags.Add("CRIT");
                if (IsBackAttack) flags.Add("BACK");
                if (IsPerfect) flags.Add("PERFECT");
                if (IsDoubleDamage) flags.Add("x2");
                if (IsParry) flags.Add("PARRY");
                return flags.Count > 0 ? string.Join(" ", flags) : "";
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
    }
}
